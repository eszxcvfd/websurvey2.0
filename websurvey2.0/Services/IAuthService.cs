using websurvey2._0.ViewModels;
using websurvey2._0.Models;

namespace websurvey2._0.Services;

public interface IAuthService
{
    Task<(bool Success, IEnumerable<string> Errors)> RegisterAsync(RegisterViewModel model, string? ip, string? userAgent, CancellationToken ct = default);
    Task<(bool Success, IEnumerable<string> Errors, User? User)> LoginAsync(LoginViewModel model, string? ip, string? userAgent, CancellationToken ct = default);
    Task LogLogoutAsync(Guid userId, string? ip, string? userAgent, CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors)> RequestPasswordResetAsync(
        ForgotPasswordViewModel model,
        string? ip,
        string? userAgent,
        Func<string, string> buildResetLink,
        CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors)> ResetPasswordAsync(
        ResetPasswordViewModel model,
        string? ip,
        string? userAgent,
        CancellationToken ct = default);
}