using FormularyAPI.Models;

namespace FormularyAPI.Services;

/// <summary>
/// In-memory formulary service with realistic sample data.
/// In production, this would connect to the payer's formulary database.
/// </summary>
public interface IFormularyService
{
    List<FormularyPlan> GetAllPlans();
    FormularyPlan? GetPlan(string planId);
    List<FormularyDrug> SearchDrugs(string? name = null, string? tier = null, string? planId = null, string? therapeuticClass = null, bool? stepTherapy = null, bool? requiresPriorAuth = null);
    FormularyDrug? GetDrug(string drugId);
    CoverageCheckResponse CheckCoverage(CoverageCheckRequest request);
    List<FormularyDrug> GetAlternatives(string drugId);
    List<string> GetDrugTiers(string planId);
}

public class FormularyService : IFormularyService
{
    private readonly List<FormularyPlan> _plans;
    private readonly List<FormularyDrug> _drugs;

    public FormularyService()
    {
        _plans = InitializePlans();
        _drugs = InitializeDrugs();
    }

    public List<FormularyPlan> GetAllPlans() => _plans;

    public FormularyPlan? GetPlan(string planId) =>
        _plans.FirstOrDefault(p => p.PlanId.Equals(planId, StringComparison.OrdinalIgnoreCase));

    public List<FormularyDrug> SearchDrugs(string? name = null, string? tier = null, string? planId = null, string? therapeuticClass = null, bool? stepTherapy = null, bool? requiresPriorAuth = null)
    {
        var query = _drugs.AsEnumerable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(d =>
                d.DrugName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                d.GenericName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                d.BrandName.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(tier))
            query = query.Where(d => d.DrugTier.Equals(tier, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(planId))
            query = query.Where(d => d.PlanId.Equals(planId, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(therapeuticClass))
            query = query.Where(d => d.TherapeuticClass.Contains(therapeuticClass, StringComparison.OrdinalIgnoreCase));

        if (stepTherapy.HasValue)
            query = query.Where(d => d.StepTherapy == stepTherapy.Value);

        if (requiresPriorAuth.HasValue)
            query = query.Where(d => d.RequiresPriorAuth == requiresPriorAuth.Value);

        return query.ToList();
    }

    public FormularyDrug? GetDrug(string drugId) =>
        _drugs.FirstOrDefault(d => d.DrugId.Equals(drugId, StringComparison.OrdinalIgnoreCase));

    public CoverageCheckResponse CheckCoverage(CoverageCheckRequest request)
    {
        var matches = _drugs.Where(d =>
            (!string.IsNullOrEmpty(request.DrugName) && 
             (d.DrugName.Contains(request.DrugName, StringComparison.OrdinalIgnoreCase) ||
              d.GenericName.Contains(request.DrugName, StringComparison.OrdinalIgnoreCase))) ||
            (!string.IsNullOrEmpty(request.RxNormCode) && d.RxNormCode == request.RxNormCode));

        if (!string.IsNullOrEmpty(request.PlanId))
            matches = matches.Where(d => d.PlanId.Equals(request.PlanId, StringComparison.OrdinalIgnoreCase));

        var drug = matches.FirstOrDefault();

        if (drug == null)
        {
            return new CoverageCheckResponse
            {
                IsCovered = false,
                DrugName = request.DrugName,
                Message = "Drug not found on formulary. Contact your plan for coverage details."
            };
        }

        return new CoverageCheckResponse
        {
            IsCovered = true,
            DrugName = drug.DrugName,
            DrugTier = drug.DrugTier,
            EstimatedCopay = drug.Copay,
            CoinsurancePercent = drug.CoinsurancePercent,
            RequiresPriorAuth = drug.RequiresPriorAuth,
            QuantityLimit = drug.QuantityLimit,
            StepTherapy = drug.StepTherapy,
            Alternatives = drug.Alternatives,
            PlanName = drug.PlanName,
            Message = drug.RequiresPriorAuth ?
                "Prior authorization required. Contact your provider to initiate." :
                "Covered under your plan."
        };
    }

    public List<FormularyDrug> GetAlternatives(string drugId)
    {
        var drug = GetDrug(drugId);
        if (drug == null) return new List<FormularyDrug>();

        return _drugs.Where(d =>
            d.TherapeuticClass == drug.TherapeuticClass &&
            d.DrugId != drug.DrugId &&
            d.PlanId == drug.PlanId)
            .OrderBy(d => d.Copay)
            .ToList();
    }

    public List<string> GetDrugTiers(string planId)
    {
        return _drugs
            .Where(d => d.PlanId.Equals(planId, StringComparison.OrdinalIgnoreCase))
            .Select(d => d.DrugTier)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    // ─── Sample Data ─────────────────────────────────────────────

    private List<FormularyPlan> InitializePlans() => new()
    {
        new FormularyPlan
        {
            PlanId = "PLAN-PPO-2026",
            PlanName = "Blue Cross PPO Gold 2026",
            PayerName = "Blue Cross Blue Shield",
            PlanType = "PPO",
            PlanYear = 2026,
            DrugTiers = new()
            {
                new() { TierName = "generic", TierDescription = "Tier 1 - Generic Drugs", DefaultCopay = 10, DefaultCoinsurance = 0, MailOrderAvailable = true },
                new() { TierName = "preferred-brand", TierDescription = "Tier 2 - Preferred Brand", DefaultCopay = 30, DefaultCoinsurance = 0, MailOrderAvailable = true },
                new() { TierName = "non-preferred-brand", TierDescription = "Tier 3 - Non-Preferred Brand", DefaultCopay = 50, DefaultCoinsurance = 20, MailOrderAvailable = true },
                new() { TierName = "specialty", TierDescription = "Tier 4 - Specialty Drugs", DefaultCopay = 0, DefaultCoinsurance = 30, MailOrderAvailable = false }
            }
        },
        new FormularyPlan
        {
            PlanId = "PLAN-HMO-2026",
            PlanName = "Aetna HMO Silver 2026",
            PayerName = "Aetna",
            PlanType = "HMO",
            PlanYear = 2026,
            DrugTiers = new()
            {
                new() { TierName = "generic", TierDescription = "Tier 1 - Generic", DefaultCopay = 5, DefaultCoinsurance = 0, MailOrderAvailable = true },
                new() { TierName = "preferred-brand", TierDescription = "Tier 2 - Preferred", DefaultCopay = 25, DefaultCoinsurance = 0, MailOrderAvailable = true },
                new() { TierName = "non-preferred-brand", TierDescription = "Tier 3 - Non-Preferred", DefaultCopay = 45, DefaultCoinsurance = 25, MailOrderAvailable = false },
                new() { TierName = "specialty", TierDescription = "Tier 4 - Specialty", DefaultCopay = 0, DefaultCoinsurance = 35, MailOrderAvailable = false }
            }
        }
    };

    private List<FormularyDrug> InitializeDrugs() => new()
    {
        // ── Cardiovascular ──────────────────────────────────────
        new() { DrugId = "DRUG-001", DrugName = "Lisinopril 10mg", GenericName = "Lisinopril", BrandName = "Prinivil", NdcCode = "00006-0106-54", RxNormCode = "314076", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Cardiovascular - ACE Inhibitor", DosageForm = "Tablet", Alternatives = new() { "Enalapril 10mg", "Ramipril 5mg" } },
        new() { DrugId = "DRUG-002", DrugName = "Enalapril 10mg", GenericName = "Enalapril", BrandName = "Vasotec", NdcCode = "00187-0014-30", RxNormCode = "29046", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Cardiovascular - ACE Inhibitor", DosageForm = "Tablet", Alternatives = new() { "Lisinopril 10mg", "Ramipril 5mg" } },
        new() { DrugId = "DRUG-003", DrugName = "Amlodipine 5mg", GenericName = "Amlodipine", BrandName = "Norvasc", NdcCode = "00069-1520-30", RxNormCode = "329528", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Cardiovascular - Calcium Channel Blocker", DosageForm = "Tablet", Alternatives = new() { "Nifedipine 30mg ER" } },
        new() { DrugId = "DRUG-004", DrugName = "Atorvastatin 20mg", GenericName = "Atorvastatin", BrandName = "Lipitor", NdcCode = "00071-0155-23", RxNormCode = "259255", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Cardiovascular - Statin", DosageForm = "Tablet", Alternatives = new() { "Simvastatin 20mg", "Rosuvastatin 10mg" } },
        new() { DrugId = "DRUG-005", DrugName = "Crestor 10mg", GenericName = "Rosuvastatin", BrandName = "Crestor", NdcCode = "00310-0751-90", RxNormCode = "301542", DrugTier = "preferred-brand", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = true, Copay = 30, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Cardiovascular - Statin", DosageForm = "Tablet", Alternatives = new() { "Atorvastatin 20mg", "Simvastatin 20mg" } },

        // ── Diabetes ────────────────────────────────────────────
        new() { DrugId = "DRUG-006", DrugName = "Metformin 500mg", GenericName = "Metformin", BrandName = "Glucophage", NdcCode = "00087-6060-13", RxNormCode = "861004", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Diabetes - Biguanide", DosageForm = "Tablet", Alternatives = new() { "Metformin 850mg", "Metformin 1000mg" } },
        new() { DrugId = "DRUG-007", DrugName = "Jardiance 10mg", GenericName = "Empagliflozin", BrandName = "Jardiance", NdcCode = "00597-0152-30", RxNormCode = "1545653", DrugTier = "preferred-brand", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = true, Copay = 30, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Diabetes - SGLT2 Inhibitor", DosageForm = "Tablet", Alternatives = new() { "Farxiga 10mg", "Invokana 100mg" } },
        new() { DrugId = "DRUG-008", DrugName = "Ozempic 1mg", GenericName = "Semaglutide", BrandName = "Ozempic", NdcCode = "00169-4132-12", RxNormCode = "1991302", DrugTier = "specialty", RequiresPriorAuth = true, QuantityLimit = true, StepTherapy = true, Copay = 0, CoinsurancePercent = 30, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Diabetes - GLP-1 Agonist", DosageForm = "Injection", Alternatives = new() { "Trulicity 1.5mg", "Victoza 1.8mg" } },

        // ── Pain / Anti-inflammatory ────────────────────────────
        new() { DrugId = "DRUG-009", DrugName = "Ibuprofen 200mg", GenericName = "Ibuprofen", BrandName = "Advil", NdcCode = "00573-0150-40", RxNormCode = "310965", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 5, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Pain - NSAID", DosageForm = "Tablet", Alternatives = new() { "Naproxen 250mg" } },
        new() { DrugId = "DRUG-010", DrugName = "Celebrex 200mg", GenericName = "Celecoxib", BrandName = "Celebrex", NdcCode = "00025-1525-31", RxNormCode = "205323", DrugTier = "non-preferred-brand", RequiresPriorAuth = true, QuantityLimit = false, StepTherapy = true, Copay = 50, CoinsurancePercent = 20, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Pain - NSAID", DosageForm = "Capsule", Alternatives = new() { "Ibuprofen 200mg", "Naproxen 250mg" } },

        // ── Respiratory ─────────────────────────────────────────
        new() { DrugId = "DRUG-011", DrugName = "Albuterol Inhaler", GenericName = "Albuterol Sulfate", BrandName = "ProAir HFA", NdcCode = "59310-0579-22", RxNormCode = "245314", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Respiratory - Bronchodilator", DosageForm = "Inhaler", Alternatives = new() { "Levalbuterol Inhaler" } },
        new() { DrugId = "DRUG-012", DrugName = "Advair Diskus 250/50", GenericName = "Fluticasone/Salmeterol", BrandName = "Advair", NdcCode = "00173-0696-00", RxNormCode = "896187", DrugTier = "preferred-brand", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 30, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Respiratory - ICS/LABA", DosageForm = "Inhaler", Alternatives = new() { "Symbicort 160/4.5" } },
        new() { DrugId = "DRUG-013", DrugName = "Dupixent 300mg", GenericName = "Dupilumab", BrandName = "Dupixent", NdcCode = "00024-5918-01", RxNormCode = "1876366", DrugTier = "specialty", RequiresPriorAuth = true, QuantityLimit = true, StepTherapy = true, Copay = 0, CoinsurancePercent = 30, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Respiratory - Biologic", DosageForm = "Injection", Alternatives = new() { "Nucala 100mg", "Xolair 150mg" } },

        // ── Mental Health ───────────────────────────────────────
        new() { DrugId = "DRUG-014", DrugName = "Sertraline 50mg", GenericName = "Sertraline", BrandName = "Zoloft", NdcCode = "00049-4960-30", RxNormCode = "312940", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Mental Health - SSRI", DosageForm = "Tablet", Alternatives = new() { "Fluoxetine 20mg", "Escitalopram 10mg" } },
        new() { DrugId = "DRUG-015", DrugName = "Lexapro 10mg", GenericName = "Escitalopram", BrandName = "Lexapro", NdcCode = "00456-1010-01", RxNormCode = "352741", DrugTier = "preferred-brand", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = true, Copay = 30, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Mental Health - SSRI", DosageForm = "Tablet", Alternatives = new() { "Sertraline 50mg", "Fluoxetine 20mg" } },

        // ── Antibiotics ─────────────────────────────────────────
        new() { DrugId = "DRUG-016", DrugName = "Amoxicillin 500mg", GenericName = "Amoxicillin", BrandName = "Amoxil", NdcCode = "66685-1004-01", RxNormCode = "308182", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 5, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Antibiotic - Penicillin", DosageForm = "Capsule", Alternatives = new() { "Augmentin 500mg" } },
        new() { DrugId = "DRUG-017", DrugName = "Azithromycin 250mg", GenericName = "Azithromycin", BrandName = "Zithromax", NdcCode = "00069-3060-75", RxNormCode = "248656", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 10, CoinsurancePercent = 0, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Antibiotic - Macrolide", DosageForm = "Tablet", Alternatives = new() { "Clarithromycin 500mg" } },

        // ── Oncology (Specialty) ────────────────────────────────
        new() { DrugId = "DRUG-018", DrugName = "Keytruda 200mg", GenericName = "Pembrolizumab", BrandName = "Keytruda", NdcCode = "00006-3029-02", RxNormCode = "1657981", DrugTier = "specialty", RequiresPriorAuth = true, QuantityLimit = true, StepTherapy = false, Copay = 0, CoinsurancePercent = 30, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Oncology - Immunotherapy", DosageForm = "IV Infusion", Alternatives = new() { "Opdivo 240mg" } },
        new() { DrugId = "DRUG-019", DrugName = "Ibrance 125mg", GenericName = "Palbociclib", BrandName = "Ibrance", NdcCode = "00069-0187-07", RxNormCode = "1601380", DrugTier = "specialty", RequiresPriorAuth = true, QuantityLimit = true, StepTherapy = false, Copay = 0, CoinsurancePercent = 30, PlanId = "PLAN-PPO-2026", PlanName = "Blue Cross PPO Gold 2026", TherapeuticClass = "Oncology - CDK 4/6 Inhibitor", DosageForm = "Capsule", Alternatives = new() { "Verzenio 150mg" } },

        // ── HMO Plan Drugs (different plan, same therapeutic areas) ──
        new() { DrugId = "DRUG-101", DrugName = "Lisinopril 10mg", GenericName = "Lisinopril", BrandName = "Prinivil", NdcCode = "00006-0106-54", RxNormCode = "314076", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 5, CoinsurancePercent = 0, PlanId = "PLAN-HMO-2026", PlanName = "Aetna HMO Silver 2026", TherapeuticClass = "Cardiovascular - ACE Inhibitor", DosageForm = "Tablet", Alternatives = new() { "Enalapril 10mg" } },
        new() { DrugId = "DRUG-102", DrugName = "Metformin 500mg", GenericName = "Metformin", BrandName = "Glucophage", NdcCode = "00087-6060-13", RxNormCode = "861004", DrugTier = "generic", RequiresPriorAuth = false, QuantityLimit = false, StepTherapy = false, Copay = 5, CoinsurancePercent = 0, PlanId = "PLAN-HMO-2026", PlanName = "Aetna HMO Silver 2026", TherapeuticClass = "Diabetes - Biguanide", DosageForm = "Tablet", Alternatives = new() { "Metformin 850mg" } },
        new() { DrugId = "DRUG-103", DrugName = "Ozempic 1mg", GenericName = "Semaglutide", BrandName = "Ozempic", NdcCode = "00169-4132-12", RxNormCode = "1991302", DrugTier = "specialty", RequiresPriorAuth = true, QuantityLimit = true, StepTherapy = true, Copay = 0, CoinsurancePercent = 35, PlanId = "PLAN-HMO-2026", PlanName = "Aetna HMO Silver 2026", TherapeuticClass = "Diabetes - GLP-1 Agonist", DosageForm = "Injection", Alternatives = new() { "Trulicity 1.5mg" } },
    };
}
