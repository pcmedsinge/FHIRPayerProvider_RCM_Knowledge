using Microsoft.AspNetCore.Mvc;
using BulkDataAPI.Models;
using BulkDataAPI.Services;

namespace BulkDataAPI.Controllers;

/// <summary>
/// Patient Group management for group-level $export.
/// Maps to FHIR Group resource.
/// </summary>
[ApiController]
[Route("api/bulk/[controller]")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    /// <summary>
    /// GET /api/bulk/Group — List all patient groups
    /// </summary>
    [HttpGet]
    public IActionResult GetAllGroups()
    {
        var groups = _groupService.GetAllGroups();
        return Ok(new { total = groups.Count, groups });
    }

    /// <summary>
    /// GET /api/bulk/Group/{groupId} — Get group details
    /// </summary>
    [HttpGet("{groupId}")]
    public IActionResult GetGroup(string groupId)
    {
        var group = _groupService.GetGroup(groupId);
        if (group == null)
            return NotFound(new { error = "Group not found", groupId });
        return Ok(group);
    }

    /// <summary>
    /// POST /api/bulk/Group — Create a new patient group
    /// </summary>
    [HttpPost]
    public IActionResult CreateGroup([FromBody] CreateGroupRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
            return BadRequest(new { error = "name is required" });
        if (request.PatientIds == null || request.PatientIds.Count == 0)
            return BadRequest(new { error = "patientIds is required and must not be empty" });

        var group = _groupService.CreateGroup(request.Name, request.Description ?? "", request.PatientIds);
        return CreatedAtAction(nameof(GetGroup), new { groupId = group.GroupId }, group);
    }

    /// <summary>
    /// PUT /api/bulk/Group/{groupId}/members/add — Add members to a group
    /// </summary>
    [HttpPut("{groupId}/members/add")]
    public IActionResult AddMembers(string groupId, [FromBody] MemberUpdateRequest request)
    {
        if (request.PatientIds == null || request.PatientIds.Count == 0)
            return BadRequest(new { error = "patientIds is required" });

        var group = _groupService.AddMembers(groupId, request.PatientIds);
        if (group == null)
            return NotFound(new { error = "Group not found", groupId });
        return Ok(group);
    }

    /// <summary>
    /// PUT /api/bulk/Group/{groupId}/members/remove — Remove members from a group
    /// </summary>
    [HttpPut("{groupId}/members/remove")]
    public IActionResult RemoveMembers(string groupId, [FromBody] MemberUpdateRequest request)
    {
        if (request.PatientIds == null || request.PatientIds.Count == 0)
            return BadRequest(new { error = "patientIds is required" });

        var group = _groupService.RemoveMembers(groupId, request.PatientIds);
        if (group == null)
            return NotFound(new { error = "Group not found", groupId });
        return Ok(group);
    }

    /// <summary>
    /// DELETE /api/bulk/Group/{groupId} — Delete a group
    /// </summary>
    [HttpDelete("{groupId}")]
    public IActionResult DeleteGroup(string groupId)
    {
        var deleted = _groupService.DeleteGroup(groupId);
        if (!deleted)
            return NotFound(new { error = "Group not found", groupId });
        return Ok(new { message = "Group deleted", groupId });
    }
}

// Request models for Group endpoints
public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> PatientIds { get; set; } = new();
}

public class MemberUpdateRequest
{
    public List<string> PatientIds { get; set; } = new();
}
