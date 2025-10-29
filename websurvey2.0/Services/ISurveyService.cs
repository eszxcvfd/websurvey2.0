using websurvey2._0.Models;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public interface ISurveyService
{
    Task<(bool Success, IEnumerable<string> Errors, Survey? Survey)> CreateDraftSurvey(
        Guid ownerId, string title, string? lang, CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors)> UpdateSurveySettings(
        Guid surveyId, Guid actingUserId, SurveySettingsViewModel vm, CancellationToken ct = default);

    // NEW: Schedule management
    Task<(bool Success, IEnumerable<string> Errors)> SetOpenCloseTimeAsync(
        Guid surveyId, Guid actingUserId, DateTime? openAtUtc, DateTime? closeAtUtc, CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors)> UpdateScheduleAsync(
        Guid surveyId, Guid actingUserId, SurveyScheduleViewModel vm, CancellationToken ct = default);

    // NEW: Publish survey
    Task<(bool Success, IEnumerable<string> Errors)> PublishSurveyAsync(
        Guid surveyId, Guid actingUserId, CancellationToken ct = default);

    // NEW: Close survey
    Task<(bool Success, IEnumerable<string> Errors)> CloseSurveyAsync(
        Guid surveyId, Guid actingUserId, CancellationToken ct = default);

    // NEW: Reopen survey
    Task<(bool Success, IEnumerable<string> Errors)> ReopenSurveyAsync(
        Guid surveyId, Guid actingUserId, CancellationToken ct = default);
}