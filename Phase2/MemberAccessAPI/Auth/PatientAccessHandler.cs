using System.Security.Claims;

namespace MemberAccessAPI.Auth;

public static class PatientAccessHandler
{
    /// <summary>
    /// Extracts patient_id from JWT claims
    /// </summary>
    public static string? GetPatientId(ClaimsPrincipal user)
    {
        return user.FindFirst("patient_id")?.Value;
    }

    /// <summary>
    /// Checks if the authenticated member is allowed to access this patient's data
    /// </summary>
    public static bool CanAccessPatient(ClaimsPrincipal user, string requestedPatientId)
    {
        var tokenPatientId = GetPatientId(user);
        return tokenPatientId != null && tokenPatientId == requestedPatientId;
    }
    public static bool HasScope(ClaimsPrincipal user, string resourceType)
    {
        var scopeClaim = user.FindFirst("scope")?.Value ?? "";
        var requiredScope = $"patient/{resourceType}.read";
        return scopeClaim.Contains(requiredScope);
    }
}