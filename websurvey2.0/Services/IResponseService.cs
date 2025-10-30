using websurvey2._0.Models;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public interface IResponseService
{
    Task<(bool Success, string? Error, RespondViewModel? Model)> GetSurveyForResponseAsync(
        Guid surveyId, Guid? channelId, string? ipAddress, CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors, Guid? ResponseId)> SubmitResponseAsync(
        SubmitResponseViewModel vm, string? ipAddress, CancellationToken ct = default);

    Task<bool> ValidateAntiSpamTokenAsync(string token, CancellationToken ct = default);
}