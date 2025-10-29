using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class CollaboratorRepository : ICollaboratorRepository
{
    private readonly SurveyDbContext _db;
    public CollaboratorRepository(SurveyDbContext db) => _db = db;

    public Task<List<SurveyCollaborator>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default)
        => _db.SurveyCollaborators.AsNoTracking()
            .Where(c => c.SurveyId == surveyId)
            .OrderBy(c => c.Role)
            .ToListAsync(ct);

    public Task<SurveyCollaborator?> GetAsync(Guid surveyId, Guid userId, CancellationToken ct = default)
        => _db.SurveyCollaborators.FirstOrDefaultAsync(c => c.SurveyId == surveyId && c.UserId == userId, ct);

    public async Task AddOrUpdateAsync(Guid surveyId, Guid userId, string role, Guid? grantedBy, CancellationToken ct = default)
    {
        var existing = await _db.SurveyCollaborators.FirstOrDefaultAsync(c => c.SurveyId == surveyId && c.UserId == userId, ct);
        if (existing is null)
        {
            await _db.SurveyCollaborators.AddAsync(new SurveyCollaborator
            {
                SurveyId = surveyId,
                UserId = userId,
                Role = role,
                GrantedAtUtc = DateTime.UtcNow,
                GrantedBy = grantedBy
            }, ct);
        }
        else
        {
            existing.Role = role;
            existing.GrantedAtUtc = DateTime.UtcNow;
            existing.GrantedBy = grantedBy;
        }
    }

    public async Task RemoveAsync(Guid surveyId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _db.SurveyCollaborators.FirstOrDefaultAsync(c => c.SurveyId == surveyId && c.UserId == userId, ct);
        if (existing is not null)
        {
            _db.SurveyCollaborators.Remove(existing);
        }
    }

    public async Task<Dictionary<Guid, string>> GetRolesForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.SurveyCollaborators.AsNoTracking()
            .Where(c => c.UserId == userId)
            .GroupBy(c => c.SurveyId)
            .Select(g => g.OrderByDescending(x => x.Role).First()) // nếu nhiều role, mặc định pick theo alpha; bạn có thể map theo rank
            .ToDictionaryAsync(c => c.SurveyId, c => c.Role, ct);
    }
}