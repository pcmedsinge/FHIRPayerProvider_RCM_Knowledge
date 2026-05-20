using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using FormularyAPI.Services;
using System.Text.Json;

namespace FormularyAPI.Controllers;

/// <summary>
/// Access FHIR MedicationRequest resources from HAPI (patient prescriptions from Synthea).
/// Shows how formulary integrates with actual patient medication data.
/// </summary>
[ApiController]
[Route("api/fhir/[controller]")]
public class MedicationRequestController : ControllerBase
{
    private readonly IFhirFormularyService _fhirService;

    public MedicationRequestController(IFhirFormularyService fhirService)
    {
        _fhirService = fhirService;
    }

    /// <summary>
    /// GET /api/fhir/MedicationRequest?patient={id}&status=xxx — Get patient's prescriptions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchMedicationRequests(
        [FromQuery] string? patient = null,
        [FromQuery] string? status = null,
        [FromQuery(Name = "_count")] int count = 20)
    {
        var bundle = await _fhirService.SearchMedicationRequestsAsync(patient, status, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// GET /api/fhir/MedicationRequest/{id} — Get a specific prescription
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMedicationRequest(string id)
    {
        var medReq = await _fhirService.GetMedicationRequestAsync(id);
        if (medReq == null)
            return NotFound(new { error = "MedicationRequest not found", id });

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(medReq, options);
        return Content(json, "application/fhir+json");
    }
}
