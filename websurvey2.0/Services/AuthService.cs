using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;
using websurvey2._0.Repositories;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IActivityLogRepository _logs;
    private readonly SurveyDbContext _db;
    private readonly IEmailService _email;

    public AuthService(IUserRepository users, IActivityLogRepository logs, SurveyDbContext db, IEmailService email)
    {
        _users = users;
        _logs = logs;
        _db = db;
        _email = email;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> RegisterAsync(RegisterViewModel model, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var errors = new List<string>();

        // R24/R31: enforce strong password beyond annotations if needed (e.g., disallow email fragment)
        if (!string.IsNullOrWhiteSpace(model.Email) &&
            !string.IsNullOrWhiteSpace(model.Password) &&
            model.Password.Contains(model.Email.Split('@')[0], StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Password must not contain parts of the email.");
            return (false, errors);
        }

        if (await _users.EmailExistsAsync(model.Email, ct))
        {
            errors.Add("Email is already registered.");
            return (false, errors);
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = model.Email.Trim(),
            FullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim(),
            IsActive = true,
            FailedLoginCount = 0,
            LockedUntilUtc = null,
        };

        // Strong hash using ASP.NET Core Identity v3 format (PBKDF2 with salt and versioning)
        var hasher = new PasswordHasher<User>();
        var hashedString = hasher.HashPassword(user, model.Password);
        user.PasswordHash = Convert.FromBase64String(hashedString);

        // Atomic save of user + activity log
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _users.AddAsync(user, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = user.UserId,
                ActionType = "UserRegistered",
                ActionDetail = $"User registered. IP={ip ?? "N/A"}, UA={(userAgent ?? "N/A")}",
                // CreatedAtUtc uses DB default
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct);
            // Handle unique constraint race conditions
            if (ex.InnerException?.Message.Contains("UQ__Users__A9D10534", StringComparison.OrdinalIgnoreCase) == true ||
                ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
            {
                return (false, new[] { "Email is already registered." });
            }
            return (false, new[] { "Registration failed. Please try again later." });
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Registration failed. Please try again later." });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors, User? User)> LoginAsync(LoginViewModel model, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var genericError = new[] { "Invalid email or password." };
        var user = await _users.GetByEmailAsync(model.Email.Trim(), ct);
        if (user is null)
            return (false, genericError, null);

        // Check temporary lock
        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc.Value > DateTime.UtcNow)
        {
            var minutes = (int)Math.Ceiling((user.LockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes);
            return (false, new[] { $"Account is temporarily locked. Try again in {minutes} minute(s)." }, null);
        }

        var hasher = new PasswordHasher<User>();
        var hashedString = Convert.ToBase64String(user.PasswordHash);
        var verify = hasher.VerifyHashedPassword(user, hashedString, model.Password);

        if (verify == PasswordVerificationResult.Success || verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.FailedLoginCount = 0;
            user.LockedUntilUtc = null;
            user.UpdatedAtUtc = DateTime.UtcNow;

            await _logs.AddAsync(new ActivityLog
            {
                UserId = user.UserId,
                ActionType = "UserLoggedIn",
                ActionDetail = $"Login success. IP={ip ?? "N/A"}, UA={(userAgent ?? "N/A")}",
            }, ct);

            await _db.SaveChangesAsync(ct);
            return (true, Array.Empty<string>(), user);
        }

        // Failed login
        user.FailedLoginCount += 1;
        user.UpdatedAtUtc = DateTime.UtcNow;
        if (user.FailedLoginCount >= 5)
        {
            user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(15); // khóa tạm 15 phút
        }

        await _logs.AddAsync(new ActivityLog
        {
            UserId = user.UserId,
            ActionType = "UserLoginFailed",
            ActionDetail = $"Login failed. Count={user.FailedLoginCount}, IP={ip ?? "N/A"}, UA={(userAgent ?? "N/A")}",
        }, ct);

        await _db.SaveChangesAsync(ct);
        return (false, genericError, null);
    }

    public async Task LogLogoutAsync(Guid userId, string? ip, string? userAgent, CancellationToken ct = default)
    {
        await _logs.AddAsync(new ActivityLog
        {
            UserId = userId,
            ActionType = "UserLoggedOut",
            ActionDetail = $"Logout. IP={ip ?? "N/A"}, UA={(userAgent ?? "N/A")}",
        }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> RequestPasswordResetAsync(
        ForgotPasswordViewModel model,
        string? ip,
        string? userAgent,
        Func<string, string> buildResetLink,
        CancellationToken ct = default)
    {
        // Always return success to avoid user enumeration
        var email = model.Email.Trim();
        var user = await _users.GetByEmailAsync(email, ct);

        if (user is not null)
        {
            // Create one-time token, 15 minutes expiry
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace('+', '-').Replace('/', '_').TrimEnd('='); // URL-safe

            var expiry = DateTime.UtcNow.AddMinutes(15);

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await _users.SetResetTokenAsync(user, token, expiry, ct);

                await _logs.AddAsync(new ActivityLog
                {
                    UserId = user.UserId,
                    ActionType = "PasswordResetRequested",
                    ActionDetail = $"Reset requested. IP={ip ?? "N/A"}, UA={(userAgent ?? "N/A")}, Expiry={expiry:o}"
                }, ct);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                // Don't leak failure to user; still respond success.
            }

            // Send email out of transaction
            var link = buildResetLink(token);
            var subject = "Reset your password";
            var body = $@"
                <p>Hello{(string.IsNullOrWhiteSpace(user.FullName) ? "" : $" {System.Web.HttpUtility.HtmlEncode(user.FullName)}")},</p>
                <p>We received a request to reset your password. Click the button below to set a new password.</p>
                <p><a href=""{link}"" style=""background:#0d6efd;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none;display:inline-block"">Reset Password</a></p>
                <p>Or copy this URL into your browser:<br/><code>{link}</code></p>
                <p>This link will expire in 15 minutes and can be used only once.</p>
                <p>If you did not request this, please ignore this email.</p>";
            try { await _email.SendAsync(user.Email, subject, body, ct); } catch { /* swallow to avoid info leak */ }
        }

        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ResetPasswordAsync(
        ResetPasswordViewModel model,
        string? ip,
        string? userAgent,
        CancellationToken ct = default)
    {
        var token = model.Token.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ResetToken == token, ct);
        if (user is null || !user.ResetTokenExpiry.HasValue || user.ResetTokenExpiry.Value < DateTime.UtcNow)
        {
            return (false, new[] { "Invalid or expired reset link." });
        }

        // Optional additional rule: disallow email fragment in password
        if (!string.IsNullOrWhiteSpace(user.Email) &&
            model.Password.Contains(user.Email.Split('@')[0], StringComparison.OrdinalIgnoreCase))
        {
            return (false, new[] { "Password must not contain parts of the email." });
        }

        var hasher = new PasswordHasher<User>();
        var hashedString = hasher.HashPassword(user, model.Password);
        var newHash = Convert.FromBase64String(hashedString);

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _users.UpdatePasswordHashAsync(user, newHash, ct);
            await _users.ClearResetTokenAsync(user, ct);
            user.FailedLoginCount = 0;
            user.LockedUntilUtc = null;

            await _logs.AddAsync(new ActivityLog
            {
                UserId = user.UserId,
                ActionType = "PasswordResetCompleted",
                ActionDetail = $"Password reset successful. IP={ip ?? "N/A"}, UA={(userAgent ?? "N/A")}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            await _logs.AddAsync(new ActivityLog
            {
                UserId = user.UserId,
                ActionType = "PasswordResetFailed",
                ActionDetail = $"Password reset failed. IP={ip ?? "N/A"}, UA={(userAgent ?? "N/A")}"
            }, ct);
            await _db.SaveChangesAsync(ct);

            return (false, new[] { "Password reset failed. Please try again later." });
        }
    }
}