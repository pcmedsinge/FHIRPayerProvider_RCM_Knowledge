using PriorAuthAPI.Models;

namespace PriorAuthAPI.Services;

/// <summary>
/// Prior Authorization Submission (PAS) service.
/// Accepts PA requests, applies decision logic, manages status tracking.
/// Maps to Da Vinci PAS ($submit operation on Claim resource).
/// </summary>
public interface IPriorAuthService
{
    PriorAuthResponse SubmitRequest(PriorAuthRequest request);
    PriorAuthResponse? GetStatus(string authorizationId);
    List<PriorAuthResponse> GetRequestsByPatient(string patientId);
    List<PriorAuthResponse> GetAllRequests();
    PriorAuthResponse? CancelRequest(string authorizationId);
    PriorAuthResponse? UpdateStatus(string authorizationId, string newStatus, string? notes = null);
}

public class PriorAuthService : IPriorAuthService
{
    private readonly List<PriorAuthResponse> _decisions = new();
    private readonly Dictionary<string, PriorAuthRequest> _requests = new();
    private readonly ILogger<PriorAuthService> _logger;

    public PriorAuthService(ILogger<PriorAuthService> logger)
    {
        _logger = logger;
    }

    public PriorAuthResponse SubmitRequest(PriorAuthRequest request)
    {
        var authId = $"PA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        _requests[authId] = request;

        // Apply decision rules
        var decision = EvaluateRequest(request, authId);
        _decisions.Add(decision);

        _logger.LogInformation("PA Request {AuthId} submitted for {ServiceCode}: {Status}",
            authId, request.ServiceCode, decision.Status);

        return decision;
    }

    public PriorAuthResponse? GetStatus(string authorizationId) =>
        _decisions.FirstOrDefault(d => d.AuthorizationId.Equals(authorizationId, StringComparison.OrdinalIgnoreCase));

    public List<PriorAuthResponse> GetRequestsByPatient(string patientId)
    {
        var patientAuthIds = _requests
            .Where(r => r.Value.PatientId == patientId)
            .Select(r => r.Key)
            .ToHashSet();
        return _decisions.Where(d => patientAuthIds.Contains(d.AuthorizationId)).ToList();
    }

    public List<PriorAuthResponse> GetAllRequests() => _decisions.ToList();

    public PriorAuthResponse? CancelRequest(string authorizationId)
    {
        var decision = GetStatus(authorizationId);
        if (decision == null) return null;
        if (decision.Status is "approved" or "denied")
            return null; // Can't cancel final decisions

        decision.Status = "cancelled";
        decision.StatusHistory.Add(new PriorAuthStatusEntry
        {
            Status = "cancelled",
            Timestamp = DateTime.UtcNow,
            Note = "Cancelled by provider"
        });
        return decision;
    }

    public PriorAuthResponse? UpdateStatus(string authorizationId, string newStatus, string? notes = null)
    {
        var decision = GetStatus(authorizationId);
        if (decision == null) return null;

        decision.Status = newStatus;
        decision.StatusHistory.Add(new PriorAuthStatusEntry
        {
            Status = newStatus,
            Timestamp = DateTime.UtcNow,
            Note = notes ?? $"Status updated to {newStatus}"
        });
        if (newStatus == "approved")
        {
            decision.DecisionDate = DateTime.UtcNow;
            decision.ExpirationDate = DateTime.UtcNow.AddDays(90);
        }
        return decision;
    }

    // ─── Decision Engine ─────────────────────────────────────────

    private PriorAuthResponse EvaluateRequest(PriorAuthRequest request, string authId)
    {
        var response = new PriorAuthResponse
        {
            AuthorizationId = authId,
            RequestId = request.RequestId,
            StatusHistory = new()
            {
                new() { Status = "submitted", Timestamp = DateTime.UtcNow, Note = "Request received" }
            }
        };

        // Decision logic based on service type and supporting documentation
        var hasDocumentation = request.SupportingDocuments.Count > 0 || !string.IsNullOrEmpty(request.QuestionnaireResponseId);

        switch (request.ServiceCode)
        {
            // ── Auto-Approve Scenarios ───────────────────────────
            case "71046": // Chest X-Ray — no PA needed
                response.Status = "approved";
                response.ReviewOutcome = "complete";
                response.ApprovedServiceCode = request.ServiceCode;
                response.ReviewNotes = "Auto-approved. No prior authorization required for chest X-ray.";
                response.ExpirationDate = DateTime.UtcNow.AddDays(90);
                response.StatusHistory.Add(new() { Status = "approved", Note = "Auto-approved" });
                break;

            case "43239": // Upper GI Endoscopy — docs only, no PA
                response.Status = "approved";
                response.ReviewOutcome = "complete";
                response.ApprovedServiceCode = request.ServiceCode;
                response.ReviewNotes = "Approved with documentation on file.";
                response.ExpirationDate = DateTime.UtcNow.AddDays(60);
                response.StatusHistory.Add(new() { Status = "approved", Note = "Approved with documentation" });
                break;

            // ── Conditional Approval (based on documentation) ────
            case "70553": // MRI Brain
            case "74177": // CT Abdomen
            case "72148": // MRI Lumbar
                if (hasDocumentation)
                {
                    response.Status = "approved";
                    response.ReviewOutcome = "complete";
                    response.ApprovedServiceCode = request.ServiceCode;
                    response.ReviewNotes = "Approved based on clinical documentation provided.";
                    response.ExpirationDate = DateTime.UtcNow.AddDays(60);
                    response.StatusHistory.Add(new() { Status = "approved", Note = "Documentation sufficient" });
                }
                else
                {
                    response.Status = "pended-for-review";
                    response.ReviewOutcome = "queued";
                    response.ReviewNotes = "Additional documentation required. Please submit clinical notes, previous imaging results, and exam findings via DTR questionnaire.";
                    response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Awaiting documentation" });
                }
                break;

            // ── Surgical — Requires Full Review ──────────────────
            case "27447": // Total Knee Replacement
            case "29881": // Knee Arthroscopy
                if (hasDocumentation && request.Urgency == "urgent")
                {
                    response.Status = "approved";
                    response.ReviewOutcome = "complete";
                    response.ApprovedServiceCode = request.ServiceCode;
                    response.ReviewNotes = "Urgent approval granted with documentation on file.";
                    response.ExpirationDate = DateTime.UtcNow.AddDays(30);
                    response.StatusHistory.Add(new() { Status = "approved", Note = "Urgent approval" });
                }
                else if (hasDocumentation)
                {
                    response.Status = "pended-for-review";
                    response.ReviewOutcome = "queued";
                    response.ReviewNotes = "Under medical director review. Expected completion: 5-7 business days.";
                    response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Sent to medical director review" });
                }
                else
                {
                    response.Status = "pended-for-review";
                    response.ReviewOutcome = "queued";
                    response.ReviewNotes = "Documentation required: X-ray results, conservative treatment history, PT records, BMI. Please complete DTR questionnaire.";
                    response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Missing required documentation" });
                }
                break;

            // ── Specialty Medications ─────────────────────────────
            case "1991302": // Ozempic
                if (hasDocumentation)
                {
                    response.Status = "pended-for-review";
                    response.ReviewOutcome = "queued";
                    response.ReviewNotes = "Under pharmacy review. Step therapy verification in progress.";
                    response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Pharmacy review" });
                }
                else
                {
                    response.Status = "pended-for-review";
                    response.ReviewOutcome = "queued";
                    response.ReviewNotes = "Documentation required: HbA1c results, Metformin trial history, BMI. Step therapy must be verified.";
                    response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Missing step therapy documentation" });
                }
                break;

            case "1657981": // Keytruda
            case "1876366": // Dupixent
                response.Status = "pended-for-review";
                response.ReviewOutcome = "queued";
                response.ReviewNotes = "Specialty medication requires medical oncology/specialty review. Expected: 3-5 business days.";
                response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Specialty review required" });
                break;

            // ── DME ──────────────────────────────────────────────
            case "E0601": // CPAP
                if (hasDocumentation)
                {
                    response.Status = "approved";
                    response.ReviewOutcome = "complete";
                    response.ApprovedServiceCode = request.ServiceCode;
                    response.ApprovedQuantity = 1;
                    response.ReviewNotes = "Approved. 90-day compliance review required.";
                    response.ExpirationDate = DateTime.UtcNow.AddDays(90);
                    response.StatusHistory.Add(new() { Status = "approved", Note = "Approved with compliance requirement" });
                }
                else
                {
                    response.Status = "pended-for-review";
                    response.ReviewOutcome = "queued";
                    response.ReviewNotes = "Sleep study results and face-to-face evaluation required.";
                    response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Awaiting sleep study" });
                }
                break;

            // ── Not Covered ──────────────────────────────────────
            case "15780": // Cosmetic dermabrasion
                response.Status = "denied";
                response.ReviewOutcome = "complete";
                response.DenialReason = "Service is excluded from coverage. Cosmetic procedures are not covered under the member's benefit plan.";
                response.ReviewNotes = "Automatic denial — cosmetic exclusion.";
                response.StatusHistory.Add(new() { Status = "denied", Note = "Cosmetic exclusion" });
                break;

            // ── Default ──────────────────────────────────────────
            default:
                if (hasDocumentation)
                {
                    response.Status = "approved";
                    response.ReviewOutcome = "complete";
                    response.ApprovedServiceCode = request.ServiceCode;
                    response.ReviewNotes = "Approved based on standard coverage criteria.";
                    response.ExpirationDate = DateTime.UtcNow.AddDays(60);
                    response.StatusHistory.Add(new() { Status = "approved", Note = "Standard approval" });
                }
                else
                {
                    response.Status = "pended-for-review";
                    response.ReviewOutcome = "queued";
                    response.ReviewNotes = "Under review. Standard processing time: 3-5 business days.";
                    response.StatusHistory.Add(new() { Status = "pended-for-review", Note = "Standard review queue" });
                }
                break;
        }

        response.DecisionDate = DateTime.UtcNow;
        return response;
    }
}
