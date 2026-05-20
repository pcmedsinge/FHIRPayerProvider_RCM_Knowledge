namespace FormularyAPI.Models;

/// <summary>
/// In-memory formulary drug entry representing a drug on a payer's formulary.
/// Maps to DaVinci FormularyItem (Basic) and FormularyDrug (MedicationKnowledge).
/// </summary>
public class FormularyDrug
{
    public string DrugId { get; set; } = "";
    public string DrugName { get; set; } = "";
    public string GenericName { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string NdcCode { get; set; } = "";
    public string RxNormCode { get; set; } = "";
    public string DrugTier { get; set; } = ""; // generic, preferred-brand, non-preferred-brand, specialty
    public bool RequiresPriorAuth { get; set; }
    public bool QuantityLimit { get; set; }
    public bool StepTherapy { get; set; }
    public decimal Copay { get; set; }
    public decimal CoinsurancePercent { get; set; }
    public string PlanId { get; set; } = "";
    public string PlanName { get; set; } = "";
    public string TherapeuticClass { get; set; } = "";
    public string DosageForm { get; set; } = "";
    public List<string> Alternatives { get; set; } = new();
}

public class FormularyPlan
{
    public string PlanId { get; set; } = "";
    public string PlanName { get; set; } = "";
    public string PayerName { get; set; } = "";
    public string PlanType { get; set; } = ""; // HMO, PPO, EPO
    public int PlanYear { get; set; } = 2026;
    public List<DrugTierDefinition> DrugTiers { get; set; } = new();
}

public class DrugTierDefinition
{
    public string TierName { get; set; } = "";
    public string TierDescription { get; set; } = "";
    public decimal DefaultCopay { get; set; }
    public decimal DefaultCoinsurance { get; set; }
    public bool MailOrderAvailable { get; set; }
}

public class CoverageCheckRequest
{
    public string DrugName { get; set; } = "";
    public string? RxNormCode { get; set; }
    public string? PlanId { get; set; }
}

public class CoverageCheckResponse
{
    public bool IsCovered { get; set; }
    public string DrugName { get; set; } = "";
    public string? DrugTier { get; set; }
    public decimal? EstimatedCopay { get; set; }
    public decimal? CoinsurancePercent { get; set; }
    public bool RequiresPriorAuth { get; set; }
    public bool QuantityLimit { get; set; }
    public bool StepTherapy { get; set; }
    public List<string> Alternatives { get; set; } = new();
    public string? PlanName { get; set; }
    public string? Message { get; set; }
}
