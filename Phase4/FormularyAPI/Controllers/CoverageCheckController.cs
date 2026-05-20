using Microsoft.AspNetCore.Mvc;
using FormularyAPI.Services;
using FormularyAPI.Models;

namespace FormularyAPI.Controllers;

/// <summary>
/// Coverage check: determine if a drug is covered and what it will cost.
/// </summary>
[ApiController]
[Route("api/formulary/[controller]")]
public class CoverageCheckController : ControllerBase
{
    private readonly IFormularyService _formularyService;

    public CoverageCheckController(IFormularyService formularyService)
    {
        _formularyService = formularyService;
    }

    /// <summary>
    /// POST /api/formulary/CoverageCheck — Check if a drug is covered under a plan
    /// Body: { "drugName": "Ozempic", "planId": "PLAN-PPO-2026" }
    /// </summary>
    [HttpPost]
    public IActionResult CheckCoverage([FromBody] CoverageCheckRequest request)
    {
        if (string.IsNullOrEmpty(request.DrugName) && string.IsNullOrEmpty(request.RxNormCode))
            return BadRequest(new { error = "drugName or rxNormCode is required" });

        var result = _formularyService.CheckCoverage(request);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/formulary/CoverageCheck?drugName=xxx&planId=xxx — GET version for quick checks
    /// </summary>
    [HttpGet]
    public IActionResult CheckCoverageGet(
        [FromQuery] string drugName,
        [FromQuery] string? rxNormCode = null,
        [FromQuery] string? planId = null)
    {
        if (string.IsNullOrEmpty(drugName) && string.IsNullOrEmpty(rxNormCode))
            return BadRequest(new { error = "drugName or rxNormCode is required" });

        var request = new CoverageCheckRequest
        {
            DrugName = drugName ?? "",
            RxNormCode = rxNormCode,
            PlanId = planId
        };
        var result = _formularyService.CheckCoverage(request);
        return Ok(result);
    }
}
