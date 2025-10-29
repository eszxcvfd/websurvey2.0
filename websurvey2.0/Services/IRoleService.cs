using websurvey2._0.Models;

namespace websurvey2._0.Services;

public interface IRoleService
{
    Task<(bool Success, IEnumerable<string> Errors)> AssignRoleAsync(Guid actingUserId, Guid surveyId, Guid targetUserId, string role, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> RemoveRoleAsync(Guid actingUserId, Guid surveyId, Guid targetUserId, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors, List<SurveyCollaborator> Collaborators)> GetCollaboratorsAsync(Guid actingUserId, Guid surveyId, CancellationToken ct = default);

    // NEW: Kiểm tra quyền theo action
    Task<(bool Allowed, string? Error, string? EffectiveRole)> CheckPermissionAsync(Guid userId, Guid surveyId, string action, CancellationToken ct = default);
}