using Microsoft.AspNetCore.Mvc;
using CRDService.Models;
using CRDService.Services;

namespace CRDService.Controllers;

/// <summary>
/// CDS Hooks Discovery and Hook Handlers per the CDS Hooks specification.
/// This is the core of the CRD (Coverage Requirements Discovery) service.
/// 
/// CDS Hooks spec: https://cds-hooks.hl7.org/
/// Da Vinci CRD: http://hl7.org/fhir/us/davinci-crd/
/// </summary>
[ApiController]
public class CdsHooksController : ControllerBase
{
    private readonly ICoverageRulesService _rulesService;
    private readonly ILogger<CdsHooksController> _logger;

    public CdsHooksController(ICoverageRulesService rulesService, ILogger<CdsHooksController> logger)
    {
        _rulesService = rulesService;
        _logger = logger;
    }

    /// <summary>
    /// GET /cds-services — CDS Hooks Discovery endpoint
    /// EHRs call this to discover what hooks this CRD service supports.
    /// </summary>
    [HttpGet("cds-services")]
    public IActionResult Discovery()
    {
        var response = new CdsDiscoveryResponse
        {
            Services = new List<CdsServiceDefinition>
            {
                new()
                {
                    Hook = "order-select",
                    Title = "Payer Coverage Requirements — Order Select",
                    Description = "Checks coverage requirements when a clinician selects an order (medication, procedure, imaging). Returns prior auth needs, documentation requirements, and alternatives.",
                    Id = "crd-order-select",
                    Prefetch = new CdsPrefetch
                    {
                        Patient = "Patient/{{context.patientId}}",
                        Coverage = "Coverage?beneficiary=Patient/{{context.patientId}}&status=active"
                    }
                },
                new()
                {
                    Hook = "order-sign",
                    Title = "Payer Coverage Requirements — Order Sign",
                    Description = "Final check before order is signed. Validates all requirements are met.",
                    Id = "crd-order-sign",
                    Prefetch = new CdsPrefetch
                    {
                        Patient = "Patient/{{context.patientId}}",
                        Coverage = "Coverage?beneficiary=Patient/{{context.patientId}}&status=active",
                        Conditions = "Condition?patient={{context.patientId}}&clinical-status=active"
                    }
                },
                new()
                {
                    Hook = "appointment-book",
                    Title = "Payer Coverage Requirements — Appointment Book",
                    Description = "Checks network status and coverage when scheduling appointments.",
                    Id = "crd-appointment-book",
                    Prefetch = new CdsPrefetch
                    {
                        Patient = "Patient/{{context.patientId}}",
                        Coverage = "Coverage?beneficiary=Patient/{{context.patientId}}&status=active"
                    }
                }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// POST /cds-services/crd-order-select — Handle order-select hook
    /// Called when clinician selects an order item in the EHR.
    /// </summary>
    [HttpPost("cds-services/crd-order-select")]
    public IActionResult OrderSelect([FromBody] CdsHookRequest request)
    {
        _logger.LogInformation("order-select hook called for patient {PatientId}", request.Context?.PatientId);
        var response = _rulesService.EvaluateOrderSelect(request);
        return Ok(response);
    }

    /// <summary>
    /// POST /cds-services/crd-order-sign — Handle order-sign hook
    /// Called when clinician signs/finalizes orders.
    /// </summary>
    [HttpPost("cds-services/crd-order-sign")]
    public IActionResult OrderSign([FromBody] CdsHookRequest request)
    {
        _logger.LogInformation("order-sign hook called for patient {PatientId}", request.Context?.PatientId);
        var response = _rulesService.EvaluateOrderSign(request);
        return Ok(response);
    }

    /// <summary>
    /// POST /cds-services/crd-appointment-book — Handle appointment-book hook
    /// Called when scheduling an appointment.
    /// </summary>
    [HttpPost("cds-services/crd-appointment-book")]
    public IActionResult AppointmentBook([FromBody] CdsHookRequest request)
    {
        _logger.LogInformation("appointment-book hook called for patient {PatientId}", request.Context?.PatientId);
        var response = _rulesService.EvaluateAppointmentBook(request);
        return Ok(response);
    }
}
