using Microsoft.AspNetCore.Identity;
using websurvey2._0.Models;
using websurvey2._0.Repositories;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public class AccountService : IAccountService
{
    private readonly IUserRepository _users;
    private readonly IActivityLogRepository _logs;
    private readonly SurveyDbContext _db;

    public AccountService(IUserRepository users, IActivityLogRepository logs, SurveyDbContext db)
    {
        _users = users;
        _logs = logs;
        _db = db;
    }

    public async Task<(bool Success, string? Error, ProfileViewModel? Profile)> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return (false, "User not found.", null);

        var vm = new ProfileViewModel
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl
        };
        return (true, null, vm);
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateProfileAsync(Guid userId, ProfileViewModel vm, string? ip, string? ua, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return (false, new[] { "User not found." });

        user.FullName = string.IsNullOrWhiteSpace(vm.FullName) ? null : vm.FullName.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(vm.AvatarUrl) ? null : vm.AvatarUrl.Trim();

        await _users.UpdateProfileAsync(user, ct);
        await _logs.AddAsync(new ActivityLog
        {
            UserId = user.UserId,
            ActionType = "ProfileUpdated",
            ActionDetail = $"Profile updated. IP={ip ?? "N/A"}, UA={(ua ?? "N/A")}"
        }, ct);

        await _db.SaveChangesAsync(ct);
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ChangePasswordAsync(Guid userId, ChangePasswordViewModel vm, string? ip, string? ua, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return (false, new[] { "User not found." });

        var hasher = new PasswordHasher<User>();
        var stored = Convert.ToBase64String(user.PasswordHash);
        var verify = hasher.VerifyHashedPassword(user, stored, vm.CurrentPassword);
        if (verify != PasswordVerificationResult.Success && verify != PasswordVerificationResult.SuccessRehashNeeded)
        {
            return (false, new[] { "Current password is incorrect." });
        }

        // Optional: disallow email local-part in password
        var emailLocal = user.Email.Split('@')[0];
        if (vm.NewPassword.Contains(emailLocal, StringComparison.OrdinalIgnoreCase))
            return (false, new[] { "New password must not contain parts of the email." });

        var newHashString = hasher.HashPassword(user, vm.NewPassword);
        var newHashBytes = Convert.FromBase64String(newHashString);

        await _users.UpdatePasswordHashAsync(user, newHashBytes, ct);
        user.FailedLoginCount = 0;
        user.LockedUntilUtc = null;

        await _logs.AddAsync(new ActivityLog
        {
            UserId = user.UserId,
            ActionType = "PasswordChanged",
            ActionDetail = $"Password changed. IP={ip ?? "N/A"}, UA={(ua ?? "N/A")}"
        }, ct);

        await _db.SaveChangesAsync(ct);
        return (true, Array.Empty<string>());
    }
}