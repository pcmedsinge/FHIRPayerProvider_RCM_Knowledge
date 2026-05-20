using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace ProviderDirectoryAPI.Services;

public class ProviderService : IProviderService
{
    private readonly FhirClient _fhirClient;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(IConfiguration config, ILogger<ProviderService> logger)
    {
        _logger = logger;
        var fhirServerUrl = config["FhirServer:BaseUrl"] ?? "http://localhost:8082/fhir";
        var settings = new FhirClientSettings
        {
            PreferredFormat = ResourceFormat.Json
        };
        _fhirClient = new FhirClient(fhirServerUrl, settings);
    }

    // ─── Practitioner ────────────────────────────────────────────
    public async System.Threading.Tasks.Task<Practitioner?> GetPractitionerAsync(string id)
    {
        try
        {
            return await _fhirClient.ReadAsync<Practitioner>($"Practitioner/{id}");
        }
        catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Practitioner {Id} not found", id);
            return null;
        }
    }

    public async System.Threading.Tasks.Task<Bundle> SearchPractitionersAsync(
        string? name = null, string? specialty = null, int count = 20)
    {
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(name))
            sp.Where($"name={name}");
        // Specialty is not a direct Practitioner search param in HAPI; filter via PractitionerRole
        return await _fhirClient.SearchAsync<Practitioner>(sp) ?? new Bundle();
    }

    // ─── Organization ────────────────────────────────────────────
    public async System.Threading.Tasks.Task<Organization?> GetOrganizationAsync(string id)
    {
        try
        {
            return await _fhirClient.ReadAsync<Organization>($"Organization/{id}");
        }
        catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Organization {Id} not found", id);
            return null;
        }
    }

    public async System.Threading.Tasks.Task<Bundle> SearchOrganizationsAsync(
        string? name = null, string? type = null, string? address = null, int count = 20)
    {
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(name))    sp.Where($"name={name}");
        if (!string.IsNullOrEmpty(type))    sp.Where($"type={type}");
        if (!string.IsNullOrEmpty(address)) sp.Where($"address={address}");
        return await _fhirClient.SearchAsync<Organization>(sp) ?? new Bundle();
    }

    // ─── Location ────────────────────────────────────────────────
    public async System.Threading.Tasks.Task<Location?> GetLocationAsync(string id)
    {
        try
        {
            return await _fhirClient.ReadAsync<Location>($"Location/{id}");
        }
        catch (FhirOperationException ex) when (ex.Status == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Location {Id} not found", id);
            return null;
        }
    }

    public async System.Threading.Tasks.Task<Bundle> SearchLocationsAsync(
        string? name = null, string? city = null, string? state = null, int count = 20)
    {
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(name))  sp.Where($"name={name}");
        if (!string.IsNullOrEmpty(city))  sp.Where($"address-city={city}");
        if (!string.IsNullOrEmpty(state)) sp.Where($"address-state={state}");
        return await _fhirClient.SearchAsync<Location>(sp) ?? new Bundle();
    }

    // ─── PractitionerRole ────────────────────────────────────────
    public async System.Threading.Tasks.Task<Bundle> SearchPractitionerRolesAsync(
        string? practitioner = null, string? organization = null, string? specialty = null, int count = 20)
    {
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(practitioner)) sp.Where($"practitioner=Practitioner/{practitioner}");
        if (!string.IsNullOrEmpty(organization)) sp.Where($"organization=Organization/{organization}");
        if (!string.IsNullOrEmpty(specialty))    sp.Where($"specialty={specialty}");
        sp.Include("PractitionerRole:practitioner");
        sp.Include("PractitionerRole:organization");
        sp.Include("PractitionerRole:location");
        return await _fhirClient.SearchAsync<PractitionerRole>(sp) ?? new Bundle();
    }

    // ─── HealthcareService ───────────────────────────────────────
    public async System.Threading.Tasks.Task<Bundle> SearchHealthcareServicesAsync(
        string? organization = null, string? serviceType = null, string? specialty = null, int count = 20)
    {
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(organization)) sp.Where($"organization=Organization/{organization}");
        if (!string.IsNullOrEmpty(serviceType))  sp.Where($"service-type={serviceType}");
        if (!string.IsNullOrEmpty(specialty))    sp.Where($"specialty={specialty}");
        return await _fhirClient.SearchAsync<HealthcareService>(sp) ?? new Bundle();
    }

    // ─── Find a Doctor (Combined) ────────────────────────────────
    public async System.Threading.Tasks.Task<Bundle> FindProvidersAsync(
        string? name = null, string? specialty = null, string? city = null,
        string? state = null, string? organization = null, int count = 20)
    {
        // Strategy: Search PractitionerRoles with _include to pull related resources
        var sp = new SearchParams().LimitTo(count);
        if (!string.IsNullOrEmpty(specialty))    sp.Where($"specialty={specialty}");
        if (!string.IsNullOrEmpty(organization)) sp.Where($"organization=Organization/{organization}");
        sp.Include("PractitionerRole:practitioner");
        sp.Include("PractitionerRole:organization");
        sp.Include("PractitionerRole:location");

        var roleBundle = await _fhirClient.SearchAsync<PractitionerRole>(sp) ?? new Bundle();

        // Client-side filtering by name and location
        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(state))
        {
            var practitioners = roleBundle.Entry?
                .Where(e => e.Resource is Practitioner)
                .Select(e => e.Resource as Practitioner)
                .ToList() ?? new List<Practitioner?>();

            var locations = roleBundle.Entry?
                .Where(e => e.Resource is Location)
                .Select(e => e.Resource as Location)
                .ToList() ?? new List<Location?>();

            var matchedPractitionerIds = new HashSet<string>();

            // Filter by name
            if (!string.IsNullOrEmpty(name))
            {
                foreach (var p in practitioners.Where(p => p != null))
                {
                    var fullName = string.Join(" ", p!.Name.SelectMany(n => n.Given).Concat(p.Name.Select(n => n.Family)));
                    if (fullName.Contains(name, StringComparison.OrdinalIgnoreCase))
                        matchedPractitionerIds.Add(p.Id);
                }
            }

            // Filter locations by city/state
            var matchedLocationIds = new HashSet<string>();
            if (!string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(state))
            {
                foreach (var l in locations.Where(l => l != null))
                {
                    bool matches = true;
                    if (!string.IsNullOrEmpty(city) && l!.Address?.City?.Contains(city, StringComparison.OrdinalIgnoreCase) != true)
                        matches = false;
                    if (!string.IsNullOrEmpty(state) && l!.Address?.State?.Contains(state, StringComparison.OrdinalIgnoreCase) != true)
                        matches = false;
                    if (matches) matchedLocationIds.Add(l!.Id);
                }
            }

            // Filter PractitionerRoles by matched practitioners and locations
            var filteredEntries = roleBundle.Entry?
                .Where(e =>
                {
                    if (e.Resource is PractitionerRole role)
                    {
                        bool nameMatch = string.IsNullOrEmpty(name) || 
                            (role.Practitioner?.Reference != null && matchedPractitionerIds.Any(id => role.Practitioner.Reference.Contains(id)));
                        bool locationMatch = (string.IsNullOrEmpty(city) && string.IsNullOrEmpty(state)) ||
                            role.Location?.Any(l => l.Reference != null && matchedLocationIds.Any(id => l.Reference.Contains(id))) == true;
                        return nameMatch && locationMatch;
                    }
                    return true; // keep included resources
                })
                .ToList() ?? new List<Bundle.EntryComponent>();

            roleBundle.Entry = filteredEntries;
            roleBundle.Total = filteredEntries.Count(e => e.Resource is PractitionerRole);
        }

        return roleBundle;
    }

    // ─── Practitioner Detail Bundle ──────────────────────────────
    public async System.Threading.Tasks.Task<Bundle> GetPractitionerDetailsAsync(string practitionerId)
    {
        var resultBundle = new Bundle { Type = Bundle.BundleType.Collection };

        // Get the practitioner
        var practitioner = await GetPractitionerAsync(practitionerId);
        if (practitioner != null)
        {
            resultBundle.Entry.Add(new Bundle.EntryComponent { Resource = practitioner });
        }

        // Get their PractitionerRoles with includes
        var roles = await SearchPractitionerRolesAsync(practitioner: practitionerId, count: 50);
        if (roles.Entry != null)
        {
            foreach (var entry in roles.Entry)
            {
                resultBundle.Entry.Add(entry);
            }
        }

        resultBundle.Total = resultBundle.Entry.Count;
        return resultBundle;
    }
}
