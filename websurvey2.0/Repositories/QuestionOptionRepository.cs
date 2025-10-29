using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class QuestionOptionRepository : IQuestionOptionRepository
{
    private readonly SurveyDbContext _db;
    public QuestionOptionRepository(SurveyDbContext db) => _db = db;

    public Task<List<QuestionOption>> GetByQuestionAsync(Guid questionId, CancellationToken ct = default)
        => _db.QuestionOptions.AsNoTracking()
            .Where(o => o.QuestionId == questionId)
            .OrderBy(o => o.OptionOrder)
            .ToListAsync(ct);

    public Task AddAsync(QuestionOption option, CancellationToken ct = default)
        => _db.QuestionOptions.AddAsync(option, ct).AsTask();

    public Task RemoveAsync(QuestionOption option, CancellationToken ct = default)
    {
        _db.QuestionOptions.Remove(option);
        return Task.CompletedTask;
    }
}