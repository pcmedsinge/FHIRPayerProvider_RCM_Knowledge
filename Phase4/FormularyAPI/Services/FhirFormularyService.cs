using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace FormularyAPI.Services;

/// <summary>
/// Service for interacting with FHIR MedicationRequest resources in HAPI.
/// Synthea creates MedicationRequest resources linked to patients.
/// </summary>
public interface IFhirFormularyService
{
    System.Threading.Tasks.Task<Bundle> SearchMedicationRequestsAsync(string? patient = null, string? status = null, int count = 20);
    System.Threading.Tasks.Task<Bundle> SearchMedicationsAsync(string? code = null, int count = 20);
    System.Threading.Tasks.Task<MedicationRequest?> GetMedicationRequestAsync(string id);
}

public class FhirFormularyService : IFhirFormularyService
{
    private readonly FhirClient _fhirClient;
    private readonly ILogger<FhirFormularyService> _logger;

    public FhirFormularyService(IConfiguration config, ILogger<FhirFormularyService> logger)
    {
        _logger = logger;
        var fhirServerUrl = config["FhirServer:BaseUrl"] ?? "http://localhost:8082/fhir";
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };
        _fhirClient = new FhirClient(fhirServerUrl, settings);
    }

    public async System.Threading.Tasks.Task<Bundle> SearchMedicationRequestsAsync(
        string? patient = null, string? status = null, int count = 20)
    {
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(patient)) sp.Where($"patient=Patient/{patient}");
        if (!string.IsNullOrEmpty(status))  sp.Where($"status={status}");
        return await _fhirClient.SearchAsync<MedicationRequest>(sp) ?? new Bundle();
    }

    public async System.Threading.Tasks.Task<Bundle> SearchMedicationsAsync(string? code = null, int count = 20)
    {
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(code)) sp.Where($"code={code}");
        return await _fhirClient.SearchAsync<Medication>(sp) ?? new Bundle();
    }

    public async System.Threading.Tasks.Task<MedicationRequest?> GetMedicationRequestAsync(string id)
    {
        try
        {
            return await _fhirClient.ReadAsync<MedicationRequest>($"MedicationRequest/{id}");
        }
        catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("MedicationRequest {Id} not found", id);
            return null;
        }
    }
}
