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
}