using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace MemberAccessAPI.Services;

public interface IFhirService
{
    Task<Patient?> GetPatientAsync(string patientId);
    Task<Bundle> SearchEOBByPatientAsync(string patientId, string? type = null, 
        string? startDate = null, string? endDate = null, int count = 10);
    Task<Bundle> SearchCoverageByPatientAsync(string patientId);
    Task<Bundle> GetPatientEverythingAsync(string patientId);
}
