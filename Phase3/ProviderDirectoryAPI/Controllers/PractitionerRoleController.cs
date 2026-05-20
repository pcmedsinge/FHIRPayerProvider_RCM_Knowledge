using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using ProviderDirectoryAPI.Services;
using System.Text.Json;

namespace ProviderDirectoryAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
public class PractitionerRoleController : ControllerBase
{
    private readonly IProviderService _providerService;

    public PractitionerRoleController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// GET /api/fhir/PractitionerRole?practitioner=xxx&organization=xxx&specialty=xxx
    /// Search practitioner roles linking practitioners to organizations and specialties
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchPractitionerRoles(
        [FromQuery] string? practitioner = null,
        [FromQuery] string? organization = null,
        [FromQuery] string? specialty = null,
        [FromQuery(Name = "_count")] int count = 20)
    {
        var bundle = await _providerService.SearchPractitionerRolesAsync(practitioner, organization, specialty, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
}
