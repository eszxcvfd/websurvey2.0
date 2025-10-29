using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class SurveyChannelRepository : ISurveyChannelRepository
{
    private readonly SurveyDbContext _db;

    public SurveyChannelRepository(SurveyDbContext db)
    {
        _db = db;
    }

    public async Task<List<SurveyChannel>> GetBySurveyAsync(Guid surveyId, CancellationToken ct = default)
        => await _db.Set<SurveyChannel>()
            .AsNoTracking()
            .Where(c => c.SurveyId == surveyId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<SurveyChannel?> GetByIdAsync(Guid channelId, CancellationToken ct = default)
        => await _db.Set<SurveyChannel>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChannelId == channelId, ct);

    // NEW: Get tracked entity for updates
    public async Task<SurveyChannel?> GetByIdTrackedAsync(Guid channelId, CancellationToken ct = default)
        => await _db.Set<SurveyChannel>()
        // NO AsNoTracking() here - we want tracking for updates
            .FirstOrDefaultAsync(c => c.ChannelId == channelId, ct);

    public async Task<SurveyChannel?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _db.Set<SurveyChannel>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicUrlSlug == slug, ct);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
        => await _db.Set<SurveyChannel>()
            .AsNoTracking()
            .AnyAsync(c => c.PublicUrlSlug == slug, ct);

    public async Task AddAsync(SurveyChannel channel, CancellationToken ct = default)
    {
        await _db.Set<SurveyChannel>().AddAsync(channel, ct);
    }

    public Task UpdateAsync(SurveyChannel channel, CancellationToken ct = default)
    {
        _db.Set<SurveyChannel>().Update(channel);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(SurveyChannel channel, CancellationToken ct = default)
    {
        _db.Set<SurveyChannel>().Remove(channel);
        return Task.CompletedTask;
    }
}