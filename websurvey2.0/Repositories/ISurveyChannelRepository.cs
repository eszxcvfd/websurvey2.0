using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface ISurveyChannelRepository
{
    Task<List<SurveyChannel>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default);
    Task<SurveyChannel?> GetByIdAsync(Guid channelId, CancellationToken ct = default);
    Task<SurveyChannel?> GetByIdTrackedAsync(Guid channelId, CancellationToken ct = default); // NEW
    Task<SurveyChannel?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(SurveyChannel channel, CancellationToken ct = default);
    Task UpdateAsync(SurveyChannel channel, CancellationToken ct = default);
    Task RemoveAsync(SurveyChannel channel, CancellationToken ct = default);
}