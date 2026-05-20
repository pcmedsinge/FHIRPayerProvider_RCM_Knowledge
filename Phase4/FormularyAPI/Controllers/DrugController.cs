using Microsoft.AspNetCore.Mvc;
using FormularyAPI.Services;

namespace FormularyAPI.Controllers;

/// <summary>
/// Search and retrieve formulary drug information.
/// Maps to DaVinci FormularyDrug (MedicationKnowledge) and FormularyItem (Basic).
/// </summary>
[ApiController]
[Route("api/formulary/[controller]")]
public class DrugController : ControllerBase
{
    private readonly IFormularyService _formularyService;

    public DrugController(IFormularyService formularyService)
    {
        _formularyService = formularyService;
    }

    /// <summary>
    /// GET /api/formulary/Drug?name=xxx&tier=xxx&planId=xxx&therapeuticClass=xxx&stepTherapy=true|false&requiresPriorAuth=true|false
    /// Search the formulary for drugs
    /// </summary>
    [HttpGet]
    public IActionResult SearchDrugs(
        [FromQuery] string? name = null,
        [FromQuery] string? tier = null,
        [FromQuery] string? planId = null,
        [FromQuery] string? therapeuticClass = null,
        [FromQuery] bool? stepTherapy = null,
        [FromQuery] bool? requiresPriorAuth = null)
    {
        var drugs = _formularyService.SearchDrugs(name, tier, planId, therapeuticClass, stepTherapy, requiresPriorAuth);
        return Ok(new
        {
            total = drugs.Count,
            results = drugs
        });
    }

    /// <summary>
    /// GET /api/formulary/Drug/{drugId} — Get specific drug details
    /// </summary>
    [HttpGet("{drugId}")]
    public IActionResult GetDrug(string drugId)
    {
        var drug = _formularyService.GetDrug(drugId);
        if (drug == null)
            return NotFound(new { error = "Drug not found", drugId });
        return Ok(drug);
    }

    /// <summary>
    /// GET /api/formulary/Drug/{drugId}/alternatives — Get lower-cost alternatives
    /// </summary>
    [HttpGet("{drugId}/alternatives")]
    public IActionResult GetAlternatives(string drugId)
    {
        var drug = _formularyService.GetDrug(drugId);
        if (drug == null)
            return NotFound(new { error = "Drug not found", drugId });

        var alternatives = _formularyService.GetAlternatives(drugId);
        return Ok(new
        {
            originalDrug = drug.DrugName,
            originalTier = drug.DrugTier,
            originalCopay = drug.Copay,
            alternatives = alternatives.Select(a => new
            {
                a.DrugId,
                a.DrugName,
                a.GenericName,
                a.DrugTier,
                a.Copay,
                a.CoinsurancePercent,
                potentialSavings = drug.Copay - a.Copay
            })
        });
    }
}
