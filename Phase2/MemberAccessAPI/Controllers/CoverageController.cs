using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using MemberAccessAPI.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using MemberAccessAPI.Auth;

namespace MemberAccessAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
[Authorize]
public class CoverageController : ControllerBase
{
    private readonly IFhirService _fhirService;

    public CoverageController(IFhirService fhirService)
    {
        _fhirService = fhirService;
    }

    /// <summary>
    /// GET /api/fhir/Coverage?patient={id} — Get member's insurance coverage
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchCoverage([FromQuery] string patient)
    {
        if (string.IsNullOrEmpty(patient))
            return BadRequest(new { error = "patient parameter is required" });

        // Access control: member can only see their own data
        if (!PatientAccessHandler.CanAccessPatient(User, patient))
            return Forbid();

        var bundle = await _fhirService.SearchCoverageByPatientAsync(patient);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        
        return Content(json, "application/fhir+json");
    }
}