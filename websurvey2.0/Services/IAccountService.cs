using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public interface IAccountService
{
    Task<(bool Success, string? Error, ProfileViewModel? Profile)> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateProfileAsync(Guid userId, ProfileViewModel vm, string? ip, string? ua, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors)> ChangePasswordAsync(Guid userId, ChangePasswordViewModel vm, string? ip, string? ua, CancellationToken ct = default);
}