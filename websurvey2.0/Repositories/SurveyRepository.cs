using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class SurveyRepository : ISurveyRepository
{
    private readonly SurveyDbContext _db;
    public SurveyRepository(SurveyDbContext db) => _db = db;

    public Task AddAsync(Survey survey, CancellationToken ct = default)
        => _db.Surveys.AddAsync(survey, ct).AsTask();

    public Task<Survey?> GetByIdAsync(Guid surveyId, CancellationToken ct = default)
        => _db.Surveys.AsNoTracking().FirstOrDefaultAsync(s => s.SurveyId == surveyId, ct);

    public Task<Survey?> GetByIdTrackedAsync(Guid surveyId, CancellationToken ct = default)
        => _db.Surveys.FirstOrDefaultAsync(s => s.SurveyId == surveyId, ct);

    public Task<List<Survey>> GetOwnedByAsync(Guid ownerId, CancellationToken ct = default)
        => _db.Surveys.AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<List<Survey>> GetSharedWithAsync(Guid userId, CancellationToken ct = default)
        => _db.Surveys.AsNoTracking()
            .Where(s => _db.SurveyCollaborators.Any(c => c.SurveyId == s.SurveyId && c.UserId == userId))
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);
}