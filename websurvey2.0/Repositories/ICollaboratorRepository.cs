using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface ICollaboratorRepository
{
    Task<List<SurveyCollaborator>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default);
    Task<SurveyCollaborator?> GetAsync(Guid surveyId, Guid userId, CancellationToken ct = default);
    Task AddOrUpdateAsync(Guid surveyId, Guid userId, string role, Guid? grantedBy, CancellationToken ct = default);
    Task RemoveAsync(Guid surveyId, Guid userId, CancellationToken ct = default);

    // NEW: lấy role cho tất cả survey của 1 user
    Task<Dictionary<Guid, string>> GetRolesForUserAsync(Guid userId, CancellationToken ct = default);
}