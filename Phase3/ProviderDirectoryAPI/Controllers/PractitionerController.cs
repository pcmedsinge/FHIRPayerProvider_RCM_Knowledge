using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using ProviderDirectoryAPI.Services;
using System.Text.Json;

namespace ProviderDirectoryAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
public class PractitionerController : ControllerBase
{
    private readonly IProviderService _providerService;

    public PractitionerController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// GET /api/fhir/Practitioner/{id} — Get a specific practitioner
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPractitioner(string id)
    {
        var practitioner = await _providerService.GetPractitionerAsync(id);
        if (practitioner == null)
            return NotFound(new { error = "Practitioner not found", id });

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(practitioner, options);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// GET /api/fhir/Practitioner?name=xxx — Search practitioners
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchPractitioners(
        [FromQuery] string? name = null,
        [FromQuery] string? specialty = null,
        [FromQuery(Name = "_count")] int count = 20)
    {
        var bundle = await _providerService.SearchPractitionersAsync(name, specialty, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// GET /api/fhir/Practitioner/{id}/details — Full practitioner detail with roles/locations/orgs
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetPractitionerDetails(string id)
    {
        var bundle = await _providerService.GetPractitionerDetailsAsync(id);
        if (bundle.Entry.Count == 0)
            return NotFound(new { error = "Practitioner not found", id });

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
}
