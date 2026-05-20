using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace MemberAccessAPI.Services;

public class FhirService : IFhirService
{
    private readonly FhirClient _fhirClient;
    private readonly ILogger<FhirService> _logger;

    public FhirService(IConfiguration config, ILogger<FhirService> logger)
    {
        _logger = logger;
        var fhirServerUrl = config["FhirServer:BaseUrl"] ?? "http://localhost:8082/fhir";
        
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };
        _fhirClient = new FhirClient(fhirServerUrl, settings);
    }

    public async Task<Patient?> GetPatientAsync(string patientId)
    {
        try
        {
            return await _fhirClient.ReadAsync<Patient>($"Patient/{patientId}");
        }
        catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Patient {PatientId} not found", patientId);
            return null;
        }
    }

    public async Task<Bundle> SearchEOBByPatientAsync(string patientId, 
        string? type = null, string? startDate = null, string? endDate = null, int count = 10)
    {
        // Request more results when filtering client-side by type
        var fetchCount = !string.IsNullOrEmpty(type) ? count * 5 : count;

        var searchParams = new SearchParams()
            .Where($"patient=Patient/{patientId}")
            .LimitTo(fetchCount);

        // Note: "type" is NOT a valid HAPI search parameter for EOB
        // Valid params: care-team, claim, coverage, created, disposition, encounter,
        //   enterer, facility, identifier, patient, payee, provider, status
        // So we filter by type client-side after fetching

        if (!string.IsNullOrEmpty(startDate))
            searchParams.Where($"created=ge{startDate}");

        if (!string.IsNullOrEmpty(endDate))
            searchParams.Where($"created=le{endDate}");

        var bundle = await _fhirClient.SearchAsync<ExplanationOfBenefit>(searchParams) ?? new Bundle();

        // Client-side filter by claim type (professional, institutional, pharmacy)
        if (!string.IsNullOrEmpty(type))
        {
            var filtered = bundle.Entry?
                .Where(e => e.Resource is ExplanationOfBenefit eob &&
                    eob.Type?.Coding?.Any(c => c.Code == type) == true)
                .Take(count)
                .ToList() ?? new List<Bundle.EntryComponent>();

            bundle.Entry = filtered;
            bundle.Total = filtered.Count;
        }

        return bundle;
    }

    public async Task<Bundle> SearchCoverageByPatientAsync(string patientId)
    {
        var searchParams = new SearchParams()
            .Where($"beneficiary=Patient/{patientId}");

        return await _fhirClient.SearchAsync<Coverage>(searchParams) ?? new Bundle();
    }

    public async Task<Bundle> GetPatientEverythingAsync(string patientId)
    {
        // $everything is a FHIR operation that returns all data for a patient
        var result = await _fhirClient.InstanceOperationAsync(
            ResourceIdentity.Build("Patient", patientId), "everything", useGet: true);
        return result as Bundle ?? new Bundle();
    }
    public async Task<Bundle> GetNextPageAsync(string nextUrl)
    {
    // Follow the next page link from a Bundle
        var result = await _fhirClient.ContinueAsync(null);
        return result ?? new Bundle();
    }
}