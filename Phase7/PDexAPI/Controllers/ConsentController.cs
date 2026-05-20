using Microsoft.AspNetCore.Mvc;
using PDexAPI.Models;
using PDexAPI.Services;

namespace PDexAPI.Controllers;

/// <summary>
/// Consent management for payer-to-payer data exchange.
/// Patients must consent before data can be transferred between payers.
/// </summary>
[ApiController]
[Route("api/pdex/[controller]")]
public class ConsentController : ControllerBase
{
    private readonly IConsentService _consentService;

    public ConsentController(IConsentService consentService)
    {
        _consentService = consentService;
    }

    /// <summary>
    /// POST /api/pdex/Consent — Create a new data exchange consent
    /// </summary>
    [HttpPost]
    public IActionResult CreateConsent([FromBody] ConsentRequest request)
    {
        if (string.IsNullOrEmpty(request.PatientId))
            return BadRequest(new { error = "patientId is required" });
        if (string.IsNullOrEmpty(request.SourcePayerId))
            return BadRequest(new { error = "sourcePayerId is required" });
        if (string.IsNullOrEmpty(request.TargetPayerId))
            return BadRequest(new { error = "targetPayerId is required" });
        if (request.SourcePayerId == request.TargetPayerId)
            return BadRequest(new { error = "Source and target payers must be different" });

        var consent = _consentService.CreateConsent(request);
        return CreatedAtAction(nameof(GetConsent), new { consentId = consent.ConsentId }, consent);
    }

    /// <summary>
    /// GET /api/pdex/Consent/{consentId} — Get consent by ID
    /// </summary>
    [HttpGet("{consentId}")]
    public IActionResult GetConsent(string consentId)
    {
        var consent = _consentService.GetConsent(consentId);
        if (consent == null)
            return NotFound(new { error = "Consent not found", consentId });
        return Ok(consent);
    }

    /// <summary>
    /// PUT /api/pdex/Consent/{consentId}/activate — Patient signs/activates consent
    /// </summary>
    [HttpPut("{consentId}/activate")]
    public IActionResult ActivateConsent(string consentId)
    {
        var consent = _consentService.ActivateConsent(consentId);
        if (consent == null)
            return BadRequest(new { error = "Cannot activate. Consent not found or not in draft status.", consentId });
        return Ok(consent);
    }

    /// <summary>
    /// PUT /api/pdex/Consent/{consentId}/revoke — Patient revokes consent
    /// </summary>
    [HttpPut("{consentId}/revoke")]
    public IActionResult RevokeConsent(string consentId)
    {
        var consent = _consentService.RevokeConsent(consentId);
        if (consent == null)
            return BadRequest(new { error = "Cannot revoke. Consent not found or already revoked.", consentId });
        return Ok(consent);
    }

    /// <summary>
    /// GET /api/pdex/Consent/patient/{patientId} — Get patient's consents
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public IActionResult GetPatientConsents(string patientId)
    {
        var consents = _consentService.GetConsentsByPatient(patientId);
        return Ok(new { total = consents.Count, patientId, consents });
    }

    /// <summary>
    /// GET /api/pdex/Consent — List all consents (admin)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllConsents()
    {
        var consents = _consentService.GetAllConsents();
        return Ok(new { total = consents.Count, consents });
    }
}
