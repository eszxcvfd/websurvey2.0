using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class SurveyResponseRepository : ISurveyResponseRepository
{
    private readonly SurveyDbContext _db;

    public SurveyResponseRepository(SurveyDbContext db) => _db = db;

    public Task<SurveyResponse?> GetByIdAsync(Guid responseId, CancellationToken ct = default)
        => _db.SurveyResponses
            .AsNoTracking()
            .Include(r => r.ResponseAnswers)
            .FirstOrDefaultAsync(r => r.ResponseId == responseId, ct);

    public Task<SurveyResponse?> GetByIdTrackedAsync(Guid responseId, CancellationToken ct = default)
        => _db.SurveyResponses
            .Include(r => r.ResponseAnswers)
            .FirstOrDefaultAsync(r => r.ResponseId == responseId, ct);

    public Task<int> GetResponseCountAsync(Guid surveyId, CancellationToken ct = default)
        => _db.SurveyResponses
            .AsNoTracking()
            .CountAsync(r => r.SurveyId == surveyId && r.Status == "Completed", ct);

    public Task<bool> HasRespondedAsync(Guid surveyId, string identifier, CancellationToken ct = default)
        => _db.SurveyResponses
            .AsNoTracking()
            .AnyAsync(r => r.SurveyId == surveyId 
                && (r.RespondentEmail == identifier || r.RespondentIp == identifier)
                && r.Status == "Completed", ct);

    public Task AddAsync(SurveyResponse response, CancellationToken ct = default)
        => _db.SurveyResponses.AddAsync(response, ct).AsTask();

    public Task<SurveyResponse?> GetByTokenAsync(string anonToken, CancellationToken ct = default)
        => _db.SurveyResponses
            .AsNoTracking()
            .Include(r => r.ResponseAnswers)
            .FirstOrDefaultAsync(r => r.AnonToken == anonToken, ct);
}