using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface IResponseAnswerRepository
{
    Task<List<ResponseAnswer>> GetByResponseAsync(Guid responseId, CancellationToken ct = default);
    Task AddAsync(ResponseAnswer answer, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<ResponseAnswer> answers, CancellationToken ct = default);
    Task<ResponseAnswer?> GetAsync(Guid responseId, Guid questionId, CancellationToken ct = default);
}