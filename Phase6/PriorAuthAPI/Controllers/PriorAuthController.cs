using Microsoft.AspNetCore.Mvc;
using PriorAuthAPI.Models;
using PriorAuthAPI.Services;

namespace PriorAuthAPI.Controllers;

/// <summary>
/// Prior Authorization Support (PAS) endpoints.
/// Submit PA requests, check status, cancel requests.
/// Maps to Da Vinci PAS $submit operation.
/// </summary>
[ApiController]
[Route("api/pas/[controller]")]
public class PriorAuthController : ControllerBase
{
    private readonly IPriorAuthService _priorAuthService;

    public PriorAuthController(IPriorAuthService priorAuthService)
    {
        _priorAuthService = priorAuthService;
    }

    /// <summary>
    /// POST /api/pas/PriorAuth/submit — Submit a prior authorization request ($submit)
    /// </summary>
    [HttpPost("submit")]
    public IActionResult Submit([FromBody] PriorAuthRequest request)
    {
        if (string.IsNullOrEmpty(request.PatientId))
            return BadRequest(new { error = "patientId is required" });
        if (string.IsNullOrEmpty(request.ServiceCode))
            return BadRequest(new { error = "serviceCode is required" });

        var response = _priorAuthService.SubmitRequest(request);
        return CreatedAtAction(nameof(GetStatus), new { authorizationId = response.AuthorizationId }, response);
    }

    /// <summary>
    /// GET /api/pas/PriorAuth/status/{authorizationId} — Check PA status ($inquiry)
    /// </summary>
    [HttpGet("status/{authorizationId}")]
    public IActionResult GetStatus(string authorizationId)
    {
        var status = _priorAuthService.GetStatus(authorizationId);
        if (status == null)
            return NotFound(new { error = "Authorization not found", authorizationId });
        return Ok(status);
    }

    /// <summary>
    /// GET /api/pas/PriorAuth/patient/{patientId} — Get all PA requests for a patient
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public IActionResult GetPatientRequests(string patientId)
    {
        var requests = _priorAuthService.GetRequestsByPatient(patientId);
        return Ok(new { total = requests.Count, patientId, requests });
    }

    /// <summary>
    /// GET /api/pas/PriorAuth — List all PA requests (admin view)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllRequests()
    {
        var requests = _priorAuthService.GetAllRequests();
        return Ok(new { total = requests.Count, requests });
    }

    /// <summary>
    /// PUT /api/pas/PriorAuth/cancel/{authorizationId} — Cancel a pending PA request
    /// </summary>
    [HttpPut("cancel/{authorizationId}")]
    public IActionResult Cancel(string authorizationId)
    {
        var result = _priorAuthService.CancelRequest(authorizationId);
        if (result == null)
            return BadRequest(new { error = "Cannot cancel. Request not found or already finalized.", authorizationId });
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/pas/PriorAuth/update/{authorizationId} — Admin: update status (simulate payer review)
    /// </summary>
    [HttpPut("update/{authorizationId}")]
    public IActionResult UpdateStatus(string authorizationId, [FromQuery] string status, [FromQuery] string? notes = null)
    {
        var validStatuses = new[] { "pending", "approved", "denied", "pended-for-review", "partial", "cancelled" };
        if (!validStatuses.Contains(status))
            return BadRequest(new { error = "Invalid status", validStatuses });

        var result = _priorAuthService.UpdateStatus(authorizationId, status, notes);
        if (result == null)
            return NotFound(new { error = "Authorization not found", authorizationId });
        return Ok(result);
    }
}
