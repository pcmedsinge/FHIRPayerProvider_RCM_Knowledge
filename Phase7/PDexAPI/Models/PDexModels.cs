namespace PDexAPI.Models;

/// <summary>
/// Consent for payer-to-payer data exchange.
/// Maps to FHIR Consent resource.
/// </summary>
public class DataExchangeConsent
{
    public string ConsentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string SourcePayerId { get; set; } = string.Empty;
    public string SourcePayerName { get; set; } = string.Empty;
    public string TargetPayerId { get; set; } = string.Empty;
    public string TargetPayerName { get; set; } = string.Empty;
    public string Status { get; set; } = "draft"; // draft, active, rejected, revoked
    public string Scope { get; set; } = "patient-privacy"; // patient-privacy
    public List<string> DataCategories { get; set; } = new(); // claims, encounters, medications, conditions, etc.
    public DateTime? ConsentDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
}

/// <summary>
/// Request to create a new data exchange consent form.
/// </summary>
public class ConsentRequest
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string SourcePayerId { get; set; } = string.Empty;
    public string TargetPayerId { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public int ExpirationDays { get; set; } = 365;
}

/// <summary>
/// $member-match request. Maps to Da Vinci HRex $member-match.
/// Identifies a patient across payer systems.
/// </summary>
public class MemberMatchRequest
{
    public string MemberFirstName { get; set; } = string.Empty;
    public string MemberLastName { get; set; } = string.Empty;
    public string MemberDateOfBirth { get; set; } = string.Empty; // YYYY-MM-DD
    public string MemberGender { get; set; } = string.Empty; // male, female, other
    public string? MemberId { get; set; }
    public string? SubscriberId { get; set; }
    public string OldPayerId { get; set; } = string.Empty;
    public string NewPayerId { get; set; } = string.Empty;
    public string? CoverageId { get; set; }
}

/// <summary>
/// Result of $member-match operation.
/// </summary>
public class MemberMatchResponse
{
    public bool Matched { get; set; }
    public string? MatchedMemberId { get; set; }
    public string? MatchedPatientId { get; set; }
    public string? UniquePatientIdentifier { get; set; }
    public string MatchConfidence { get; set; } = "none"; // none, possible, probable, certain
    public string? OldPayerCoverageId { get; set; }
    public string? NewPayerCoverageId { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Payer-to-Payer data exchange job.
/// Tracks the status of data transfer between payers.
/// </summary>
public class DataExchangeJob
{
    public string JobId { get; set; } = string.Empty;
    public string ConsentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string SourcePayerId { get; set; } = string.Empty;
    public string TargetPayerId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued"; // queued, in-progress, completed, failed, cancelled
    public List<string> RequestedCategories { get; set; } = new();
    public List<ExchangedResource> ExchangedResources { get; set; } = new();
    public int TotalResourcesFound { get; set; }
    public int TotalResourcesTransferred { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ProvenanceRecord> ProvenanceRecords { get; set; } = new();
}

/// <summary>
/// Resource that was exchanged between payers.
/// </summary>
public class ExchangedResource
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public string ProvenanceId { get; set; } = string.Empty;
}

/// <summary>
/// Provenance record tracking the origin and transfer of data.
/// Maps to FHIR Provenance resource.
/// </summary>
public class ProvenanceRecord
{
    public string ProvenanceId { get; set; } = string.Empty;
    public string TargetResourceType { get; set; } = string.Empty;
    public string TargetResourceId { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty; // Payer that provided the data
    public string Activity { get; set; } = string.Empty; // transmit, receive, transform
    public DateTime Recorded { get; set; } = DateTime.UtcNow;
    public string SourcePayerId { get; set; } = string.Empty;
    public string? Signature { get; set; }
}

/// <summary>
/// Request to initiate a payer-to-payer data exchange.
/// </summary>
public class ExchangeRequest
{
    public string ConsentId { get; set; } = string.Empty;
    public List<string>? DataCategories { get; set; } // Optional override; defaults to consent categories
}

/// <summary>
/// Known payer info.
/// </summary>
public class PayerInfo
{
    public string PayerId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string FhirEndpoint { get; set; } = string.Empty;
}

/// <summary>
/// Patient data summary held by this payer (simulated).
/// </summary>
public class PatientDataSummary
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public Dictionary<string, int> ResourceCounts { get; set; } = new();
    public DateTime? CoverageStart { get; set; }
    public DateTime? CoverageEnd { get; set; }
    public string PayerId { get; set; } = string.Empty;
}
