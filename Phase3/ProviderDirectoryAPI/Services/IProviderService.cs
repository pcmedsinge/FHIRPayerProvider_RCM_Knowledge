using Hl7.Fhir.Model;

namespace ProviderDirectoryAPI.Services;

public interface IProviderService
{
    // Practitioner operations
    System.Threading.Tasks.Task<Practitioner?> GetPractitionerAsync(string id);
    System.Threading.Tasks.Task<Bundle> SearchPractitionersAsync(string? name = null, string? specialty = null, int count = 20);
    
    // Organization operations
    System.Threading.Tasks.Task<Organization?> GetOrganizationAsync(string id);
    System.Threading.Tasks.Task<Bundle> SearchOrganizationsAsync(string? name = null, string? type = null, string? address = null, int count = 20);
    
    // Location operations
    System.Threading.Tasks.Task<Location?> GetLocationAsync(string id);
    System.Threading.Tasks.Task<Bundle> SearchLocationsAsync(string? name = null, string? city = null, string? state = null, int count = 20);
    
    // PractitionerRole - links practitioners to organizations/locations/specialties
    System.Threading.Tasks.Task<Bundle> SearchPractitionerRolesAsync(string? practitioner = null, string? organization = null, string? specialty = null, int count = 20);
    
    // HealthcareService
    System.Threading.Tasks.Task<Bundle> SearchHealthcareServicesAsync(string? organization = null, string? serviceType = null, string? specialty = null, int count = 20);

    // Combined "Find a Doctor" query
    System.Threading.Tasks.Task<Bundle> FindProvidersAsync(string? name = null, string? specialty = null, string? city = null, string? state = null, string? organization = null, int count = 20);
    
    // $everything-style: get practitioner with roles, locations, orgs
    System.Threading.Tasks.Task<Bundle> GetPractitionerDetailsAsync(string practitionerId);
}
