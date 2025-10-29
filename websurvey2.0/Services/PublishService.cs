using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using websurvey2._0.Models;
using websurvey2._0.Repositories;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public class PublishService : IPublishService
{
    private readonly ISurveyChannelRepository _channels;
    private readonly ISurveyRepository _surveys;
    private readonly IActivityLogRepository _logs;
    private readonly IRoleService _roles;
    private readonly SurveyDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _email; // NEW: Email service

    public PublishService(
        ISurveyChannelRepository channels,
        ISurveyRepository surveys,
        IActivityLogRepository logs,
        IRoleService roles,
        SurveyDbContext db,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        IEmailService email) // NEW: Email service
    {
        _channels = channels;
        _surveys = surveys;
        _logs = logs;
        _roles = roles;
        _db = db;
        _env = env;
        _httpContextAccessor = httpContextAccessor;
        _email = email; // NEW: Email service
    }

    public async Task<(bool Success, IEnumerable<string> Errors, SurveyChannel? Channel)> CreatePublicLinkAsync(
        Guid actingUserId,
        PublishLinkViewModel vm,
        CancellationToken ct = default)
    {
        // Check permissions
        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, vm.SurveyId, "Publish", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." }, null);

        // Verify survey exists
        var survey = await _surveys.GetByIdAsync(vm.SurveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." }, null);

        // Generate unique slug if not provided or if exists
        string slug;
        if (string.IsNullOrWhiteSpace(vm.PublicUrlSlug))
        {
            slug = GenerateUniqueSlug();
        }
        else
        {
            slug = vm.PublicUrlSlug.Trim().ToLowerInvariant();
            
            // Check if slug already exists
            if (await _channels.SlugExistsAsync(slug, ct))
            {
                return (false, new[] { $"URL slug '{slug}' is already taken. Please choose another one." }, null);
            }
        }

        // Build full URL
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request != null
            ? $"{request.Scheme}://{request.Host}"
            : "https://localhost";
        var fullUrl = $"{baseUrl}/s/{slug}";

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var channel = new SurveyChannel
            {
                ChannelId = Guid.NewGuid(),
                SurveyId = vm.SurveyId,
                ChannelType = vm.ChannelType,
                PublicUrlSlug = slug,
                FullUrl = fullUrl,
                IsActive = vm.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };

            // Generate QR code if requested
            if (vm.GenerateQrCode)
            {
                var qrPath = await GenerateQrCodeAsync(fullUrl, channel.ChannelId, ct);
                channel.QrImagePath = qrPath;
            }

            await _channels.AddAsync(channel, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = vm.SurveyId,
                ActionType = "SurveyChannelCreated",
                ActionDetail = $"Public link created. Slug='{slug}', URL='{fullUrl}', QR={(vm.GenerateQrCode ? "Yes" : "No")}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return (true, Array.Empty<string>(), channel);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { $"Failed to create link: {ex.Message}" }, null);
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateChannelStatusAsync(
        Guid actingUserId,
        Guid channelId,
        bool isActive,
        CancellationToken ct = default)
    {
        // Fetch only what we need for permission/logging
        var meta = await _db.Set<SurveyChannel>()
            .AsNoTracking()
            .Where(c => c.ChannelId == channelId)
            .Select(c => new { c.ChannelId, c.SurveyId })
            .FirstOrDefaultAsync(ct);

        if (meta is null) return (false, new[] { "Channel not found." });

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, meta.SurveyId, "Publish", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Robust partial update using a stub entity (avoids tracking issues)
            var stub = new SurveyChannel { ChannelId = meta.ChannelId, IsActive = isActive };
            _db.Attach(stub);
            _db.Entry(stub).Property(x => x.IsActive).IsModified = true;

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = meta.SurveyId,
                ActionType = "SurveyChannelStatusUpdated",
                ActionDetail = $"Channel {channelId} status changed to {(isActive ? "Active" : "Inactive")}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { $"Failed to update channel status: {ex.Message}" });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> DeleteChannelAsync(
        Guid actingUserId,
        Guid channelId,
        CancellationToken ct = default)
    {
        var channel = await _db.Set<SurveyChannel>().FirstOrDefaultAsync(c => c.ChannelId == channelId, ct);
        if (channel is null) return (false, new[] { "Channel not found." });

        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, channel.SurveyId, "Publish", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." });

        // Check if channel has responses
        var hasResponses = await _db.Set<SurveyResponse>()
            .AnyAsync(r => r.ChannelId == channelId, ct);

        if (hasResponses)
        {
            return (false, new[] { "Cannot delete channel with existing responses. Deactivate it instead." });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Delete QR code file if exists
            if (!string.IsNullOrWhiteSpace(channel.QrImagePath))
            {
                DeleteQrCodeFile(channel.QrImagePath);
            }

            await _channels.RemoveAsync(channel, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = channel.SurveyId,
                ActionType = "SurveyChannelDeleted",
                ActionDetail = $"Channel {channelId} (slug: {channel.PublicUrlSlug}) deleted"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Failed to delete channel." });
        }
    }

    public async Task<PublishLinksListViewModel?> GetChannelsBySurveyAsync(
        Guid actingUserId,
        Guid surveyId,
        CancellationToken ct = default)
    {
        var (allowed, _, _) = await _roles.CheckPermissionAsync(actingUserId, surveyId, "Publish", ct);
        if (!allowed) return null;

        var survey = await _surveys.GetByIdAsync(surveyId, ct);
        if (survey is null) return null;

        var channels = await _channels.GetBySurveyAsync(surveyId, ct);

        // Get response counts per channel
        var responseCounts = await _db.Set<SurveyResponse>()
            .Where(r => r.SurveyId == surveyId && r.ChannelId != null)
            .GroupBy(r => r.ChannelId!.Value)
            .Select(g => new { ChannelId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ChannelId, x => x.Count, ct);

        return new PublishLinksListViewModel
        {
            SurveyId = surveyId,
            SurveyTitle = survey.Title,
            Channels = channels.Select(c => new PublishLinkItemViewModel
            {
                ChannelId = c.ChannelId,
                ChannelType = c.ChannelType,
                PublicUrlSlug = c.PublicUrlSlug,
                FullUrl = c.FullUrl,
                QrImagePath = c.QrImagePath,
                IsActive = c.IsActive,
                CreatedAtUtc = c.CreatedAtUtc,
                ResponseCount = responseCounts.TryGetValue(c.ChannelId, out var count) ? count : 0
            }).ToList()
        };
    }

    public string GenerateUniqueSlug(string? baseSlug = null)
    {
        if (!string.IsNullOrWhiteSpace(baseSlug))
        {
            return baseSlug.Trim().ToLowerInvariant();
        }

        // Generate random 8-character alphanumeric slug
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = RandomNumberGenerator.GetBytes(8);
        var result = new StringBuilder(8);

        foreach (var b in random)
        {
            result.Append(chars[b % chars.Length]);
        }

        return result.ToString();
    }

    private async Task<string> GenerateQrCodeAsync(string url, Guid channelId, CancellationToken ct)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            // Save to wwwroot/qrcodes
            var qrFolder = Path.Combine(_env.WebRootPath, "qrcodes");
            if (!Directory.Exists(qrFolder))
            {
                Directory.CreateDirectory(qrFolder);
            }

            var fileName = $"{channelId}.png";
            var filePath = Path.Combine(qrFolder, fileName);

            await File.WriteAllBytesAsync(filePath, qrCodeImage, ct);

            return $"/qrcodes/{fileName}";
        }
        catch
        {
            return string.Empty;
        }
    }

    private void DeleteQrCodeFile(string qrImagePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(qrImagePath)) return;

            var filePath = Path.Combine(_env.WebRootPath, qrImagePath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    // NEW: Send email campaign
    public async Task<(bool Success, IEnumerable<string> Errors, EmailCampaignResultViewModel? Result)> SendEmailCampaignAsync(
        Guid actingUserId,
        SendEmailViewModel vm,
        CancellationToken ct = default)
    {
        // Check permissions
        var (allowed, error, _) = await _roles.CheckPermissionAsync(actingUserId, vm.SurveyId, "Publish", ct);
        if (!allowed) return (false, new[] { error ?? "Access denied." }, null);

        // Verify survey exists and is published
        var survey = await _surveys.GetByIdAsync(vm.SurveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." }, null);

        if (survey.Status != "Published")
        {
            return (false, new[] { "Survey must be published before sending emails." }, null);
        }

        // Parse recipient emails
        var emails = vm.RecipientEmails
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct()
            .ToList();

        if (emails.Count == 0)
        {
            return (false, new[] { "No valid recipient emails provided." }, null);
        }

        // Validate email format
        var invalidEmails = emails.Where(e => !IsValidEmail(e)).ToList();
        if (invalidEmails.Any())
        {
            return (false, new[] { $"Invalid email format: {string.Join(", ", invalidEmails)}" }, null);
        }

        // Get or create survey link
        string surveyUrl;
        if (vm.IncludeSurveyLink)
        {
            if (!string.IsNullOrWhiteSpace(vm.PublicUrlSlug))
            {
                // Use existing slug
                var existingChannel = await _db.Set<SurveyChannel>()
                    .FirstOrDefaultAsync(c => c.PublicUrlSlug == vm.PublicUrlSlug && c.SurveyId == vm.SurveyId, ct);
                
                if (existingChannel != null)
                {
                    surveyUrl = existingChannel.FullUrl ?? string.Empty;
                }
                else
                {
                    return (false, new[] { "Specified survey link not found." }, null);
                }
            }
            else
            {
                // Create new link for email campaign
                var slug = GenerateUniqueSlug();
                var request = _httpContextAccessor.HttpContext?.Request;
                var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "https://localhost";
                surveyUrl = $"{baseUrl}/s/{slug}";

                var newChannel = new SurveyChannel
                {
                    ChannelId = Guid.NewGuid(),
                    SurveyId = vm.SurveyId,
                    ChannelType = "Email",
                    PublicUrlSlug = slug,
                    FullUrl = surveyUrl,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _channels.AddAsync(newChannel, ct);
                await _db.SaveChangesAsync(ct);
                vm.PublicUrlSlug = slug;
            }
        }
        else
        {
            surveyUrl = string.Empty;
        }

        // Build email content
        var emailBodyWithLink = vm.EmailBody;
        if (vm.IncludeSurveyLink && !string.IsNullOrEmpty(surveyUrl))
        {
            emailBodyWithLink += $@"
                <div style=""margin-top: 30px; text-align: center;"">
                    <a href=""{surveyUrl}"" style=""background:#0d6efd;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block;font-weight:500"">
                        Take Survey
                    </a>
                </div>
                <p style=""margin-top: 20px; font-size: 12px; color: #666;"">
                    Or copy this link: <a href=""{surveyUrl}"">{surveyUrl}</a>
                </p>";
        }

        // Send emails
        var successCount = 0;
        var failureCount = 0;
        var failedEmails = new List<string>();

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Create email channel record
            var emailChannel = new SurveyChannel
            {
                ChannelId = Guid.NewGuid(),
                SurveyId = vm.SurveyId,
                ChannelType = "Email",
                PublicUrlSlug = vm.PublicUrlSlug,
                FullUrl = surveyUrl,
                EmailSubject = vm.EmailSubject,
                EmailBody = vm.EmailBody,
                SentAtUtc = DateTime.UtcNow,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _channels.AddAsync(emailChannel, ct);

            // Send to each recipient
            foreach (var email in emails)
            {
                try
                {
                    await _email.SendAsync(email, vm.EmailSubject, emailBodyWithLink, ct);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    failedEmails.Add(email);
                    // Log the error for debugging
                    // Note: You may want to inject ILogger<PublishService> to log this
                    Console.WriteLine($"Failed to send email to {email}: {ex.Message}");
                }
            }

            // Log activity
            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = vm.SurveyId,
                ActionType = "EmailCampaignSent",
                ActionDetail = $"Email campaign sent. Subject='{vm.EmailSubject}', " +
                              $"Total={emails.Count}, Success={successCount}, Failed={failureCount}, " +
                              $"Link={(vm.IncludeSurveyLink ? surveyUrl : "N/A")}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var result = new EmailCampaignResultViewModel
            {
                TotalEmails = emails.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                FailedEmails = failedEmails,
                ChannelId = emailChannel.ChannelId
            };

            return (true, Array.Empty<string>(), result);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { $"Failed to send email campaign: {ex.Message}" }, null);
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}