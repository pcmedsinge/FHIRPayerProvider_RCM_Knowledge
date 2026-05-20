using Microsoft.AspNetCore.Mvc;
using PDexAPI.Models;
using PDexAPI.Services;

namespace PDexAPI.Controllers;

/// <summary>
/// Data exchange orchestration — initiate and manage payer-to-payer data transfers.
/// Requires active consent before exchange can proceed.
/// </summary>
[ApiController]
[Route("api/pdex/[controller]")]
public class ExchangeController : ControllerBase
{
    private readonly IDataExchangeService _exchangeService;
    private readonly IConsentService _consentService;

    public ExchangeController(IDataExchangeService exchangeService, IConsentService consentService)
    {
        _exchangeService = exchangeService;
        _consentService = consentService;
    }

    /// <summary>
    /// POST /api/pdex/Exchange — Initiate a data exchange (requires active consent)
    /// </summary>
    [HttpPost]
    public IActionResult InitiateExchange([FromBody] ExchangeRequest request)
    {
        if (string.IsNullOrEmpty(request.ConsentId))
            return BadRequest(new { error = "consentId is required" });

        var consent = _consentService.GetConsent(request.ConsentId);
        if (consent == null)
            return NotFound(new { error = "Consent not found", consentId = request.ConsentId });

        if (consent.Status != "active")
            return BadRequest(new { error = "Consent must be active before data exchange", currentStatus = consent.Status });

        if (consent.ExpirationDate.HasValue && consent.ExpirationDate < DateTime.UtcNow)
            return BadRequest(new { error = "Consent has expired", expirationDate = consent.ExpirationDate });

        var job = _exchangeService.InitiateExchange(request, consent);
        return CreatedAtAction(nameof(GetJob), new { jobId = job.JobId }, job);
    }

    /// <summary>
    /// POST /api/pdex/Exchange/{jobId}/execute — Execute the data exchange (pulls from HAPI FHIR)
    /// </summary>
    [HttpPost("{jobId}/execute")]
    public async Task<IActionResult> ExecuteExchange(string jobId)
    {
        try
        {
            var job = await _exchangeService.ExecuteExchangeAsync(jobId);
            return Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/pdex/Exchange/{jobId} — Get exchange job status
    /// </summary>
    [HttpGet("{jobId}")]
    public IActionResult GetJob(string jobId)
    {
        var job = _exchangeService.GetJob(jobId);
        if (job == null)
            return NotFound(new { error = "Exchange job not found", jobId });
        return Ok(job);
    }

    /// <summary>
    /// GET /api/pdex/Exchange — List all exchange jobs (admin)
    /// </summary>
    [HttpGet]
    public IActionResult GetAllJobs()
    {
        var jobs = _exchangeService.GetAllJobs();
        return Ok(new { total = jobs.Count, jobs });
    }

    /// <summary>
    /// GET /api/pdex/Exchange/patient/{patientId} — Get patient's exchange jobs
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public IActionResult GetPatientJobs(string patientId)
    {
        var jobs = _exchangeService.GetJobsByPatient(patientId);
        return Ok(new { total = jobs.Count, patientId, jobs });
    }

    /// <summary>
    /// PUT /api/pdex/Exchange/{jobId}/cancel — Cancel an exchange job
    /// </summary>
    [HttpPut("{jobId}/cancel")]
    public IActionResult CancelJob(string jobId)
    {
        var job = _exchangeService.CancelJob(jobId);
        if (job == null)
            return BadRequest(new { error = "Cannot cancel. Job not found or already completed/cancelled.", jobId });
        return Ok(job);
    }

    /// <summary>
    /// GET /api/pdex/Exchange/{jobId}/provenance — Get provenance records for an exchange
    /// </summary>
    [HttpGet("{jobId}/provenance")]
    public IActionResult GetProvenance(string jobId)
    {
        var job = _exchangeService.GetJob(jobId);
        if (job == null)
            return NotFound(new { error = "Exchange job not found", jobId });

        return Ok(new
        {
            jobId = job.JobId,
            total = job.ProvenanceRecords.Count,
            provenanceRecords = job.ProvenanceRecords
        });
    }

    /// <summary>
    /// GET /api/pdex/Exchange/{jobId}/resources — Get exchanged resources summary
    /// </summary>
    [HttpGet("{jobId}/resources")]
    public IActionResult GetExchangedResources(string jobId)
    {
        var job = _exchangeService.GetJob(jobId);
        if (job == null)
            return NotFound(new { error = "Exchange job not found", jobId });

        var summary = job.ExchangedResources
            .GroupBy(r => r.ResourceType)
            .Select(g => new { resourceType = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToList();

        return Ok(new
        {
            jobId = job.JobId,
            totalResources = job.TotalResourcesTransferred,
            byType = summary,
            resources = job.ExchangedResources
        });
    }
}
