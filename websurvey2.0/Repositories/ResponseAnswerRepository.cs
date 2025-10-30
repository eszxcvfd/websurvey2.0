using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class ResponseAnswerRepository : IResponseAnswerRepository
{
    private readonly SurveyDbContext _db;

    public ResponseAnswerRepository(SurveyDbContext db) => _db = db;

    public Task<List<ResponseAnswer>> GetByResponseAsync(Guid responseId, CancellationToken ct = default)
        => _db.ResponseAnswers
            .AsNoTracking()
            .Where(a => a.ResponseId == responseId)
            .ToListAsync(ct);

    public Task AddAsync(ResponseAnswer answer, CancellationToken ct = default)
        => _db.ResponseAnswers.AddAsync(answer, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<ResponseAnswer> answers, CancellationToken ct = default)
        => _db.ResponseAnswers.AddRangeAsync(answers, ct);

    public Task<ResponseAnswer?> GetAsync(Guid responseId, Guid questionId, CancellationToken ct = default)
        => _db.ResponseAnswers
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ResponseId == responseId && a.QuestionId == questionId, ct);
}