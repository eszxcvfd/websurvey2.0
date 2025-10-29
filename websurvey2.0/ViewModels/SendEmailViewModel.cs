using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class SendEmailViewModel
{
    public Guid SurveyId { get; set; }
    public string? SurveyTitle { get; set; }

    [Required(ErrorMessage = "Email subject is required")]
    [StringLength(255, ErrorMessage = "Subject cannot exceed 255 characters")]
    public string EmailSubject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email body is required")]
    public string EmailBody { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one recipient email is required")]
    public string RecipientEmails { get; set; } = string.Empty; // Comma-separated emails

    public bool IncludeSurveyLink { get; set; } = true;
    public string? PublicUrlSlug { get; set; }
}

public class EmailCampaignResultViewModel
{
    public int TotalEmails { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedEmails { get; set; } = new();
    public Guid? ChannelId { get; set; }
}