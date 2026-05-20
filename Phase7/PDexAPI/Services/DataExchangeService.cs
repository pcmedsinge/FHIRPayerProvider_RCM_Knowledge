using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using PDexAPI.Models;
using System.Text.Json;

namespace PDexAPI.Services;

/// <summary>
/// Data exchange service — orchestrates payer-to-payer data transfer.
/// Pulls data from HAPI FHIR (simulating old payer) and tracks transfer.
/// </summary>
public interface IDataExchangeService
{
    DataExchangeJob InitiateExchange(ExchangeRequest request, DataExchangeConsent consent);
    DataExchangeJob? GetJob(string jobId);
    List<DataExchangeJob> GetAllJobs();
    List<DataExchangeJob> GetJobsByPatient(string patientId);
    DataExchangeJob? CancelJob(string jobId);
    System.Threading.Tasks.Task<DataExchangeJob> ExecuteExchangeAsync(string jobId);
}

public class DataExchangeService : IDataExchangeService
{
    private readonly List<DataExchangeJob> _jobs = new();
    private readonly IConsentService _consentService;
    private readonly IConfiguration _config;
    private readonly ILogger<DataExchangeService> _logger;

    public DataExchangeService(IConsentService consentService, IConfiguration config, ILogger<DataExchangeService> logger)
    {
        _consentService = consentService;
        _config = config;
        _logger = logger;
    }

    public DataExchangeJob InitiateExchange(ExchangeRequest request, DataExchangeConsent consent)
    {
        var job = new DataExchangeJob
        {
            JobId = $"EXCH-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            ConsentId = consent.ConsentId,
            PatientId = consent.PatientId,
            SourcePayerId = consent.SourcePayerId,
            TargetPayerId = consent.TargetPayerId,
            Status = "queued",
            RequestedCategories = request.DataCategories ?? consent.DataCategories
        };

        _jobs.Add(job);
        _logger.LogInformation("Exchange job {JobId} created for patient {PatientId}", job.JobId, job.PatientId);
        return job;
    }

    public DataExchangeJob? GetJob(string jobId) =>
        _jobs.FirstOrDefault(j => j.JobId == jobId);

    public List<DataExchangeJob> GetAllJobs() => _jobs.ToList();

    public List<DataExchangeJob> GetJobsByPatient(string patientId) =>
        _jobs.Where(j => j.PatientId == patientId).ToList();

    public DataExchangeJob? CancelJob(string jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job == null || job.Status == "completed" || job.Status == "cancelled") return null;

        job.Status = "cancelled";
        return job;
    }

    /// <summary>
    /// Execute the data exchange — pull from HAPI FHIR server (simulating old payer's data).
    /// </summary>
    public async System.Threading.Tasks.Task<DataExchangeJob> ExecuteExchangeAsync(string jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.JobId == jobId);
        if (job == null) throw new InvalidOperationException($"Job {jobId} not found");

        job.Status = "in-progress";
        job.StartedAt = DateTime.UtcNow;

        try
        {
            var baseUrl = _config["FhirServer:BaseUrl"] ?? "http://localhost:8082/fhir";
            var settings = new FhirClientSettings
            {
                PreferredFormat = ResourceFormat.Json
            };
            var client = new FhirClient(baseUrl, settings);

            var categoryToResourceType = new Dictionary<string, string[]>
            {
                ["claims"] = new[] { "ExplanationOfBenefit" },
                ["encounters"] = new[] { "Encounter" },
                ["medications"] = new[] { "MedicationRequest" },
                ["conditions"] = new[] { "Condition" },
                ["allergies"] = new[] { "AllergyIntolerance" },
                ["procedures"] = new[] { "Procedure" },
                ["observations"] = new[] { "Observation" },
                ["diagnostics"] = new[] { "DiagnosticReport" },
                ["immunizations"] = new[] { "Immunization" }
            };

            foreach (var category in job.RequestedCategories)
            {
                if (!categoryToResourceType.TryGetValue(category, out var resourceTypes))
                    continue;

                foreach (var resourceType in resourceTypes)
                {
                    try
                    {
                        var searchParams = new SearchParams()
                            .Where($"patient=Patient/{job.PatientId}")
                            .LimitTo(50);

                        Bundle? results = null;

                        // Use appropriate search based on resource type
                        switch (resourceType)
                        {
                            case "ExplanationOfBenefit":
                                results = await client.SearchAsync<ExplanationOfBenefit>(searchParams);
                                break;
                            case "Encounter":
                                results = await client.SearchAsync<Encounter>(searchParams);
                                break;
                            case "MedicationRequest":
                                results = await client.SearchAsync<MedicationRequest>(searchParams);
                                break;
                            case "Condition":
                                results = await client.SearchAsync<Condition>(searchParams);
                                break;
                            case "AllergyIntolerance":
                                results = await client.SearchAsync<AllergyIntolerance>(searchParams);
                                break;
                            case "Procedure":
                                results = await client.SearchAsync<Procedure>(searchParams);
                                break;
                            case "Observation":
                                results = await client.SearchAsync<Observation>(searchParams);
                                break;
                            case "DiagnosticReport":
                                results = await client.SearchAsync<DiagnosticReport>(searchParams);
                                break;
                            case "Immunization":
                                results = await client.SearchAsync<Immunization>(searchParams);
                                break;
                        }

                        if (results?.Entry != null)
                        {
                            foreach (var entry in results.Entry)
                            {
                                if (entry.Resource == null) continue;

                                var provenanceId = $"PROV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

                                job.ExchangedResources.Add(new ExchangedResource
                                {
                                    ResourceType = resourceType,
                                    ResourceId = entry.Resource.Id,
                                    Category = category,
                                    TransferredAt = DateTime.UtcNow,
                                    ProvenanceId = provenanceId
                                });

                                job.ProvenanceRecords.Add(new ProvenanceRecord
                                {
                                    ProvenanceId = provenanceId,
                                    TargetResourceType = resourceType,
                                    TargetResourceId = entry.Resource.Id,
                                    Agent = job.SourcePayerId,
                                    Activity = "transmit",
                                    Recorded = DateTime.UtcNow,
                                    SourcePayerId = job.SourcePayerId
                                });
                            }

                            job.TotalResourcesFound += results.Entry.Count;
                            job.TotalResourcesTransferred += results.Entry.Count;
                        }

                        _logger.LogInformation("Exchanged {Count} {Type} resources for job {JobId}",
                            results?.Entry?.Count ?? 0, resourceType, job.JobId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch {ResourceType} for job {JobId}", resourceType, job.JobId);
                    }
                }
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Exchange job {JobId} failed", job.JobId);
        }

        return job;
    }
}
