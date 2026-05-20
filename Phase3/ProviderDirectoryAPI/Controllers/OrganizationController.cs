using Microsoft.AspNetCore.Mvc;
using Hl7.Fhir.Serialization;
using ProviderDirectoryAPI.Services;
using System.Text.Json;

namespace ProviderDirectoryAPI.Controllers;

[ApiController]
[Route("api/fhir/[controller]")]
public class OrganizationController : ControllerBase
{
    private readonly IProviderService _providerService;

    public OrganizationController(IProviderService providerService)
    {
        _providerService = providerService;
    }

    /// <summary>
    /// GET /api/fhir/Organization/{id} — Get a specific organization
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrganization(string id)
    {
        var org = await _providerService.GetOrganizationAsync(id);
        if (org == null)
            return NotFound(new { error = "Organization not found", id });

        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(org, options);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// GET /api/fhir/Organization?name=xxx&type=xxx — Search organizations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchOrganizations(
        [FromQuery] string? name = null,
        [FromQuery] string? type = null,
        [FromQuery] string? address = null,
        [FromQuery(Name = "_count")] int count = 20)
    {
        var bundle = await _providerService.SearchOrganizationsAsync(name, type, address, count);
        var options = new JsonSerializerOptions().ForFhir().Pretty();
        var json = JsonSerializer.Serialize(bundle, options);
        return Content(json, "application/fhir+json");
    }
}
