using PriorAuthAPI.Models;

namespace PriorAuthAPI.Services;

/// <summary>
/// DTR Questionnaire service — serves documentation templates for prior auth.
/// Maps to Da Vinci DTR (Documentation Templates and Rules).
/// </summary>
public interface IQuestionnaireService
{
    List<DtrQuestionnaire> GetAllQuestionnaires();
    DtrQuestionnaire? GetQuestionnaire(string id);
    DtrQuestionnaire? GetQuestionnaireByServiceCode(string serviceCode);
    DtrQuestionnaireResponse SaveResponse(DtrQuestionnaireResponse response);
    DtrQuestionnaireResponse? GetResponse(string responseId);
    List<DtrQuestionnaireResponse> GetResponsesByPatient(string patientId);
}

public class QuestionnaireService : IQuestionnaireService
{
    private readonly List<DtrQuestionnaire> _questionnaires;
    private readonly List<DtrQuestionnaireResponse> _responses = new();

    public QuestionnaireService()
    {
        _questionnaires = InitializeQuestionnaires();
    }

    public List<DtrQuestionnaire> GetAllQuestionnaires() => _questionnaires;

    public DtrQuestionnaire? GetQuestionnaire(string id) =>
        _questionnaires.FirstOrDefault(q => q.QuestionnaireId.Equals(id, StringComparison.OrdinalIgnoreCase));

    public DtrQuestionnaire? GetQuestionnaireByServiceCode(string serviceCode) =>
        _questionnaires.FirstOrDefault(q => q.ServiceCode.Equals(serviceCode, StringComparison.OrdinalIgnoreCase));

    public DtrQuestionnaireResponse SaveResponse(DtrQuestionnaireResponse response)
    {
        response.ResponseId = Guid.NewGuid().ToString("N")[..12];
        response.Authored = DateTime.UtcNow;
        _responses.Add(response);
        return response;
    }

    public DtrQuestionnaireResponse? GetResponse(string responseId) =>
        _responses.FirstOrDefault(r => r.ResponseId == responseId);

    public List<DtrQuestionnaireResponse> GetResponsesByPatient(string patientId) =>
        _responses.Where(r => r.PatientId == patientId).ToList();

    private List<DtrQuestionnaire> InitializeQuestionnaires() => new()
    {
        // MRI Brain
        new()
        {
            QuestionnaireId = "DTR-Q-70553",
            Title = "MRI Brain — Prior Authorization Documentation",
            ServiceCode = "70553",
            Description = "Documentation required for MRI Brain with and without contrast",
            Questions = new()
            {
                new() { LinkId = "1", Text = "Clinical indication for MRI", Type = "string", Required = true, AutoPopulateFrom = "Condition.code.text" },
                new() { LinkId = "2", Text = "Duration of symptoms", Type = "choice", Required = true, AnswerOptions = new() { new() { Value = "< 2 weeks", Display = "Less than 2 weeks" }, new() { Value = "2-6 weeks", Display = "2 to 6 weeks" }, new() { Value = "> 6 weeks", Display = "More than 6 weeks" } } },
                new() { LinkId = "3", Text = "Previous imaging performed?", Type = "boolean", Required = true },
                new() { LinkId = "4", Text = "Previous imaging results (if any)", Type = "string", Required = false },
                new() { LinkId = "5", Text = "Neurological exam findings", Type = "string", Required = true },
                new() { LinkId = "6", Text = "Red flag symptoms present? (sudden severe headache, focal deficit, seizure)", Type = "boolean", Required = true },
                new() { LinkId = "7", Text = "Date of last neurology consultation", Type = "date", Required = false }
            }
        },
        // Total Knee Replacement
        new()
        {
            QuestionnaireId = "DTR-Q-27447",
            Title = "Total Knee Replacement — Prior Authorization Documentation",
            ServiceCode = "27447",
            Description = "Documentation required for total knee arthroplasty",
            Questions = new()
            {
                new() { LinkId = "1", Text = "Diagnosis (ICD-10 code and description)", Type = "string", Required = true, AutoPopulateFrom = "Condition.code" },
                new() { LinkId = "2", Text = "Duration of knee symptoms", Type = "choice", Required = true, AnswerOptions = new() { new() { Value = "< 3 months", Display = "Less than 3 months" }, new() { Value = "3-6 months", Display = "3 to 6 months" }, new() { Value = "6-12 months", Display = "6 to 12 months" }, new() { Value = "> 12 months", Display = "More than 12 months" } } },
                new() { LinkId = "3", Text = "X-ray findings (Kellgren-Lawrence grade)", Type = "choice", Required = true, AnswerOptions = new() { new() { Value = "grade-1", Display = "Grade 1 - Doubtful" }, new() { Value = "grade-2", Display = "Grade 2 - Minimal" }, new() { Value = "grade-3", Display = "Grade 3 - Moderate" }, new() { Value = "grade-4", Display = "Grade 4 - Severe" } } },
                new() { LinkId = "4", Text = "Conservative treatment tried? (check all that apply)", Type = "string", Required = true },
                new() { LinkId = "5", Text = "Physical therapy completed? (minimum 6 weeks)", Type = "boolean", Required = true },
                new() { LinkId = "6", Text = "Number of physical therapy sessions", Type = "integer", Required = false },
                new() { LinkId = "7", Text = "Current BMI", Type = "string", Required = true, AutoPopulateFrom = "Observation.where(code='39156-5').value" },
                new() { LinkId = "8", Text = "Corticosteroid injections tried?", Type = "boolean", Required = true },
                new() { LinkId = "9", Text = "NSAID trial completed?", Type = "boolean", Required = true },
                new() { LinkId = "10", Text = "Functional limitation description", Type = "string", Required = true }
            }
        },
        // Ozempic
        new()
        {
            QuestionnaireId = "DTR-Q-1991302",
            Title = "Semaglutide (Ozempic) — Prior Authorization Documentation",
            ServiceCode = "1991302",
            Description = "Documentation for GLP-1 receptor agonist authorization",
            Questions = new()
            {
                new() { LinkId = "1", Text = "Diagnosis", Type = "choice", Required = true, AnswerOptions = new() { new() { Value = "E11", Display = "Type 2 Diabetes" }, new() { Value = "E66", Display = "Obesity" } } },
                new() { LinkId = "2", Text = "Most recent HbA1c value", Type = "string", Required = true, AutoPopulateFrom = "Observation.where(code='4548-4').value" },
                new() { LinkId = "3", Text = "Date of HbA1c test", Type = "date", Required = true },
                new() { LinkId = "4", Text = "Metformin trial completed? (minimum 3 months)", Type = "boolean", Required = true },
                new() { LinkId = "5", Text = "Metformin dose and duration", Type = "string", Required = false },
                new() { LinkId = "6", Text = "Reason if Metformin not tolerated", Type = "string", Required = false },
                new() { LinkId = "7", Text = "Current BMI", Type = "string", Required = true },
                new() { LinkId = "8", Text = "Other diabetes medications tried", Type = "string", Required = false }
            }
        },
        // CPAP
        new()
        {
            QuestionnaireId = "DTR-Q-E0601",
            Title = "CPAP Device — Prior Authorization Documentation",
            ServiceCode = "E0601",
            Description = "Documentation for CPAP/BiPAP device authorization",
            Questions = new()
            {
                new() { LinkId = "1", Text = "Sleep study type", Type = "choice", Required = true, AnswerOptions = new() { new() { Value = "PSG", Display = "In-lab polysomnography (PSG)" }, new() { Value = "HST", Display = "Home sleep test (HST)" } } },
                new() { LinkId = "2", Text = "Sleep study date", Type = "date", Required = true },
                new() { LinkId = "3", Text = "AHI (Apnea-Hypopnea Index) score", Type = "string", Required = true },
                new() { LinkId = "4", Text = "AHI >= 15 events/hour?", Type = "boolean", Required = true },
                new() { LinkId = "5", Text = "If AHI 5-14: documented symptoms? (excessive daytime sleepiness, impaired cognition, mood disorders, insomnia, hypertension, ischemic heart disease, stroke)", Type = "string", Required = false },
                new() { LinkId = "6", Text = "Face-to-face evaluation completed?", Type = "boolean", Required = true },
                new() { LinkId = "7", Text = "Date of face-to-face evaluation", Type = "date", Required = false },
                new() { LinkId = "8", Text = "Compliance monitoring plan in place?", Type = "boolean", Required = true }
            }
        },
        // CT Abdomen
        new()
        {
            QuestionnaireId = "DTR-Q-74177",
            Title = "CT Abdomen/Pelvis — Prior Authorization Documentation",
            ServiceCode = "74177",
            Description = "Documentation for abdominal CT with contrast",
            Questions = new()
            {
                new() { LinkId = "1", Text = "Clinical indication", Type = "string", Required = true },
                new() { LinkId = "2", Text = "Relevant lab results", Type = "string", Required = true },
                new() { LinkId = "3", Text = "Previous imaging performed for this issue?", Type = "boolean", Required = true },
                new() { LinkId = "4", Text = "Ultrasound tried first?", Type = "boolean", Required = true },
                new() { LinkId = "5", Text = "Is this for emergency/acute presentation?", Type = "boolean", Required = true }
            }
        }
    };
}
