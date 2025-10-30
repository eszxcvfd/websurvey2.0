using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface ISurveyResponseRepository
{
    Task<SurveyResponse?> GetByIdAsync(Guid responseId, CancellationToken ct = default);
    Task<SurveyResponse?> GetByIdTrackedAsync(Guid responseId, CancellationToken ct = default);
    Task<int> GetResponseCountAsync(Guid surveyId, CancellationToken ct = default);
    Task<bool> HasRespondedAsync(Guid surveyId, string identifier, CancellationToken ct = default);
    Task AddAsync(SurveyResponse response, CancellationToken ct = default);
    Task<SurveyResponse?> GetByTokenAsync(string anonToken, CancellationToken ct = default);
}