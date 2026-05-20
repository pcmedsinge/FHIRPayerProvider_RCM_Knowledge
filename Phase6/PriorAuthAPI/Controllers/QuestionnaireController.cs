using Microsoft.AspNetCore.Mvc;
using PriorAuthAPI.Models;
using PriorAuthAPI.Services;

namespace PriorAuthAPI.Controllers;

/// <summary>
/// DTR Questionnaire endpoints — Documentation Templates and Rules.
/// Serves documentation forms that providers fill out during prior auth.
/// </summary>
[ApiController]
[Route("api/dtr/[controller]")]
public class QuestionnaireController : ControllerBase
{
    private readonly IQuestionnaireService _questionnaireService;

    public QuestionnaireController(IQuestionnaireService questionnaireService)
    {
        _questionnaireService = questionnaireService;
    }

    /// <summary>
    /// GET /api/dtr/Questionnaire — List all DTR questionnaire templates
    /// </summary>
    [HttpGet]
    public IActionResult GetAllQuestionnaires()
    {
        var questionnaires = _questionnaireService.GetAllQuestionnaires();
        return Ok(new
        {
            total = questionnaires.Count,
            questionnaires = questionnaires.Select(q => new
            {
                q.QuestionnaireId,
                q.Title,
                q.ServiceCode,
                q.Description,
                questionCount = q.Questions.Count
            })
        });
    }

    /// <summary>
    /// GET /api/dtr/Questionnaire/{id} — Get a specific questionnaire template
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetQuestionnaire(string id)
    {
        var q = _questionnaireService.GetQuestionnaire(id);
        if (q == null)
            return NotFound(new { error = "Questionnaire not found", id });
        return Ok(q);
    }

    /// <summary>
    /// GET /api/dtr/Questionnaire/by-service/{serviceCode} — Find questionnaire by CPT/HCPCS code
    /// </summary>
    [HttpGet("by-service/{serviceCode}")]
    public IActionResult GetByServiceCode(string serviceCode)
    {
        var q = _questionnaireService.GetQuestionnaireByServiceCode(serviceCode);
        if (q == null)
            return NotFound(new { error = "No questionnaire for this service code", serviceCode });
        return Ok(q);
    }

    /// <summary>
    /// POST /api/dtr/Questionnaire/response — Submit a completed questionnaire response
    /// </summary>
    [HttpPost("response")]
    public IActionResult SubmitResponse([FromBody] DtrQuestionnaireResponse response)
    {
        if (string.IsNullOrEmpty(response.QuestionnaireId))
            return BadRequest(new { error = "questionnaireId is required" });
        if (string.IsNullOrEmpty(response.PatientId))
            return BadRequest(new { error = "patientId is required" });

        var saved = _questionnaireService.SaveResponse(response);
        return CreatedAtAction(nameof(GetResponse), new { responseId = saved.ResponseId }, saved);
    }

    /// <summary>
    /// GET /api/dtr/Questionnaire/response/{responseId} — Get a submitted response
    /// </summary>
    [HttpGet("response/{responseId}")]
    public IActionResult GetResponse(string responseId)
    {
        var response = _questionnaireService.GetResponse(responseId);
        if (response == null)
            return NotFound(new { error = "Response not found", responseId });
        return Ok(response);
    }

    /// <summary>
    /// GET /api/dtr/Questionnaire/responses/patient/{patientId} — All responses for a patient
    /// </summary>
    [HttpGet("responses/patient/{patientId}")]
    public IActionResult GetPatientResponses(string patientId)
    {
        var responses = _questionnaireService.GetResponsesByPatient(patientId);
        return Ok(new { total = responses.Count, patientId, responses });
    }
}
