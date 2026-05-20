using BulkDataAPI.Models;

namespace BulkDataAPI.Services;

/// <summary>
/// Group management for group-level bulk exports.
/// </summary>
public interface IGroupService
{
    PatientGroup CreateGroup(string name, string description, List<string> patientIds);
    PatientGroup? GetGroup(string groupId);
    List<PatientGroup> GetAllGroups();
    PatientGroup? AddMembers(string groupId, List<string> patientIds);
    PatientGroup? RemoveMembers(string groupId, List<string> patientIds);
    bool DeleteGroup(string groupId);
}

public class GroupService : IGroupService
{
    private readonly List<PatientGroup> _groups;
    private readonly ILogger<GroupService> _logger;

    public GroupService(ILogger<GroupService> logger)
    {
        _logger = logger;

        // Pre-configured groups matching HAPI FHIR patient data
        _groups = new List<PatientGroup>
        {
            new()
            {
                GroupId = "GRP-DIABETES",
                Name = "Diabetes Care Cohort",
                Description = "Patients with Type 2 Diabetes for quality reporting",
                MemberPatientIds = new List<string> { "51707", "52458", "65520" }
            },
            new()
            {
                GroupId = "GRP-CARDIAC",
                Name = "Cardiac Risk Group",
                Description = "Patients with cardiovascular risk factors",
                MemberPatientIds = new List<string> { "51707", "52458" }
            },
            new()
            {
                GroupId = "GRP-HEDIS",
                Name = "HEDIS Reporting Population",
                Description = "Members included in HEDIS quality measure reporting",
                MemberPatientIds = new List<string> { "51707", "52458", "65520" }
            },
            new()
            {
                GroupId = "GRP-HIGHRISK",
                Name = "High-Risk Patients",
                Description = "Patients requiring care management",
                MemberPatientIds = new List<string> { "52458" }
            }
        };
    }

    public PatientGroup CreateGroup(string name, string description, List<string> patientIds)
    {
        var group = new PatientGroup
        {
            GroupId = $"GRP-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            Name = name,
            Description = description,
            MemberPatientIds = patientIds
        };
        _groups.Add(group);
        _logger.LogInformation("Group {GroupId} created with {Count} members", group.GroupId, patientIds.Count);
        return group;
    }

    public PatientGroup? GetGroup(string groupId) =>
        _groups.FirstOrDefault(g => g.GroupId == groupId);

    public List<PatientGroup> GetAllGroups() => _groups.ToList();

    public PatientGroup? AddMembers(string groupId, List<string> patientIds)
    {
        var group = _groups.FirstOrDefault(g => g.GroupId == groupId);
        if (group == null) return null;

        foreach (var id in patientIds)
        {
            if (!group.MemberPatientIds.Contains(id))
                group.MemberPatientIds.Add(id);
        }
        return group;
    }

    public PatientGroup? RemoveMembers(string groupId, List<string> patientIds)
    {
        var group = _groups.FirstOrDefault(g => g.GroupId == groupId);
        if (group == null) return null;

        group.MemberPatientIds.RemoveAll(id => patientIds.Contains(id));
        return group;
    }

    public bool DeleteGroup(string groupId)
    {
        return _groups.RemoveAll(g => g.GroupId == groupId) > 0;
    }
}
