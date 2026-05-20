using PDexAPI.Models;

namespace PDexAPI.Services;

/// <summary>
/// $member-match service. Identifies a patient across payer systems.
/// Simulates the Da Vinci HRex $member-match operation.
/// </summary>
public interface IMemberMatchService
{
    MemberMatchResponse MatchMember(MemberMatchRequest request);
    List<PatientDataSummary> GetKnownMembers();
    PatientDataSummary? GetMemberSummary(string patientId);
}

public class MemberMatchService : IMemberMatchService
{
    private readonly List<PatientDataSummary> _knownMembers;
    private readonly ILogger<MemberMatchService> _logger;

    public MemberMatchService(ILogger<MemberMatchService> logger)
    {
        _logger = logger;

        // Simulated member registry (maps to patients in HAPI FHIR server)
        _knownMembers = new List<PatientDataSummary>
        {
            new()
            {
                PatientId = "51707",
                PatientName = "Ramon Schulist",
                PayerId = "PAYER-ALPHA",
                CoverageStart = new DateTime(2020, 1, 1),
                CoverageEnd = new DateTime(2025, 12, 31),
                ResourceCounts = new Dictionary<string, int>
                {
                    ["ExplanationOfBenefit"] = 85,
                    ["Encounter"] = 42,
                    ["MedicationRequest"] = 15,
                    ["Condition"] = 8,
                    ["Procedure"] = 12,
                    ["Observation"] = 65,
                    ["AllergyIntolerance"] = 3,
                    ["DiagnosticReport"] = 20
                }
            },
            new()
            {
                PatientId = "52458",
                PatientName = "Rasheeda Heaney",
                PayerId = "PAYER-ALPHA",
                CoverageStart = new DateTime(2019, 6, 1),
                CoverageEnd = new DateTime(2024, 5, 31),
                ResourceCounts = new Dictionary<string, int>
                {
                    ["ExplanationOfBenefit"] = 120,
                    ["Encounter"] = 55,
                    ["MedicationRequest"] = 22,
                    ["Condition"] = 12,
                    ["Procedure"] = 8,
                    ["Observation"] = 90,
                    ["AllergyIntolerance"] = 1,
                    ["DiagnosticReport"] = 15
                }
            },
            new()
            {
                PatientId = "65520",
                PatientName = "Karena O'Keefe",
                PayerId = "PAYER-ALPHA",
                CoverageStart = new DateTime(2021, 3, 15),
                CoverageEnd = new DateTime(2026, 3, 14),
                ResourceCounts = new Dictionary<string, int>
                {
                    ["ExplanationOfBenefit"] = 45,
                    ["Encounter"] = 28,
                    ["MedicationRequest"] = 10,
                    ["Condition"] = 5,
                    ["Procedure"] = 6,
                    ["Observation"] = 40,
                    ["AllergyIntolerance"] = 2,
                    ["DiagnosticReport"] = 12
                }
            },
            new()
            {
                PatientId = "55001",
                PatientName = "Margaret Johnson",
                PayerId = "PAYER-BETA",
                CoverageStart = new DateTime(2018, 1, 1),
                CoverageEnd = new DateTime(2023, 12, 31),
                ResourceCounts = new Dictionary<string, int>
                {
                    ["ExplanationOfBenefit"] = 200,
                    ["Encounter"] = 95,
                    ["MedicationRequest"] = 35,
                    ["Condition"] = 18,
                    ["Procedure"] = 22,
                    ["Observation"] = 150,
                    ["AllergyIntolerance"] = 4,
                    ["DiagnosticReport"] = 30
                }
            },
            new()
            {
                PatientId = "55002",
                PatientName = "Robert Williams",
                PayerId = "PAYER-GAMMA",
                CoverageStart = new DateTime(2022, 7, 1),
                CoverageEnd = new DateTime(2025, 6, 30),
                ResourceCounts = new Dictionary<string, int>
                {
                    ["ExplanationOfBenefit"] = 30,
                    ["Encounter"] = 15,
                    ["MedicationRequest"] = 8,
                    ["Condition"] = 4,
                    ["Procedure"] = 3,
                    ["Observation"] = 25,
                    ["AllergyIntolerance"] = 0,
                    ["DiagnosticReport"] = 5
                }
            }
        };
    }

    public MemberMatchResponse MatchMember(MemberMatchRequest request)
    {
        _logger.LogInformation(
            "Member match request: {First} {Last}, DOB: {DOB}, from {OldPayer} to {NewPayer}",
            request.MemberFirstName, request.MemberLastName, request.MemberDateOfBirth,
            request.OldPayerId, request.NewPayerId);

        // Strategy 1: Direct ID match
        if (!string.IsNullOrEmpty(request.MemberId))
        {
            var directMatch = _knownMembers.FirstOrDefault(m => m.PatientId == request.MemberId);
            if (directMatch != null)
            {
                return new MemberMatchResponse
                {
                    Matched = true,
                    MatchedMemberId = directMatch.PatientId,
                    MatchedPatientId = directMatch.PatientId,
                    UniquePatientIdentifier = $"UPI-{directMatch.PatientId}-{directMatch.PayerId}",
                    MatchConfidence = "certain",
                    OldPayerCoverageId = $"COV-{request.OldPayerId}-{directMatch.PatientId}",
                    NewPayerCoverageId = $"COV-{request.NewPayerId}-{directMatch.PatientId}",
                    Message = "Exact member ID match found"
                };
            }
        }

        // Strategy 2: Name + DOB match
        var nameMatches = _knownMembers.Where(m =>
            m.PatientName.Contains(request.MemberLastName, StringComparison.OrdinalIgnoreCase) &&
            m.PatientName.Contains(request.MemberFirstName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (nameMatches.Count == 1)
        {
            var match = nameMatches[0];
            return new MemberMatchResponse
            {
                Matched = true,
                MatchedMemberId = match.PatientId,
                MatchedPatientId = match.PatientId,
                UniquePatientIdentifier = $"UPI-{match.PatientId}-{match.PayerId}",
                MatchConfidence = "probable",
                OldPayerCoverageId = $"COV-{request.OldPayerId}-{match.PatientId}",
                NewPayerCoverageId = $"COV-{request.NewPayerId}-{match.PatientId}",
                Message = "Matched by name. DOB verification recommended."
            };
        }
        else if (nameMatches.Count > 1)
        {
            return new MemberMatchResponse
            {
                Matched = false,
                MatchConfidence = "possible",
                Message = $"Multiple potential matches found ({nameMatches.Count}). Additional demographics needed."
            };
        }

        // Strategy 3: Last name partial match
        var partialMatches = _knownMembers.Where(m =>
            m.PatientName.Contains(request.MemberLastName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (partialMatches.Count > 0)
        {
            return new MemberMatchResponse
            {
                Matched = false,
                MatchConfidence = "possible",
                Message = $"Partial name match found ({partialMatches.Count} candidates). Full demographics required."
            };
        }

        // No match
        return new MemberMatchResponse
        {
            Matched = false,
            MatchConfidence = "none",
            Message = "No matching member found in the system."
        };
    }

    public List<PatientDataSummary> GetKnownMembers() => _knownMembers.ToList();

    public PatientDataSummary? GetMemberSummary(string patientId) =>
        _knownMembers.FirstOrDefault(m => m.PatientId == patientId);
}
