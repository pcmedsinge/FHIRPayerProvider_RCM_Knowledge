using PDexAPI.Models;

namespace PDexAPI.Services;

/// <summary>
/// Consent management service for payer-to-payer data exchange.
/// Manages patient consent lifecycle: create, activate, revoke.
/// </summary>
public interface IConsentService
{
    DataExchangeConsent CreateConsent(ConsentRequest request);
    DataExchangeConsent? GetConsent(string consentId);
    DataExchangeConsent? ActivateConsent(string consentId);
    DataExchangeConsent? RevokeConsent(string consentId);
    List<DataExchangeConsent> GetConsentsByPatient(string patientId);
    List<DataExchangeConsent> GetAllConsents();
    bool HasActiveConsent(string patientId, string sourcePayerId, string targetPayerId);
}

public class ConsentService : IConsentService
{
    private readonly List<DataExchangeConsent> _consents = new();
    private readonly IConfiguration _config;
    private readonly ILogger<ConsentService> _logger;

    public ConsentService(IConfiguration config, ILogger<ConsentService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public DataExchangeConsent CreateConsent(ConsentRequest request)
    {
        var knownPayers = _config.GetSection("Payers:KnownPayers").Get<List<PayerInfo>>() ?? new();
        var sourcePayer = knownPayers.FirstOrDefault(p => p.PayerId == request.SourcePayerId);
        var targetPayer = knownPayers.FirstOrDefault(p => p.PayerId == request.TargetPayerId);

        var consent = new DataExchangeConsent
        {
            ConsentId = $"CONSENT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            PatientId = request.PatientId,
            PatientName = request.PatientName,
            SourcePayerId = request.SourcePayerId,
            SourcePayerName = sourcePayer?.PayerName ?? request.SourcePayerId,
            TargetPayerId = request.TargetPayerId,
            TargetPayerName = targetPayer?.PayerName ?? request.TargetPayerId,
            Status = "draft",
            DataCategories = request.DataCategories.Count > 0
                ? request.DataCategories
                : new List<string> { "claims", "encounters", "medications", "conditions", "allergies", "procedures", "observations" },
            ExpirationDate = DateTime.UtcNow.AddDays(request.ExpirationDays)
        };

        _consents.Add(consent);
        _logger.LogInformation("Consent {ConsentId} created for patient {PatientId}", consent.ConsentId, consent.PatientId);
        return consent;
    }

    public DataExchangeConsent? GetConsent(string consentId) =>
        _consents.FirstOrDefault(c => c.ConsentId == consentId);

    public DataExchangeConsent? ActivateConsent(string consentId)
    {
        var consent = _consents.FirstOrDefault(c => c.ConsentId == consentId);
        if (consent == null || consent.Status != "draft") return null;

        consent.Status = "active";
        consent.ConsentDate = DateTime.UtcNow;
        _logger.LogInformation("Consent {ConsentId} activated", consentId);
        return consent;
    }

    public DataExchangeConsent? RevokeConsent(string consentId)
    {
        var consent = _consents.FirstOrDefault(c => c.ConsentId == consentId);
        if (consent == null || consent.Status == "revoked") return null;

        consent.Status = "revoked";
        consent.RevokedAt = DateTime.UtcNow;
        _logger.LogInformation("Consent {ConsentId} revoked", consentId);
        return consent;
    }

    public List<DataExchangeConsent> GetConsentsByPatient(string patientId) =>
        _consents.Where(c => c.PatientId == patientId).ToList();

    public List<DataExchangeConsent> GetAllConsents() => _consents.ToList();

    public bool HasActiveConsent(string patientId, string sourcePayerId, string targetPayerId) =>
        _consents.Any(c =>
            c.PatientId == patientId &&
            c.SourcePayerId == sourcePayerId &&
            c.TargetPayerId == targetPayerId &&
            c.Status == "active" &&
            (c.ExpirationDate == null || c.ExpirationDate > DateTime.UtcNow));
}
