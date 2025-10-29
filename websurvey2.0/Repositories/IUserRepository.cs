using websurvey2._0.Models;

namespace websurvey2._0.Repositories;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateProfileAsync(User user, CancellationToken ct = default);
    Task UpdatePasswordHashAsync(User user, byte[] newHash, CancellationToken ct = default);
    Task SetResetTokenAsync(User user, string token, DateTime expiry, CancellationToken ct = default);
    Task ClearResetTokenAsync(User user, CancellationToken ct = default);
}