namespace CRDService.Models;

// ─── CDS Hooks Discovery ────────────────────────────────────────

public class CdsServiceDefinition
{
    public string Hook { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Id { get; set; } = "";
    public CdsPrefetch? Prefetch { get; set; }
}

public class CdsPrefetch
{
    public string? Patient { get; set; }
    public string? Coverage { get; set; }
    public string? Conditions { get; set; }
    public string? Medications { get; set; }
}

public class CdsDiscoveryResponse
{
    public List<CdsServiceDefinition> Services { get; set; } = new();
}

// ─── CDS Hooks Request ──────────────────────────────────────────

public class CdsHookRequest
{
    public string HookInstance { get; set; } = "";
    public string FhirServer { get; set; } = "";
    public string Hook { get; set; } = "";
    public CdsContext Context { get; set; } = new();
    public Dictionary<string, object>? Prefetch { get; set; }
}

public class CdsContext
{
    public string? UserId { get; set; }
    public string? PatientId { get; set; }
    public string? EncounterId { get; set; }
    public List<CdsSelection>? Selections { get; set; }
    public object? DraftOrders { get; set; }
}

public class CdsSelection
{
    public string? ResourceType { get; set; }
    public string? Code { get; set; }
    public string? Display { get; set; }
}

// ─── CDS Hooks Response ─────────────────────────────────────────

public class CdsHookResponse
{
    public List<CdsCard> Cards { get; set; } = new();
    public List<CdsSystemAction>? SystemActions { get; set; }
}

public class CdsCard
{
    public string Uuid { get; set; } = Guid.NewGuid().ToString();
    public string Summary { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Indicator { get; set; } = "info"; // info, warning, critical
    public CdsSource Source { get; set; } = new();
    public List<CdsSuggestion>? Suggestions { get; set; }
    public List<CdsLink>? Links { get; set; }
    public bool? SelectionBehavior { get; set; }
}

public class CdsSource
{
    public string Label { get; set; } = "";
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public string? Topic { get; set; }
}

public class CdsSuggestion
{
    public string Label { get; set; } = "";
    public string? Uuid { get; set; }
    public bool? IsRecommended { get; set; }
    public List<CdsSuggestionAction>? Actions { get; set; }
}

public class CdsSuggestionAction
{
    public string Type { get; set; } = "create"; // create, update, delete
    public string Description { get; set; } = "";
    public object? Resource { get; set; }
}

public class CdsLink
{
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    public string Type { get; set; } = "absolute"; // absolute, smart
    public string? AppContext { get; set; }
}

public class CdsSystemAction
{
    public string Type { get; set; } = "update";
    public string Description { get; set; } = "";
    public object? Resource { get; set; }
}
