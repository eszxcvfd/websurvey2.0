using Microsoft.EntityFrameworkCore;
using websurvey2._0.Models;
using websurvey2._0.Repositories;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _surveys;
    private readonly IActivityLogRepository _logs;
    private readonly SurveyDbContext _db;

    public SurveyService(ISurveyRepository surveys, IActivityLogRepository logs, SurveyDbContext db)
    {
        _surveys = surveys;
        _logs = logs;
        _db = db;
    }

    // Helper: normalize any DateTime? to UTC
    private static DateTime? NormalizeToUtc(DateTime? dt)
    {
        if (!dt.HasValue) return null;
        var v = dt.Value;
        return v.Kind switch
        {
            DateTimeKind.Utc => v,
            DateTimeKind.Local => v.ToUniversalTime(),
            _ => DateTime.SpecifyKind(v, DateTimeKind.Utc) // FIX: Unspecified đã là UTC từ client-side script
        };
    }

    public async Task<(bool Success, IEnumerable<string> Errors, Survey? Survey)> CreateDraftSurvey(
        Guid ownerId,
        string title,
        string? lang,
        CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title))
        {
            return (false, new[] { "Title is required." }, null);
        }

        var survey = new Survey
        {
            SurveyId = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = title.Trim(),
            DefaultLanguage = string.IsNullOrWhiteSpace(lang) ? null : lang.Trim(),
            Status = "Draft",
            IsAnonymous = false
        };

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _surveys.AddAsync(survey, ct);

            await _logs.AddAsync(new ActivityLog
            {
                UserId = ownerId,
                SurveyId = survey.SurveyId,
                ActionType = "SurveyCreated",
                ActionDetail = $"Survey created. Title='{survey.Title}', Lang={(survey.DefaultLanguage ?? "N/A")}, Status=Draft"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>(), survey);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Create survey failed. Please try again later." }, null);
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateSurveySettings(
        Guid surveyId, Guid actingUserId, SurveySettingsViewModel vm, CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdTrackedAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        // Update fields
        survey.Title = vm.Title.Trim();
        survey.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
        survey.IsAnonymous = vm.IsAnonymous;
        survey.DefaultLanguage = string.IsNullOrWhiteSpace(vm.DefaultLanguage) ? null : vm.DefaultLanguage.Trim();
        survey.UpdatedAtUtc = DateTime.UtcNow;

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = survey.SurveyId,
                ActionType = "SurveySettingsUpdated",
                ActionDetail = $"Settings updated. Title='{survey.Title}', Lang={(survey.DefaultLanguage ?? "N/A")}, Anonymous={survey.IsAnonymous}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Update settings failed. Please try again later." });
        }
    }

    // NEW: Set open/close time
    public async Task<(bool Success, IEnumerable<string> Errors)> SetOpenCloseTimeAsync(
        Guid surveyId,
        Guid actingUserId,
        DateTime? openAtUtc,
        DateTime? closeAtUtc,
        CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdTrackedAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        // Normalize incoming values to UTC first
        var openUtc = NormalizeToUtc(openAtUtc);
        var closeUtc = NormalizeToUtc(closeAtUtc);

        // Validation: Close date must be after open date
        if (openUtc.HasValue && closeUtc.HasValue && closeUtc.Value <= openUtc.Value)
        {
            return (false, new[] { "Close date must be after open date." });
        }

        // Validation: Cannot set dates in the past (with tolerance)
        var now = DateTime.UtcNow;
        if (openUtc.HasValue && openUtc.Value < now.AddMinutes(-5))
        {
            return (false, new[] { "Open date cannot be in the past." });
        }

        if (closeUtc.HasValue && closeUtc.Value < now.AddMinutes(-5))
        {
            return (false, new[] { "Close date cannot be in the past." });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            survey.OpenAtUtc = openUtc;
            survey.CloseAtUtc = closeUtc;
            survey.UpdatedAtUtc = DateTime.UtcNow;

            var detail = $"Schedule updated. Open={(openUtc?.ToString("o") ?? "N/A")}, Close={(closeUtc?.ToString("o") ?? "N/A")}";
            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = survey.SurveyId,
                ActionType = "SurveyScheduleUpdated",
                ActionDetail = detail
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Update schedule failed. Please try again later." });
        }
    }

    // NEW: Update full schedule including quota
    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateScheduleAsync(
        Guid surveyId,
        Guid actingUserId,
        SurveyScheduleViewModel vm,
        CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdTrackedAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        // DEBUG: Log incoming values
        await _logs.AddAsync(new ActivityLog
        {
            UserId = actingUserId,
            SurveyId = surveyId,
            ActionType = "DEBUG_ScheduleInput",
            ActionDetail = $"Incoming: OpenAtUtc={vm.OpenAtUtc?.ToString("o") ?? "NULL"} (Kind={vm.OpenAtUtc?.Kind}), " +
                          $"CloseAtUtc={vm.CloseAtUtc?.ToString("o") ?? "NULL"} (Kind={vm.CloseAtUtc?.Kind}), " +
                          $"CurrentUTC={DateTime.UtcNow:o}"
        }, ct);

        // Normalize vm times to UTC before validation
        var openUtc = NormalizeToUtc(vm.OpenAtUtc);
        var closeUtc = NormalizeToUtc(vm.CloseAtUtc);

        // DEBUG: Log normalized values
        await _logs.AddAsync(new ActivityLog
        {
            UserId = actingUserId,
            SurveyId = surveyId,
            ActionType = "DEBUG_ScheduleNormalized",
            ActionDetail = $"Normalized: OpenAtUtc={openUtc?.ToString("o") ?? "NULL"}, " +
                          $"CloseAtUtc={closeUtc?.ToString("o") ?? "NULL"}"
        }, ct);

        // Validation: Close date must be after open date
        if (openUtc.HasValue && closeUtc.HasValue && closeUtc.Value <= openUtc.Value)
        {
            return (false, new[] { "Close date must be after open date." });
        }

        // Validation: Dates cannot be in the past (REMOVED validation for more flexibility)
        var now = DateTime.UtcNow;
        // Comment out these validations temporarily to allow any date
        /*
        if (openUtc.HasValue && openUtc.Value < now.AddMinutes(-5))
        {
            return (false, new[] { "Open date cannot be in the past." });
        }

        if (closeUtc.HasValue && closeUtc.Value < now.AddMinutes(-5))
        {
            return (false, new[] { "Close date cannot be in the past." });
        }
        */

        // Validation: Response quota must be positive if provided
        if (vm.ResponseQuota.HasValue && vm.ResponseQuota.Value < 1)
        {
            return (false, new[] { "Response quota must be at least 1." });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            survey.OpenAtUtc = openUtc;
            survey.CloseAtUtc = closeUtc;
            survey.ResponseQuota = vm.ResponseQuota;
            survey.QuotaBehavior = string.IsNullOrWhiteSpace(vm.QuotaBehavior) ? null : vm.QuotaBehavior.Trim();
            survey.UpdatedAtUtc = DateTime.UtcNow;

            var detail = $"Schedule updated. Open={(openUtc?.ToString("o") ?? "N/A")}, " +
                        $"Close={(closeUtc?.ToString("o") ?? "N/A")}, " +
                        $"Quota={(vm.ResponseQuota?.ToString() ?? "N/A")}, " +
                        $"Behavior={(vm.QuotaBehavior ?? "N/A")}";

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = survey.SurveyId,
                ActionType = "SurveyScheduleUpdated",
                ActionDetail = detail
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Update schedule failed. Please try again later." });
        }
    }

    // NEW: Publish survey
    public async Task<(bool Success, IEnumerable<string> Errors)> PublishSurveyAsync(
        Guid surveyId,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdTrackedAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        // Validation: Cannot publish if already published or closed
        if (survey.Status == "Published")
        {
            return (false, new[] { "Survey is already published." });
        }

        if (survey.Status == "Closed")
        {
            return (false, new[] { "Cannot publish a closed survey." });
        }

        // Validation: Must have at least one question (optional - depends on your requirements)
        var hasQuestions = await _db.Questions.AnyAsync(q => q.SurveyId == surveyId, ct);
        if (!hasQuestions)
        {
            return (false, new[] { "Cannot publish survey without questions." });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            survey.Status = "Published";
            survey.UpdatedAtUtc = DateTime.UtcNow;
            
            // If no open date is set, set it to slightly in the past to avoid race condition
            if (!survey.OpenAtUtc.HasValue)
            {
                survey.OpenAtUtc = DateTime.UtcNow.AddSeconds(-30); // Set to 30 seconds ago
            }

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = survey.SurveyId,
                ActionType = "SurveyPublished",
                ActionDetail = $"Survey published. Title='{survey.Title}', OpenAt={(survey.OpenAtUtc?.ToString("o") ?? "N/A")}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Publish survey failed. Please try again later." });
        }
    }

    // NEW: Close survey
    public async Task<(bool Success, IEnumerable<string> Errors)> CloseSurveyAsync(
        Guid surveyId,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdTrackedAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        // Validation: Cannot close if not published
        if (survey.Status != "Published")
        {
            return (false, new[] { "Only published surveys can be closed." });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            survey.Status = "Closed";
            survey.UpdatedAtUtc = DateTime.UtcNow;
            
            // Set close date to now if not already set
            if (!survey.CloseAtUtc.HasValue)
            {
                survey.CloseAtUtc = DateTime.UtcNow;
            }

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = survey.SurveyId,
                ActionType = "SurveyClosed",
                ActionDetail = $"Survey closed. Title='{survey.Title}', ClosedAt={survey.CloseAtUtc?.ToString("o")}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Close survey failed. Please try again later." });
        }
    }

    // NEW: Reopen survey
    public async Task<(bool Success, IEnumerable<string> Errors)> ReopenSurveyAsync(
        Guid surveyId,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        var survey = await _surveys.GetByIdTrackedAsync(surveyId, ct);
        if (survey is null) return (false, new[] { "Survey not found." });

        // Validation: Can only reopen closed surveys
        if (survey.Status != "Closed")
        {
            return (false, new[] { "Only closed surveys can be reopened." });
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            survey.Status = "Published";
            survey.UpdatedAtUtc = DateTime.UtcNow;
            
            // Clear the close date when reopening
            survey.CloseAtUtc = null;

            await _logs.AddAsync(new ActivityLog
            {
                UserId = actingUserId,
                SurveyId = survey.SurveyId,
                ActionType = "SurveyReopened",
                ActionDetail = $"Survey reopened. Title='{survey.Title}', ReopenedAt={DateTime.UtcNow:o}"
            }, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, Array.Empty<string>());
        }
        catch
        {
            await tx.RollbackAsync(ct);
            return (false, new[] { "Reopen survey failed. Please try again later." });
        }
    }
}