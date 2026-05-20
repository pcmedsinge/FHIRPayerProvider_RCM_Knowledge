using Microsoft.AspNetCore.Mvc;
using BulkDataAPI.Models;
using BulkDataAPI.Services;

namespace BulkDataAPI.Controllers;

/// <summary>
/// Bulk Data Export endpoints — implements FHIR Bulk Data Access IG.
/// Supports system-level, patient-level, and group-level $export operations.
/// </summary>
[ApiController]
[Route("api/bulk/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IBulkExportService _exportService;

    public ExportController(IBulkExportService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// POST /api/bulk/Export/$export — Initiate system-level bulk export
    /// Maps to: GET [fhir-base]/$export
    /// </summary>
    [HttpPost("$export")]
    public IActionResult InitiateSystemExport([FromBody] ExportRequest? request = null)
    {
        var exportRequest = request ?? new ExportRequest { ExportType = "system" };
        exportRequest.ExportType = "system";

        var job = _exportService.InitiateExport(exportRequest);

        // FHIR Bulk Data spec: return 202 Accepted with Content-Location header
        Response.Headers.Append("Content-Location", $"/api/bulk/Export/{job.JobId}/status");
        return Accepted(new { jobId = job.JobId, message = "Export job accepted", statusUrl = $"/api/bulk/Export/{job.JobId}/status" });
    }

    /// <summary>
    /// POST /api/bulk/Export/Patient/$export — Initiate patient-level bulk export
    /// Maps to: GET [fhir-base]/Patient/$export
    /// </summary>
    [HttpPost("Patient/$export")]
    public IActionResult InitiatePatientExport([FromBody] ExportRequest? request = null)
    {
        var exportRequest = request ?? new ExportRequest { ExportType = "patient" };
        exportRequest.ExportType = "patient";

        var job = _exportService.InitiateExport(exportRequest);

        Response.Headers.Append("Content-Location", $"/api/bulk/Export/{job.JobId}/status");
        return Accepted(new { jobId = job.JobId, message = "Patient export accepted", statusUrl = $"/api/bulk/Export/{job.JobId}/status" });
    }

    /// <summary>
    /// POST /api/bulk/Export/Group/{groupId}/$export — Initiate group-level bulk export
    /// Maps to: GET [fhir-base]/Group/{id}/$export
    /// </summary>
    [HttpPost("Group/{groupId}/$export")]
    public IActionResult InitiateGroupExport(string groupId, [FromBody] ExportRequest? request = null)
    {
        var exportRequest = request ?? new ExportRequest();
        exportRequest.ExportType = "group";
        exportRequest.GroupId = groupId;

        var job = _exportService.InitiateExport(exportRequest);

        Response.Headers.Append("Content-Location", $"/api/bulk/Export/{job.JobId}/status");
        return Accepted(new { jobId = job.JobId, groupId, message = "Group export accepted", statusUrl = $"/api/bulk/Export/{job.JobId}/status" });
    }

    /// <summary>
    /// POST /api/bulk/Export/{jobId}/execute — Execute the export job (triggers HAPI FHIR pull)
    /// Non-spec convenience endpoint for demo/learning.
    /// </summary>
    [HttpPost("{jobId}/execute")]
    public async Task<IActionResult> ExecuteExport(string jobId)
    {
        try
        {
            var job = await _exportService.ExecuteExportAsync(jobId);
            return Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/bulk/Export/{jobId}/status — Check export job status
    /// Maps to: GET Content-Location from $export response
    /// Returns 202 if in-progress, 200 if complete with manifest
    /// </summary>
    [HttpGet("{jobId}/status")]
    public IActionResult GetJobStatus(string jobId)
    {
        var job = _exportService.GetJob(jobId);
        if (job == null)
            return NotFound(new { error = "Export job not found", jobId });

        if (job.Status == "in-progress" || job.Status == "queued")
        {
            Response.Headers.Append("X-Progress", $"{job.ProgressPercent}%");
            Response.Headers.Append("Retry-After", "5");
            return StatusCode(202, new { status = job.Status, progress = $"{job.ProgressPercent}%" });
        }

        if (job.Status == "completed")
        {
            // Return bulk data manifest (per spec)
            return Ok(new
            {
                transactionTime = job.TransactionTime,
                request = $"$export?_type={string.Join(",", job.ResourceTypes)}",
                requiresAccessToken = false,
                output = job.Output,
                error = job.Errors.Count > 0 ? job.Errors : null,
                summary = job.Summary
            });
        }

        return Ok(new { status = job.Status, error = job.ErrorMessage });
    }

    /// <summary>
    /// GET /api/bulk/Export/{jobId}/download/{resourceType} — Download NDJSON file
    /// Returns actual NDJSON content
    /// </summary>
    [HttpGet("{jobId}/download/{resourceType}")]
    public IActionResult DownloadNdjson(string jobId, string resourceType)
    {
        var job = _exportService.GetJob(jobId);
        if (job == null)
            return NotFound(new { error = "Export job not found" });
        if (job.Status != "completed")
            return BadRequest(new { error = "Export not yet completed", status = job.Status });

        var content = _exportService.GetNdjsonContent(jobId, resourceType);
        if (content == null)
            return NotFound(new { error = $"No {resourceType} data in this export" });

        return Content(content, "application/fhir+ndjson");
    }

    /// <summary>
    /// GET /api/bulk/Export/{jobId}/analytics — Get analytics for exported data
    /// Bonus feature: Analyze exported data for insights
    /// </summary>
    [HttpGet("{jobId}/analytics")]
    public IActionResult GetAnalytics(string jobId)
    {
        var job = _exportService.GetJob(jobId);
        if (job == null)
            return NotFound(new { error = "Export job not found" });
        if (job.Status != "completed")
            return BadRequest(new { error = "Export not yet completed" });

        var analytics = _exportService.GetAnalytics(jobId);
        if (analytics == null)
            return NotFound(new { error = "No analytics data available" });

        return Ok(analytics);
    }

    /// <summary>
    /// DELETE /api/bulk/Export/{jobId} — Delete an export job and its data
    /// Maps to: DELETE Content-Location
    /// </summary>
    [HttpDelete("{jobId}")]
    public IActionResult DeleteJob(string jobId)
    {
        var job = _exportService.DeleteJob(jobId);
        if (job == null)
            return NotFound(new { error = "Export job not found" });
        return Ok(new { message = "Export job deleted", jobId });
    }

    /// <summary>
    /// PUT /api/bulk/Export/{jobId}/cancel — Cancel an in-progress export
    /// </summary>
    [HttpPut("{jobId}/cancel")]
    public IActionResult CancelJob(string jobId)
    {
        var job = _exportService.CancelJob(jobId);
        if (job == null)
            return BadRequest(new { error = "Cannot cancel. Job not found or already completed/cancelled." });
        return Ok(new { message = "Export job cancelled", jobId, status = job.Status });
    }

    /// <summary>
    /// GET /api/bulk/Export — List all export jobs (admin)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllJobs()
    {
        var jobs = _exportService.GetAllJobs();
        return Ok(new { total = jobs.Count, jobs });
    }
}
