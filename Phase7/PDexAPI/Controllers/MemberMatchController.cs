using Microsoft.AspNetCore.Mvc;
using PDexAPI.Models;
using PDexAPI.Services;

namespace PDexAPI.Controllers;

/// <summary>
/// $member-match — Identify a patient across payer systems.
/// Critical for payer-to-payer exchange when a member switches plans.
/// </summary>
[ApiController]
[Route("api/pdex/[controller]")]
public class MemberMatchController : ControllerBase
{
    private readonly IMemberMatchService _memberMatchService;

    public MemberMatchController(IMemberMatchService memberMatchService)
    {
        _memberMatchService = memberMatchService;
    }

    /// <summary>
    /// POST /api/pdex/MemberMatch — Perform $member-match operation
    /// </summary>
    [HttpPost]
    public IActionResult MatchMember([FromBody] MemberMatchRequest request)
    {
        if (string.IsNullOrEmpty(request.MemberLastName))
            return BadRequest(new { error = "memberLastName is required" });
        if (string.IsNullOrEmpty(request.NewPayerId))
            return BadRequest(new { error = "newPayerId is required" });

        var result = _memberMatchService.MatchMember(request);

        if (result.Matched)
            return Ok(result);

        return Ok(result); // Return match result even if not matched (with confidence info)
    }

    /// <summary>
    /// GET /api/pdex/MemberMatch/members — List known members (admin/debug)
    /// </summary>
    [HttpGet("members")]
    public IActionResult GetKnownMembers()
    {
        var members = _memberMatchService.GetKnownMembers();
        return Ok(new { total = members.Count, members });
    }

    /// <summary>
    /// GET /api/pdex/MemberMatch/members/{patientId} — Get member data summary
    /// </summary>
    [HttpGet("members/{patientId}")]
    public IActionResult GetMemberSummary(string patientId)
    {
        var summary = _memberMatchService.GetMemberSummary(patientId);
        if (summary == null)
            return NotFound(new { error = "Member not found", patientId });
        return Ok(summary);
    }
}
