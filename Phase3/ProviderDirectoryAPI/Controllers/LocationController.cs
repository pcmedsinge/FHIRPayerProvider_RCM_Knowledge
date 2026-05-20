using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using ProviderDirectoryAPI.Services;
using System.Text.Json;

namespace ProviderDirectoryAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
public class LocationController : ControllerBase
{
    private readonly IProviderService _providerService;

    public LocationController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// GET /api/fhir/Location/{id} — Get a specific location
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLocation(string id)
    {
        var location = await _providerService.GetLocationAsync(id);
        if (location == null)
            return NotFound(new { error = "Location not found", id });

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(location, options);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// GET /api/fhir/Location?city=xxx&state=xxx — Search locations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchLocations(
        [FromQuery] string? name = null,
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery(Name = "_count")] int count = 20)
    {
        var bundle = await _providerService.SearchLocationsAsync(name, city, state, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
}
