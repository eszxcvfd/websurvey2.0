using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly SurveyDbContext _db;
    public ActivityLogRepository(SurveyDbContext db) => _db = db;

    public Task AddAsync(ActivityLog log, CancellationToken ct = default)
        => _db.ActivityLogs.AddAsync(log, ct).AsTask();
}