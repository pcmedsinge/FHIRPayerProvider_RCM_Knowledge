using Microsoft.AspNetCore.Mvc;
using FormularyAPI.Services;
using FormularyAPI.Models;

namespace FormularyAPI.Controllers;

/// <summary>
/// Insurance plans and their formulary tier structures.
/// Maps to DaVinci InsurancePlan profile.
/// </summary>
[ApiController]
[Route("api/formulary/[controller]")]
public class PlanController : ControllerBase
{
    private readonly IFormularyService _formularyService;

    public PlanController(IFormularyService formularyService)
    {
        _formularyService = formularyService;
    }

    /// <summary>
    /// GET /api/formulary/Plan — List all insurance plans
    /// </summary>
    [HttpGet]
    public IActionResult GetAllPlans()
    {
        var plans = _formularyService.GetAllPlans();
        return Ok(plans);
    }

    /// <summary>
    /// GET /api/formulary/Plan/{planId} — Get a specific plan with tier details
    /// </summary>
    [HttpGet("{planId}")]
    public IActionResult GetPlan(string planId)
    {
        var plan = _formularyService.GetPlan(planId);
        if (plan == null)
            return NotFound(new { error = "Plan not found", planId });
        return Ok(plan);
    }

    /// <summary>
    /// GET /api/formulary/Plan/{planId}/tiers — Get drug tiers for a plan
    /// </summary>
    [HttpGet("{planId}/tiers")]
    public IActionResult GetPlanTiers(string planId)
    {
        var plan = _formularyService.GetPlan(planId);
        if (plan == null)
            return NotFound(new { error = "Plan not found", planId });
        return Ok(plan.DrugTiers);
    }
}
