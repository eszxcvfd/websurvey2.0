using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SurveyDbContext _db;
    public UserRepository(SurveyDbContext db) => _db = db;

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => _db.Users.AsNoTracking().AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task UpdateProfileAsync(User user, CancellationToken ct = default)
    {
        user.UpdatedAtUtc = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task UpdatePasswordHashAsync(User user, byte[] newHash, CancellationToken ct = default)
    {
        user.PasswordHash = newHash;
        user.UpdatedAtUtc = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task SetResetTokenAsync(User user, string token, DateTime expiry, CancellationToken ct = default)
    {
        user.ResetToken = token;
        user.ResetTokenExpiry = expiry;
        user.UpdatedAtUtc = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task ClearResetTokenAsync(User user, CancellationToken ct = default)
    {
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}