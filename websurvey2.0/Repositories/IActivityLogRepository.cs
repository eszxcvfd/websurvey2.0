using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLog log, CancellationToken ct = default);
}