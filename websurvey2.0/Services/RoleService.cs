using websurvey2._0.Models;
using websurvey2._0.Repositories;

namespace websurvey2._0.Services;

public class RoleService : IRoleService
{
    // Admin reserved, không assign qua UI. Nhưng vẫn coi rank cao hơn Editor nếu đã tồn tại.
    private static readonly Dictionary<string, int> RoleRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Viewer"] = 1,
        ["Editor"] = 2,
        ["Admin"]  = 3, // reserved
        ["Owner"]  = 4
    };

    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Viewer", "Editor" // Admin reserved
    };

    private readonly ISurveyRepository _surveys;
    private readonly ICollaboratorRepository _collabs;
    private readonly IActivityLogRepository _logs;
    private readonly SurveyDbContext _db;

    public RoleService(ISurveyRepository surveys, ICollaboratorRepository collabs, IActivityLogRepository logs, SurveyDbContext db)
    {
        _surveys = surveys;
        _collabs = collabs;
        _logs = logs;
        _db = db;
    }

    private async Task<(bool Can, string? Error)> CheckPermissionToManageAccessAsync(Guid actingUserId, Survey survey, CancellationToken ct)
    {
        // Chỉ Owner được quản lý quyền (Admin reserved cho mục đích khác)
        if (actingUserId == survey.OwnerId) return (true, null);
        return (false, "Only Owner can manage access.");
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> AssignRoleAsync(Guid actingUserId, Guid surveyId, Guid targetUserId, string role, CancellationToken ct = default)
    {
        if (!AllowedRoles.Contains(role))
            return (false, new[] { "Invalid role. Allowed: Viewer, Editor. Admin is reserved." });

        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        var (can, err) = await CheckPermissionToManageAccessAsync(actingUserId, survey, ct);
        if (!can) return (false, new[] { err! });

        if (targetUserId == survey.OwnerId)
            return (false, new[] { "Cannot change Owner role." });

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _collabs.AddOrUpdateAsync(surveyId, targetUserId, role, actingUserId, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = surveyId,
                ActionType = "SurveyRoleAssigned",
                ActionDetail = $"User {actingUserId} granted '{role}' to {targetUserId}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Assign role failed." });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> RemoveRoleAsync(Guid actingUserId, Guid surveyId, Guid targetUserId, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        var (can, err) = await CheckPermissionToManageAccessAsync(actingUserId, survey, ct);
        if (!can) return (false, new[] { err! });

        if (targetUserId == survey.OwnerId)
            return (false, new[] { "Cannot remove Owner." });

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _collabs.RemoveAsync(surveyId, targetUserId, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = surveyId,
                ActionType = "SurveyRoleRemoved",
                ActionDetail = $"User {actingUserId} removed access of {targetUserId}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Remove role failed." });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors, List<SurveyCollaborator> Collaborators)> GetCollaboratorsAsync(Guid actingUserId, Guid surveyId, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." }, new());

        var (can, err) = await CheckPermissionToManageAccessAsync(actingUserId, survey, ct);
        if (!can) return (false, new[] { err! }, new());

        var list = await _collabs.GetBySurveyAsync(surveyId, ct);
        return (true, Array.Empty<string>(), list);
    }

    // NEW: kiểm soát thao tác theo quyền
    public async Task<(bool Allowed, string? Error, string? EffectiveRole)> CheckPermissionAsync(Guid userId, Guid surveyId, string action, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null) return (false, "Survey not found.", null);

        string effectiveRole;
        if (userId == survey.OwnerId)
        {
            effectiveRole = "Owner";
        }
        else
        {
            var collab = await _collabs.GetAsync(surveyId, userId, ct);
            if (collab is null)
                return (false, "Access denied.", null);

            effectiveRole = collab.Role;
        }

        int rank = RoleRank.TryGetValue(effectiveRole, out var r) ? r : 0;
        int required = action switch
        {
            "EditQuestion"   => RoleRank["Editor"],   // Editor trở lên (rank >= 2)
            "EditSurvey"     => RoleRank["Owner"],    // Chỉ Owner (rank >= 4)
            "Publish"        => RoleRank["Owner"],    // Chỉ Owner (rank >= 4)
            "ViewReport"     => RoleRank["Viewer"],   // Viewer trở lên (rank >= 1)
            "ManageSettings" => RoleRank["Owner"],    // Chỉ Owner (rank >= 4)
            "Settings"       => RoleRank["Owner"],    // Alias cho ManageSettings
            _                => RoleRank["Viewer"]    // Default: Viewer
        };

        if (rank >= required) return (true, null, effectiveRole);
        return (false, $"Insufficient permission for action '{action}'.", effectiveRole);
    }
}