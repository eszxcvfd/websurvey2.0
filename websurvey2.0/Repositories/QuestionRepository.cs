using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly SurveyDbContext _db;
    public QuestionRepository(SurveyDbContext db) => _db = db;

    public Task<List<Question>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default)
        => _db.Questions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .OrderBy(q => q.QuestionOrder)
            .ToListAsync(ct);

    public Task<Question?> GetByIdAsync(Guid questionId, CancellationToken ct = default)
        => _db.Questions.AsNoTracking().FirstOrDefaultAsync(q => q.QuestionId == questionId, ct);

    public Task<Question?> GetByIdTrackedAsync(Guid questionId, CancellationToken ct = default)
        => _db.Questions.Include(q => q.QuestionOptions)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId, ct);

    public async Task<int> GetNextOrderAsync(Guid surveyId, CancellationToken ct = default)
    {
        var max = await _db.Questions.AsNoTracking()
            .Where(q => q.SurveyId == surveyId)
            .Select(q => (int?)q.QuestionOrder)
            .MaxAsync(ct);
        return (max ?? 0) + 1;
    }

    public Task AddAsync(Question q, CancellationToken ct = default)
        => _db.Questions.AddAsync(q, ct).AsTask();

    public Task RemoveAsync(Question q, CancellationToken ct = default)
    {
        _db.Questions.Remove(q);
        return Task.CompletedTask;
    }

    public async Task UpdateOrdersAsync(Guid surveyId, IReadOnlyList<(Guid QuestionId, int Order)> orders, CancellationToken ct = default)
    {
        var ids = orders.Select(o => o.QuestionId).ToHashSet();
        var entities = await _db.Questions.Where(q => q.SurveyId == surveyId && ids.Contains(q.QuestionId)).ToListAsync(ct);
        foreach (var q in entities)
        {
            var o = orders.First(x => x.QuestionId == q.QuestionId);
            q.QuestionOrder = o.Order;
            q.UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}