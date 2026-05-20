namespace BulkDataAPI.Models;

/// <summary>
/// Bulk export job — tracks async $export operation.
/// Maps to FHIR Bulk Data Access ($export).
/// </summary>
public class ExportJob
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued"; // queued, in-progress, completed, failed, cancelled
    public string ExportType { get; set; } = "system"; // system, patient, group
    public string? GroupId { get; set; }
    public List<string> ResourceTypes { get; set; } = new();
    public string? Since { get; set; } // _since parameter (RFC 3339 date)
    public string? TypeFilter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ExportOutput> Output { get; set; } = new();
    public List<ExportError> Errors { get; set; } = new();
    public ExportSummary? Summary { get; set; }
    public string? TransactionTime { get; set; } // Server time at export start
}

/// <summary>
/// Output file from bulk export.
/// </summary>
public class ExportOutput
{
    public string Type { get; set; } = string.Empty; // Resource type
    public string Url { get; set; } = string.Empty; // Download URL
    public int Count { get; set; } // Number of resources
    public long SizeBytes { get; set; }
}

/// <summary>
/// Error from bulk export processing.
/// </summary>
public class ExportError
{
    public string Type { get; set; } = "OperationOutcome";
    public string Url { get; set; } = string.Empty;
    public string? Message { get; set; }
}

/// <summary>
/// Summary statistics for a completed export.
/// </summary>
public class ExportSummary
{
    public int TotalResources { get; set; }
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public Dictionary<string, int> ResourceTypeCounts { get; set; } = new();
    public double DurationSeconds { get; set; }
}

/// <summary>
/// Request to initiate a bulk export.
/// Maps to GET/POST $export with parameters.
/// </summary>
public class ExportRequest
{
    public string ExportType { get; set; } = "system"; // system, patient, group
    public string? GroupId { get; set; }
    public List<string>? ResourceTypes { get; set; } // _type parameter
    public string? Since { get; set; } // _since parameter
    public string? TypeFilter { get; set; } // _typeFilter
    public string OutputFormat { get; set; } = "application/fhir+ndjson";
}

/// <summary>
/// NDJSON line for export output.
/// </summary>
public class NdjsonResource
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string JsonContent { get; set; } = string.Empty;
}

/// <summary>
/// Analytics summary across exported data.
/// </summary>
public class DataAnalytics
{
    public int TotalPatients { get; set; }
    public int TotalClaims { get; set; }
    public int TotalEncounters { get; set; }
    public int TotalMedications { get; set; }
    public int TotalConditions { get; set; }
    public int TotalProcedures { get; set; }
    public Dictionary<string, int> PatientsByGender { get; set; } = new();
    public Dictionary<string, int> EncountersByClass { get; set; } = new();
    public Dictionary<string, int> TopConditions { get; set; } = new();
    public Dictionary<string, int> TopMedications { get; set; } = new();
    public Dictionary<string, int> ClaimsByType { get; set; } = new();
    public decimal TotalClaimAmount { get; set; }
    public decimal AverageClaimAmount { get; set; }
}

/// <summary>
/// Patient group for group-level export.
/// </summary>
public class PatientGroup
{
    public string GroupId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> MemberPatientIds { get; set; } = new();
    public string Type { get; set; } = "person"; // person, practitioner, device
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
