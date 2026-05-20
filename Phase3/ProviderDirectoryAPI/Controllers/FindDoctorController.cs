using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using ProviderDirectoryAPI.Services;
using System.Text.Json;

namespace ProviderDirectoryAPI.Controllers;

/// <summary>
/// Combined search endpoint simulating a "Find a Doctor" experience.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FindDoctorController : ControllerBase
{
    private readonly IProviderService _providerService;

    public FindDoctorController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// GET /api/FindDoctor?name=xxx&specialty=xxx&city=xxx&state=xxx&organization=xxx
    /// Search across practitioners, organizations, and locations in one call
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> FindDoctor(
        [FromQuery] string? name = null,
        [FromQuery] string? specialty = null,
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery] string? organization = null,
        [FromQuery(Name = "_count")] int count = 20)
    {
        var bundle = await _providerService.FindProvidersAsync(name, specialty, city, state, organization, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
}
