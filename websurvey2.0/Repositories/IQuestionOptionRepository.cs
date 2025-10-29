using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface IQuestionOptionRepository
{
    Task<List<QuestionOption>> GetByQuestionAsync(Guid questionId, CancellationToken ct = default);
    Task AddAsync(QuestionOption option, CancellationToken ct = default);
    Task RemoveAsync(QuestionOption option, CancellationToken ct = default);
}