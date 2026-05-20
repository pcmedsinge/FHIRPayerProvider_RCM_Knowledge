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
public class PatientController : ControllerBase
{
    
    private readonly IFhirService _fhirService;

    public PatientController(IFhirService fhirService)
    {
        _fhirService = fhirService;
    }

    /// <summary>
    /// GET /api/fhir/Patient/{id} — Get member demographics
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatient(string id)
    {
        // Access control: member can only see their own data
        if (!PatientAccessHandler.CanAccessPatient(User, id))
            return Forbid();

        var patient = await _fhirService.GetPatientAsync(id);
        
        if (patient == null)
            return NotFound(new { error = "Patient not found", patientId = id });

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(patient, options);
        return Content(json, "application/fhir+json");
    }
   
    /// <summary>
    /// GET /api/fhir/Patient/me — Get the authenticated member's own data
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var patientId = PatientAccessHandler.GetPatientId(User);
        if (patientId == null) return Unauthorized();

        var patient = await _fhirService.GetPatientAsync(patientId);
        if (patient == null) return NotFound();

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(patient, options);
        return Content(json, "application/fhir+json");
    }
    [HttpGet("{id}/$everything")]
    public async Task<IActionResult> GetEverything(string id)
    {
        if (!PatientAccessHandler.CanAccessPatient(User, id))
            return Forbid();

        var bundle = await _fhirService.GetPatientEverythingAsync(id);

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
}