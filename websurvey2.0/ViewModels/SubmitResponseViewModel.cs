using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class SubmitResponseViewModel
{
    [Required]
    public Guid SurveyId { get; set; }

    public Guid? ChannelId { get; set; }

    [EmailAddress]
    public string? RespondentEmail { get; set; }

    [Required]
    public Dictionary<Guid, string> Answers { get; set; } = new();

    public string? AntiSpamToken { get; set; }
}