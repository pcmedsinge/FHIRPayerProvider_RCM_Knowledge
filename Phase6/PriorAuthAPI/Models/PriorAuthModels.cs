namespace PriorAuthAPI.Models;

/// <summary>
/// Prior authorization request model.
/// Maps to FHIR Claim resource used in Da Vinci PAS.
/// </summary>
public class PriorAuthRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string PatientId { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string ProviderId { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string ServiceCode { get; set; } = "";
    public string ServiceCodeSystem { get; set; } = "CPT"; // CPT, HCPCS, RxNorm
    public string ServiceDescription { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public string DiagnosisCode { get; set; } = "";
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? ServiceDate { get; set; }
    public string Urgency { get; set; } = "routine"; // routine, urgent, stat
    public List<string> SupportingDocuments { get; set; } = new();
    public string? QuestionnaireResponseId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Prior authorization response/decision.
/// Maps to FHIR ClaimResponse resource.
/// </summary>
public class PriorAuthResponse
{
    public string AuthorizationId { get; set; } = "";
    public string RequestId { get; set; } = "";
    public string Status { get; set; } = "pending"; // pending, approved, denied, pended-for-review, partial, cancelled
    public string? ReviewOutcome { get; set; } // complete, error, partial, queued
    public string? DenialReason { get; set; }
    public DateTime DecisionDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get; set; }
    public string? ApprovedServiceCode { get; set; }
    public int? ApprovedQuantity { get; set; }
    public string? ApprovedProvider { get; set; }
    public string? ReviewNotes { get; set; }
    public List<PriorAuthStatusEntry> StatusHistory { get; set; } = new();
}

public class PriorAuthStatusEntry
{
    public string Status { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }
}

/// <summary>
/// DTR Questionnaire template definition.
/// </summary>
public class DtrQuestionnaire
{
    public string QuestionnaireId { get; set; } = "";
    public string Title { get; set; } = "";
    public string ServiceCode { get; set; } = "";
    public string Description { get; set; } = "";
    public List<DtrQuestion> Questions { get; set; } = new();
}

public class DtrQuestion
{
    public string LinkId { get; set; } = "";
    public string Text { get; set; } = "";
    public string Type { get; set; } = "string"; // string, boolean, date, choice, integer
    public bool Required { get; set; }
    public List<DtrAnswerOption>? AnswerOptions { get; set; }
    public string? AutoPopulateFrom { get; set; } // FHIR path for auto-population
}

public class DtrAnswerOption
{
    public string Value { get; set; } = "";
    public string Display { get; set; } = "";
}

/// <summary>
/// Completed questionnaire response from the provider.
/// </summary>
public class DtrQuestionnaireResponse
{
    public string ResponseId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string QuestionnaireId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public DateTime Authored { get; set; } = DateTime.UtcNow;
    public List<DtrAnswer> Answers { get; set; } = new();
}

public class DtrAnswer
{
    public string LinkId { get; set; } = "";
    public string QuestionText { get; set; } = "";
    public string Value { get; set; } = "";
}
