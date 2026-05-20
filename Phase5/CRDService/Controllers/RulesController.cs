using Microsoft.AspNetCore.Mvc;
using CRDService.Services;

namespace CRDService.Controllers;

/// <summary>
/// Admin/debug endpoint to view and test coverage rules directly.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly ICoverageRulesService _rulesService;

    public RulesController(ICoverageRulesService rulesService)
    {
        _rulesService = rulesService;
    }

    /// <summary>
    /// GET /api/Rules — List all coverage rules
    /// </summary>
    [HttpGet]
    public IActionResult GetAllRules()
    {
        var rules = _rulesService.GetAllRules();
        return Ok(new { total = rules.Count, rules });
    }

    /// <summary>
    /// GET /api/Rules/type/{serviceType} — Get rules by type (imaging, procedure, medication, dme)
    /// </summary>
    [HttpGet("type/{serviceType}")]
    public IActionResult GetRulesByType(string serviceType)
    {
        var rules = _rulesService.GetRulesByType(serviceType);
        return Ok(new { total = rules.Count, serviceType, rules });
    }

    /// <summary>
    /// GET /api/Rules/check/{code} — Quick check if a code requires prior auth
    /// </summary>
    [HttpGet("check/{code}")]
    public IActionResult CheckCode(string code)
    {
        var rule = _rulesService.FindRule(code);
        if (rule == null)
            return Ok(new
            {
                code,
                found = false,
                message = "No specific coverage rule found for this code. Standard coverage applies."
            });

        return Ok(new
        {
            code,
            found = true,
            rule.Description,
            rule.CoverageStatus,
            rule.RequiresPriorAuth,
            rule.RequiresDocumentation,
            rule.RequiredDocuments,
            rule.AlternativeSuggestion,
            rule.ConditionNote
        });
    }
}
