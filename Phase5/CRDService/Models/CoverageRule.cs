namespace CRDService.Models;

/// <summary>
/// Coverage rules used by the CRD engine to determine requirements.
/// </summary>
public class CoverageRule
{
    public string RuleId { get; set; } = "";
    public string ServiceType { get; set; } = ""; // procedure, medication, imaging, dme
    public string Code { get; set; } = "";
    public string CodeSystem { get; set; } = "";
    public string Description { get; set; } = "";
    public bool RequiresPriorAuth { get; set; }
    public bool RequiresDocumentation { get; set; }
    public string? DocumentationUrl { get; set; }
    public List<string> RequiredDocuments { get; set; } = new();
    public string CoverageStatus { get; set; } = "covered"; // covered, not-covered, conditional
    public string? AlternativeSuggestion { get; set; }
    public string? ConditionNote { get; set; }
}
