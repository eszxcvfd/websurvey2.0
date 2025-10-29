using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface ISurveyRepository
{
    Task AddAsync(Survey survey, CancellationToken ct = default);
    Task<Survey?> GetByIdAsync(Guid surveyId, CancellationToken ct = default);
    Task<Survey?> GetByIdTrackedAsync(Guid surveyId, CancellationToken ct = default);
    Task<List<Survey>> GetOwnedByAsync(Guid ownerId, CancellationToken ct = default);
    Task<List<Survey>> GetSharedWithAsync(Guid userId, CancellationToken ct = default);
}