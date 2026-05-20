using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using ProviderDirectoryAPI.Services;
using System.Text.Json;

namespace ProviderDirectoryAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
public class HealthcareServiceController : ControllerBase
{
    private readonly IProviderService _providerService;

    public HealthcareServiceController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// GET /api/fhir/HealthcareService?organization=xxx&service-type=xxx
    /// Search healthcare services offered by organizations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchHealthcareServices(
        [FromQuery] string? organization = null,
        [FromQuery(Name = "service-type")] string? serviceType = null,
        [FromQuery] string? specialty = null,
        [FromQuery(Name = "_count")] int count = 20)
    {
        var bundle = await _providerService.SearchHealthcareServicesAsync(organization, serviceType, specialty, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
}
