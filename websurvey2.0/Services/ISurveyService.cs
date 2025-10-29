using websurvey2._0.Models;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public interface ISurveyService
{
    Task<(bool Success, IEnumerable<string> Errors, Survey? Survey)> CreateDraftSurvey(
        Guid ownerId, string title, string? lang, CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors)> UpdateSurveySettings(
        Guid surveyId, Guid actingUserId, SurveySettingsViewModel vm, CancellationToken ct = default);
}