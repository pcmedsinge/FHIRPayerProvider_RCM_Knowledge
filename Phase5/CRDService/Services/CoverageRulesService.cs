using CRDService.Models;

namespace CRDService.Services;

/// <summary>
/// In-memory coverage rules engine simulating a payer's CRD decision logic.
/// In production, this would connect to the payer's medical policy database.
/// </summary>
public interface ICoverageRulesService
{
    CoverageRule? FindRule(string code, string? codeSystem = null);
    List<CoverageRule> GetAllRules();
    List<CoverageRule> GetRulesByType(string serviceType);
    CdsHookResponse EvaluateOrderSelect(CdsHookRequest request);
    CdsHookResponse EvaluateOrderSign(CdsHookRequest request);
    CdsHookResponse EvaluateAppointmentBook(CdsHookRequest request);
}

public class CoverageRulesService : ICoverageRulesService
{
    private readonly List<CoverageRule> _rules;
    private readonly ILogger<CoverageRulesService> _logger;

    public CoverageRulesService(ILogger<CoverageRulesService> logger)
    {
        _logger = logger;
        _rules = InitializeRules();
    }

    public CoverageRule? FindRule(string code, string? codeSystem = null) =>
        _rules.FirstOrDefault(r =>
            r.Code.Equals(code, StringComparison.OrdinalIgnoreCase) &&
            (codeSystem == null || r.CodeSystem.Equals(codeSystem, StringComparison.OrdinalIgnoreCase)));

    public List<CoverageRule> GetAllRules() => _rules;

    public List<CoverageRule> GetRulesByType(string serviceType) =>
        _rules.Where(r => r.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase)).ToList();

    // ─── Hook Evaluators ─────────────────────────────────────────

    public CdsHookResponse EvaluateOrderSelect(CdsHookRequest request)
    {
        var response = new CdsHookResponse();
        _logger.LogInformation("Evaluating order-select hook for patient {PatientId}", request.Context?.PatientId);

        // Check selections for coverage requirements
        if (request.Context?.Selections != null)
        {
            foreach (var selection in request.Context.Selections)
            {
                if (selection.Code != null)
                {
                    var rule = FindRule(selection.Code);
                    if (rule != null)
                    {
                        response.Cards.AddRange(BuildCardsFromRule(rule, selection.Display ?? selection.Code));
                    }
                }
            }
        }

        // Check draftOrders if present
        response.Cards.AddRange(EvaluateDraftOrders(request));

        if (response.Cards.Count == 0)
        {
            response.Cards.Add(new CdsCard
            {
                Summary = "No coverage requirements identified",
                Detail = "The selected service does not require prior authorization based on current rules.",
                Indicator = "info",
                Source = new CdsSource { Label = "Payer CRD Service", Url = "https://example-payer.com/crd" }
            });
        }

        return response;
    }

    public CdsHookResponse EvaluateOrderSign(CdsHookRequest request)
    {
        var response = new CdsHookResponse();
        _logger.LogInformation("Evaluating order-sign hook for patient {PatientId}", request.Context?.PatientId);

        // Evaluate explicit selections when provided
        if (request.Context?.Selections != null)
        {
            foreach (var selection in request.Context.Selections)
            {
                if (selection.Code != null)
                {
                    var rule = FindRule(selection.Code);
                    if (rule != null)
                    {
                        response.Cards.AddRange(BuildCardsFromRule(rule, selection.Display ?? selection.Code));
                    }
                }
            }
        }

        // Evaluate all draft orders being signed
        response.Cards.AddRange(EvaluateDraftOrders(request));

        // Add signing-specific guidance
        if (response.Cards.Any(c => c.Indicator == "warning" || c.Indicator == "critical"))
        {
            response.Cards.Add(new CdsCard
            {
                Summary = "⚠️ Review Required Before Signing",
                Detail = "One or more orders have coverage requirements that should be addressed before final signature.",
                Indicator = "warning",
                Source = new CdsSource { Label = "Payer CRD Service" }
            });
        }

        return response;
    }

    public CdsHookResponse EvaluateAppointmentBook(CdsHookRequest request)
    {
        var response = new CdsHookResponse();
        _logger.LogInformation("Evaluating appointment-book hook for patient {PatientId}", request.Context?.PatientId);

        // For appointment-book, provide general guidance
        response.Cards.Add(new CdsCard
        {
            Summary = "Network Status Check",
            Detail = "Please verify that the provider/facility is in-network for the member's plan before scheduling.",
            Indicator = "info",
            Source = new CdsSource { Label = "Payer CRD Service" },
            Links = new List<CdsLink>
            {
                new() { Label = "Provider Directory", Url = "http://localhost:5230/swagger", Type = "absolute" }
            }
        });

        return response;
    }

    // ─── Card Builders ───────────────────────────────────────────  

    private List<CdsCard> BuildCardsFromRule(CoverageRule rule, string serviceName)
    {
        var cards = new List<CdsCard>();

        // Prior Auth card
        if (rule.RequiresPriorAuth)
        {
            cards.Add(new CdsCard
            {
                Summary = $"Prior Authorization Required: {serviceName}",
                Detail = $"{rule.Description}\n\n**Required Documents**: {string.Join(", ", rule.RequiredDocuments)}\n\n{rule.ConditionNote ?? ""}",
                Indicator = "warning",
                Source = new CdsSource
                {
                    Label = "Payer CRD Service",
                    Url = "https://example-payer.com/crd"
                },
                Links = new List<CdsLink>
                {
                    new() { Label = "Start Prior Auth (PAS)", Url = "http://localhost:5260/swagger", Type = "absolute" },
                    new() { Label = "Documentation Requirements", Url = rule.DocumentationUrl ?? "https://example-payer.com/docs", Type = "absolute" }
                },
                Suggestions = new List<CdsSuggestion>
                {
                    new()
                    {
                        Label = "Launch DTR to collect documentation",
                        IsRecommended = true,
                        Actions = new List<CdsSuggestionAction>
                        {
                            new() { Type = "create", Description = "Launch Documentation Templates & Rules app" }
                        }
                    }
                }
            });
        }

        // Documentation card
        if (rule.RequiresDocumentation && !rule.RequiresPriorAuth)
        {
            cards.Add(new CdsCard
            {
                Summary = $"Documentation Required: {serviceName}",
                Detail = $"Please provide: {string.Join(", ", rule.RequiredDocuments)}",
                Indicator = "info",
                Source = new CdsSource { Label = "Payer CRD Service" }
            });
        }

        // Alternative suggestion card
        if (!string.IsNullOrEmpty(rule.AlternativeSuggestion))
        {
            cards.Add(new CdsCard
            {
                Summary = $"Alternative Available: {serviceName}",
                Detail = rule.AlternativeSuggestion,
                Indicator = "info",
                Source = new CdsSource { Label = "Payer CRD Service" },
                Suggestions = new List<CdsSuggestion>
                {
                    new() { Label = "Consider alternative", IsRecommended = false }
                }
            });
        }

        // Not covered card
        if (rule.CoverageStatus == "not-covered")
        {
            cards.Add(new CdsCard
            {
                Summary = $"⛔ Not Covered: {serviceName}",
                Detail = $"This service is not covered under the member's current plan. {rule.ConditionNote ?? "Contact the plan for details."}",
                Indicator = "critical",
                Source = new CdsSource { Label = "Payer CRD Service" }
            });
        }

        return cards;
    }

    private List<CdsCard> EvaluateDraftOrders(CdsHookRequest request)
    {
        var cards = new List<CdsCard>();

        // Extract codes from draft orders (simplified — in reality would parse FHIR Bundle)
        if (request.Context?.DraftOrders is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                var text = jsonElement.GetRawText();
                // Look for CPT/HCPCS codes in the draft orders
                foreach (var rule in _rules)
                {
                    if (text.Contains(rule.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        cards.AddRange(BuildCardsFromRule(rule, rule.Description));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error parsing draft orders: {Error}", ex.Message);
            }
        }

        return cards;
    }

    // ─── Rules Data ──────────────────────────────────────────────

    private List<CoverageRule> InitializeRules() => new()
    {
        // ── Imaging ──────────────────────────────────────────────
        new()
        {
            RuleId = "CRD-001", ServiceType = "imaging", Code = "70553", CodeSystem = "CPT",
            Description = "MRI Brain with and without contrast",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            DocumentationUrl = "https://example-payer.com/docs/mri-brain",
            RequiredDocuments = new() { "Clinical indication", "Previous imaging results", "Neurological exam findings" },
            CoverageStatus = "conditional",
            ConditionNote = "Prior auth required. Must demonstrate medical necessity with failed conservative treatment."
        },
        new()
        {
            RuleId = "CRD-002", ServiceType = "imaging", Code = "74177", CodeSystem = "CPT",
            Description = "CT Abdomen/Pelvis with contrast",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "Clinical indication", "Lab results" },
            CoverageStatus = "conditional",
            ConditionNote = "Prior auth required for non-emergency use.",
            AlternativeSuggestion = "Consider ultrasound (76700) as first-line imaging — covered without prior auth."
        },
        new()
        {
            RuleId = "CRD-003", ServiceType = "imaging", Code = "71046", CodeSystem = "CPT",
            Description = "Chest X-Ray 2 views",
            RequiresPriorAuth = false, RequiresDocumentation = false,
            CoverageStatus = "covered"
        },
        new()
        {
            RuleId = "CRD-004", ServiceType = "imaging", Code = "72148", CodeSystem = "CPT",
            Description = "MRI Lumbar Spine without contrast",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "Duration of symptoms (>6 weeks)", "Conservative treatment history", "Neurological exam" },
            CoverageStatus = "conditional",
            ConditionNote = "Requires 6+ weeks of conservative treatment before approval."
        },

        // ── Procedures ───────────────────────────────────────────
        new()
        {
            RuleId = "CRD-005", ServiceType = "procedure", Code = "27447", CodeSystem = "CPT",
            Description = "Total Knee Replacement",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "X-ray evidence of degenerative changes", "6 months conservative treatment", "BMI documentation", "Physical therapy records" },
            CoverageStatus = "conditional",
            ConditionNote = "Requires documented failure of 6 months conservative treatment including physical therapy.",
            AlternativeSuggestion = "Consider knee arthroscopy (29881) or corticosteroid injection (20610) before total replacement."
        },
        new()
        {
            RuleId = "CRD-006", ServiceType = "procedure", Code = "29881", CodeSystem = "CPT",
            Description = "Knee Arthroscopy with Meniscectomy",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "MRI results", "Conservative treatment history" },
            CoverageStatus = "conditional"
        },
        new()
        {
            RuleId = "CRD-007", ServiceType = "procedure", Code = "43239", CodeSystem = "CPT",
            Description = "Upper GI Endoscopy with Biopsy",
            RequiresPriorAuth = false, RequiresDocumentation = true,
            RequiredDocuments = new() { "Clinical indication", "Symptom duration" },
            CoverageStatus = "covered"
        },

        // ── Medications (high-cost) ──────────────────────────────
        new()
        {
            RuleId = "CRD-008", ServiceType = "medication", Code = "1991302", CodeSystem = "RxNorm",
            Description = "Semaglutide (Ozempic) injection",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "HbA1c results", "Metformin trial documentation", "BMI documentation" },
            CoverageStatus = "conditional",
            ConditionNote = "Step therapy required: must have tried Metformin first.",
            AlternativeSuggestion = "Consider Metformin (generic, $10 copay) or Jardiance ($30 copay) as step therapy options."
        },
        new()
        {
            RuleId = "CRD-009", ServiceType = "medication", Code = "1657981", CodeSystem = "RxNorm",
            Description = "Pembrolizumab (Keytruda) IV infusion",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "Pathology report", "PD-L1 expression results", "Staging documentation", "Treatment plan" },
            CoverageStatus = "conditional",
            ConditionNote = "Specialty drug. Requires oncology review. Must have documented PD-L1 positive status."
        },
        new()
        {
            RuleId = "CRD-010", ServiceType = "medication", Code = "1876366", CodeSystem = "RxNorm",
            Description = "Dupilumab (Dupixent) injection",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "Diagnosis confirmation (atopic dermatitis/asthma)", "Failed topical therapy", "Severity scoring" },
            CoverageStatus = "conditional"
        },

        // ── DME (Durable Medical Equipment) ──────────────────────
        new()
        {
            RuleId = "CRD-011", ServiceType = "dme", Code = "E0601", CodeSystem = "HCPCS",
            Description = "CPAP Device",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "Sleep study results (AHI score)", "Physician order", "Compliance monitoring plan" },
            CoverageStatus = "conditional",
            ConditionNote = "AHI must be >= 5 events/hour. 90-day compliance review required."
        },
        new()
        {
            RuleId = "CRD-012", ServiceType = "dme", Code = "K0856", CodeSystem = "HCPCS",
            Description = "Power Wheelchair",
            RequiresPriorAuth = true, RequiresDocumentation = true,
            RequiredDocuments = new() { "Face-to-face exam", "Mobility assessment", "Home assessment", "Failed manual wheelchair trial" },
            CoverageStatus = "conditional",
            ConditionNote = "Requires face-to-face exam within 45 days. Must document inability to use manual wheelchair."
        },

        // ── Cosmetic / Not Covered ───────────────────────────────
        new()
        {
            RuleId = "CRD-013", ServiceType = "procedure", Code = "15780", CodeSystem = "CPT",
            Description = "Dermabrasion (cosmetic)",
            RequiresPriorAuth = false, RequiresDocumentation = false,
            CoverageStatus = "not-covered",
            ConditionNote = "Cosmetic procedures are excluded from coverage."
        },
    };
}
