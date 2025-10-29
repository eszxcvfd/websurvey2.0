using websurvey2._0.Models;
using websurvey2._0.ViewModels;

namespace websurvey2._0.Services;

public interface IPublishService
{
    Task<(bool Success, IEnumerable<string> Errors, SurveyChannel? Channel)> CreatePublicLinkAsync(
        Guid actingUserId,
        PublishLinkViewModel vm,
        CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors)> UpdateChannelStatusAsync(
        Guid actingUserId,
        Guid channelId,
        bool isActive,
        CancellationToken ct = default);

    Task<(bool Success, IEnumerable<string> Errors)> DeleteChannelAsync(
        Guid actingUserId,
        Guid channelId,
        CancellationToken ct = default);

    Task<PublishLinksListViewModel?> GetChannelsBySurveyAsync(
        Guid actingUserId,
        Guid surveyId,
        CancellationToken ct = default);

    string GenerateUniqueSlug(string? baseSlug = null);

    // NEW: Email campaign
    Task<(bool Success, IEnumerable<string> Errors, EmailCampaignResultViewModel? Result)> SendEmailCampaignAsync(
        Guid actingUserId,
        SendEmailViewModel vm,
        CancellationToken ct = default);
}