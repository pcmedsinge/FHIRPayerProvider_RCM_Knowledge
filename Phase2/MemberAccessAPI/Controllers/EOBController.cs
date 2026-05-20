using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using MemberAccessAPI.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using MemberAccessAPI.Auth;

[ApiController]
[Route("api/fhir/ExplanationOfBenefit")]
[Authorize]
public class EOBController : ControllerBase
{
    private readonly IFhirService _fhirService;
    

    public EOBController(IFhirService fhirService)
    {
        _fhirService = fhirService;
    }

    /// <summary>
    /// GET /api/fhir/ExplanationOfBenefit?patient={id} — Get member's claims
    /// Optional: type (professional|institutional|pharmacy), startDate, endDate, _count
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchEOB(
        [FromQuery] string patient,
        [FromQuery] string? type = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery(Name = "_count")] int count = 10)
    {
        if (string.IsNullOrEmpty(patient))
            return BadRequest(new { error = "patient parameter is required" });

        // Access control: member can only see their own data
        if (!PatientAccessHandler.CanAccessPatient(User, patient))
            return Forbid();

        if (!PatientAccessHandler.HasScope(User, "ExplanationOfBenefit"))
            return StatusCode(403, "Insufficient scope. Required: patient/ExplanationOfBenefit.read");

        var bundle = await _fhirService.SearchEOBByPatientAsync(patient, type, startDate, endDate, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
    
}