using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using BulkDataAPI.Models;
using System.Text.Json;

namespace BulkDataAPI.Services;

/// <summary>
/// Bulk export service — orchestrates async FHIR $export operations.
/// Pulls data from HAPI FHIR server, generates NDJSON output files.
/// Supports system-level, patient-level, and group-level exports.
/// </summary>
public interface IBulkExportService
{
    ExportJob InitiateExport(ExportRequest request);
    ExportJob? GetJob(string jobId);
    List<ExportJob> GetAllJobs();
    ExportJob? CancelJob(string jobId);
    ExportJob? DeleteJob(string jobId);
    System.Threading.Tasks.Task<ExportJob> ExecuteExportAsync(string jobId);
    string? GetNdjsonContent(string jobId, string resourceType);
    DataAnalytics? GetAnalytics(string jobId);
}

public class BulkExportService : IBulkExportService
{
    private readonly List<ExportJob> _jobs = new();
    private readonly Dictionary<string, Dictionary<string, List<NdjsonResource>>> _exportData = new();
    private readonly IGroupService _groupService;
    private readonly IConfiguration _config;
    private readonly ILogger<BulkExportService> _logger;

    public BulkExportService(IGroupService groupService, IConfiguration config, ILogger<BulkExportService> logger)
    {
        _groupService = groupService;
        _config = config;
        _logger = logger;
    }

    public ExportJob InitiateExport(ExportRequest request)
    {
        var defaultTypes = _config["BulkExport:DefaultResourceTypes"] ?? "Patient,ExplanationOfBenefit";
        var resourceTypes = request.ResourceTypes?.Count > 0
            ? request.ResourceTypes
            : defaultTypes.Split(',').ToList();

        var job = new ExportJob
        {
            JobId = $"EXPORT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Status = "queued",
            ExportType = request.ExportType,
            GroupId = request.GroupId,
            ResourceTypes = resourceTypes,
            Since = request.Since,
            TypeFilter = request.TypeFilter,
            TransactionTime = DateTime.UtcNow.ToString("O")
        };

        _jobs.Add(job);
        _logger.LogInformation("Export job {JobId} created: type={Type}, resources={Resources}",
            job.JobId, job.ExportType, string.Join(",", job.ResourceTypes));
        return job;
    }

    public ExportJob? GetJob(string jobId) =>
        _jobs.FirstOrDefault(j => j.JobId == jobId);

    public List<ExportJob> GetAllJobs() => _jobs.ToList();

    public ExportJob? CancelJob(string jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job == null || job.Status == "completed" || job.Status == "cancelled") return null;
        job.Status = "cancelled";
        return job;
    }

    public ExportJob? DeleteJob(string jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job == null) return null;
        _jobs.Remove(job);
        _exportData.Remove(jobId);
        return job;
    }

    public async System.Threading.Tasks.Task<ExportJob> ExecuteExportAsync(string jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job == null) throw new InvalidOperationException($"Job {jobId} not found");
        if (job.Status == "completed") return job;

        job.Status = "in-progress";
        job.StartedAt = DateTime.UtcNow;
        var jobData = new Dictionary<string, List<NdjsonResource>>();

        try
        {
            var baseUrl = _config["FhirServer:BaseUrl"] ?? "http://localhost:8082/fhir";
            var settings = new FhirClientSettings { PreferredFormat = ResourceFormat.Json };
            var client = new FhirClient(baseUrl, settings);
            var jsonOptions = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector).Pretty();

            // Determine patient scope
            List<string>? patientIds = null;
            if (job.ExportType == "group" && !string.IsNullOrEmpty(job.GroupId))
            {
                var group = _groupService.GetGroup(job.GroupId);
                if (group == null) throw new InvalidOperationException($"Group {job.GroupId} not found");
                patientIds = group.MemberPatientIds;
            }

            int totalProcessed = 0;
            int totalTypes = job.ResourceTypes.Count;

            foreach (var resourceType in job.ResourceTypes)
            {
                try
                {
                    var resources = new List<NdjsonResource>();
                    Bundle? results = null;

                    // Build search parameters
                    var searchParams = new SearchParams().LimitTo(200);

                    // Add _since filter if specified
                    // Note: HAPI FHIR supports _lastUpdated parameter

                    if (job.ExportType == "patient" || job.ExportType == "group")
                    {
                        // Patient-scoped export
                        var targetPatients = patientIds ?? new List<string>();

                        if (resourceType == "Patient")
                        {
                            // For Patient resources, fetch each patient
                            foreach (var pid in targetPatients)
                            {
                                try
                                {
                                    var patient = await client.ReadAsync<Patient>($"Patient/{pid}");
                                    if (patient != null)
                                    {
                                        var json = JsonSerializer.Serialize(patient, jsonOptions);
                                        resources.Add(new NdjsonResource
                                        {
                                            ResourceType = "Patient",
                                            ResourceId = patient.Id,
                                            JsonContent = json
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to read Patient/{Id}", pid);
                                }
                            }
                        }
                        else
                        {
                            // For other resources, search by patient
                            foreach (var pid in targetPatients)
                            {
                                try
                                {
                                    var patientSearch = new SearchParams()
                                        .Where($"patient=Patient/{pid}")
                                        .LimitTo(100);

                                    var bundle = await SearchByTypeAsync(client, resourceType, patientSearch);
                                    if (bundle?.Entry != null)
                                    {
                                        foreach (var entry in bundle.Entry)
                                        {
                                            if (entry.Resource == null) continue;
                                            var json = JsonSerializer.Serialize(entry.Resource, jsonOptions);
                                            resources.Add(new NdjsonResource
                                            {
                                                ResourceType = resourceType,
                                                ResourceId = entry.Resource.Id,
                                                JsonContent = json
                                            });
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to search {Type} for Patient/{Id}", resourceType, pid);
                                }
                            }
                        }
                    }
                    else
                    {
                        // System-level export — get all resources of this type
                        results = await SearchByTypeAsync(client, resourceType, searchParams);

                        if (results?.Entry != null)
                        {
                            foreach (var entry in results.Entry)
                            {
                                if (entry.Resource == null) continue;
                                var json = JsonSerializer.Serialize(entry.Resource, jsonOptions);
                                resources.Add(new NdjsonResource
                                {
                                    ResourceType = resourceType,
                                    ResourceId = entry.Resource.Id,
                                    JsonContent = json
                                });
                            }
                        }
                    }

                    if (resources.Count > 0)
                    {
                        jobData[resourceType] = resources;

                        // Create NDJSON content
                        var ndjsonContent = string.Join("\n", resources.Select(r =>
                            r.JsonContent.Replace("\r\n", " ").Replace("\n", " ")));
                        var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(ndjsonContent);

                        job.Output.Add(new ExportOutput
                        {
                            Type = resourceType,
                            Url = $"http://localhost:5280/api/bulk/Export/{jobId}/download/{resourceType}",
                            Count = resources.Count,
                            SizeBytes = sizeBytes
                        });
                    }

                    totalProcessed++;
                    job.ProgressPercent = (int)((double)totalProcessed / totalTypes * 100);

                    _logger.LogInformation("Exported {Count} {Type} resources for job {JobId}",
                        resources.Count, resourceType, job.JobId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to export {Type} for job {JobId}", resourceType, job.JobId);
                    job.Errors.Add(new ExportError
                    {
                        Message = $"Failed to export {resourceType}: {ex.Message}"
                    });
                }
            }

            _exportData[jobId] = jobData;

            // Build summary
            var totalResources = jobData.Values.Sum(list => list.Count);
            var totalSize = job.Output.Sum(o => o.SizeBytes);

            job.Summary = new ExportSummary
            {
                TotalResources = totalResources,
                TotalFiles = job.Output.Count,
                TotalSizeBytes = totalSize,
                ResourceTypeCounts = jobData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count),
                DurationSeconds = (DateTime.UtcNow - (job.StartedAt ?? DateTime.UtcNow)).TotalSeconds
            };

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressPercent = 100;
            job.ExpiresAt = DateTime.UtcNow.AddHours(24); // NDJSON files available for 24h
        }
        catch (Exception ex)
        {
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Export job {JobId} failed", job.JobId);
        }

        return job;
    }

    public string? GetNdjsonContent(string jobId, string resourceType)
    {
        if (!_exportData.TryGetValue(jobId, out var jobData)) return null;
        if (!jobData.TryGetValue(resourceType, out var resources)) return null;

        // Generate NDJSON: one JSON object per line, no pretty printing
        var lines = resources.Select(r =>
        {
            // Compact the JSON (remove whitespace/newlines)
            try
            {
                var doc = JsonDocument.Parse(r.JsonContent);
                return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = false });
            }
            catch
            {
                return r.JsonContent.Replace("\r\n", " ").Replace("\n", " ");
            }
        });

        return string.Join("\n", lines);
    }

    public DataAnalytics? GetAnalytics(string jobId)
    {
        if (!_exportData.TryGetValue(jobId, out var jobData)) return null;

        var analytics = new DataAnalytics();

        // Count by resource type
        if (jobData.TryGetValue("Patient", out var patients))
        {
            analytics.TotalPatients = patients.Count;

            // Parse patient demographics
            foreach (var p in patients)
            {
                try
                {
                    var doc = JsonDocument.Parse(p.JsonContent);
                    var gender = doc.RootElement.TryGetProperty("gender", out var g) ? g.GetString() ?? "unknown" : "unknown";
                    analytics.PatientsByGender.TryGetValue(gender, out var count);
                    analytics.PatientsByGender[gender] = count + 1;
                }
                catch { }
            }
        }

        if (jobData.TryGetValue("ExplanationOfBenefit", out var eobs))
        {
            analytics.TotalClaims = eobs.Count;

            foreach (var eob in eobs)
            {
                try
                {
                    var doc = JsonDocument.Parse(eob.JsonContent);

                    // Try to get claim type
                    if (doc.RootElement.TryGetProperty("type", out var typeEl) &&
                        typeEl.TryGetProperty("coding", out var codingArr))
                    {
                        foreach (var coding in codingArr.EnumerateArray())
                        {
                            if (coding.TryGetProperty("code", out var code))
                            {
                                var typeCode = code.GetString() ?? "unknown";
                                analytics.ClaimsByType.TryGetValue(typeCode, out var cnt);
                                analytics.ClaimsByType[typeCode] = cnt + 1;
                            }
                        }
                    }

                    // Try to get total amount
                    if (doc.RootElement.TryGetProperty("total", out var totalArr))
                    {
                        foreach (var total in totalArr.EnumerateArray())
                        {
                            if (total.TryGetProperty("amount", out var amount) &&
                                amount.TryGetProperty("value", out var value))
                            {
                                analytics.TotalClaimAmount += value.GetDecimal();
                            }
                        }
                    }
                }
                catch { }
            }

            if (analytics.TotalClaims > 0)
                analytics.AverageClaimAmount = analytics.TotalClaimAmount / analytics.TotalClaims;
        }

        if (jobData.TryGetValue("Encounter", out var encounters))
        {
            analytics.TotalEncounters = encounters.Count;

            foreach (var enc in encounters)
            {
                try
                {
                    var doc = JsonDocument.Parse(enc.JsonContent);
                    if (doc.RootElement.TryGetProperty("class", out var classEl) &&
                        classEl.TryGetProperty("code", out var code))
                    {
                        var classCode = code.GetString() ?? "unknown";
                        analytics.EncountersByClass.TryGetValue(classCode, out var cnt);
                        analytics.EncountersByClass[classCode] = cnt + 1;
                    }
                }
                catch { }
            }
        }

        if (jobData.TryGetValue("MedicationRequest", out var meds))
        {
            analytics.TotalMedications = meds.Count;

            foreach (var med in meds)
            {
                try
                {
                    var doc = JsonDocument.Parse(med.JsonContent);
                    if (doc.RootElement.TryGetProperty("medicationCodeableConcept", out var medCode) &&
                        medCode.TryGetProperty("coding", out var codingArr))
                    {
                        foreach (var coding in codingArr.EnumerateArray())
                        {
                            if (coding.TryGetProperty("display", out var display))
                            {
                                var medName = display.GetString() ?? "unknown";
                                // Truncate long medication names
                                if (medName.Length > 50) medName = medName[..50] + "...";
                                analytics.TopMedications.TryGetValue(medName, out var cnt);
                                analytics.TopMedications[medName] = cnt + 1;
                            }
                        }
                    }
                }
                catch { }
            }

            // Keep only top 20 medications
            analytics.TopMedications = analytics.TopMedications
                .OrderByDescending(kvp => kvp.Value)
                .Take(20)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        if (jobData.TryGetValue("Condition", out var conditions))
        {
            analytics.TotalConditions = conditions.Count;

            foreach (var cond in conditions)
            {
                try
                {
                    var doc = JsonDocument.Parse(cond.JsonContent);
                    if (doc.RootElement.TryGetProperty("code", out var codeEl) &&
                        codeEl.TryGetProperty("coding", out var codingArr))
                    {
                        foreach (var coding in codingArr.EnumerateArray())
                        {
                            if (coding.TryGetProperty("display", out var display))
                            {
                                var condName = display.GetString() ?? "unknown";
                                if (condName.Length > 60) condName = condName[..60] + "...";
                                analytics.TopConditions.TryGetValue(condName, out var cnt);
                                analytics.TopConditions[condName] = cnt + 1;
                            }
                        }
                    }
                }
                catch { }
            }

            // Keep only top 20 conditions
            analytics.TopConditions = analytics.TopConditions
                .OrderByDescending(kvp => kvp.Value)
                .Take(20)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        if (jobData.TryGetValue("Procedure", out var procedures))
            analytics.TotalProcedures = procedures.Count;

        return analytics;
    }

    /// <summary>
    /// Search HAPI FHIR by resource type string.
    /// </summary>
    private async System.Threading.Tasks.Task<Bundle?> SearchByTypeAsync(FhirClient client, string resourceType, SearchParams searchParams)
    {
        return resourceType switch
        {
            "Patient" => await client.SearchAsync<Patient>(searchParams),
            "ExplanationOfBenefit" => await client.SearchAsync<ExplanationOfBenefit>(searchParams),
            "Coverage" => await client.SearchAsync<Coverage>(searchParams),
            "Encounter" => await client.SearchAsync<Encounter>(searchParams),
            "MedicationRequest" => await client.SearchAsync<MedicationRequest>(searchParams),
            "Condition" => await client.SearchAsync<Condition>(searchParams),
            "Procedure" => await client.SearchAsync<Procedure>(searchParams),
            "Observation" => await client.SearchAsync<Observation>(searchParams),
            "AllergyIntolerance" => await client.SearchAsync<AllergyIntolerance>(searchParams),
            "DiagnosticReport" => await client.SearchAsync<DiagnosticReport>(searchParams),
            "Immunization" => await client.SearchAsync<Immunization>(searchParams),
            "Practitioner" => await client.SearchAsync<Practitioner>(searchParams),
            "Organization" => await client.SearchAsync<Organization>(searchParams),
            _ => null
        };
    }
}
