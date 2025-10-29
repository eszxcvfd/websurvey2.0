using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface IQuestionRepository
{
    Task<List<Question>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default);
    Task<Question?> GetByIdAsync(Guid questionId, CancellationToken ct = default);
    Task<Question?> GetByIdTrackedAsync(Guid questionId, CancellationToken ct = default);
    Task<int> GetNextOrderAsync(Guid surveyId, CancellationToken ct = default);
    Task AddAsync(Question q, CancellationToken ct = default);
    Task RemoveAsync(Question q, CancellationToken ct = default);
    Task UpdateOrdersAsync(Guid surveyId, IReadOnlyList<(Guid QuestionId, int Order)> orders, CancellationToken ct = default);
}