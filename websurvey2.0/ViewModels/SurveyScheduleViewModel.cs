using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class SurveyScheduleViewModel
{
    [Required]
    public Guid SurveyId { get; set; }

    public string SurveyTitle { get; set; } = string.Empty;

    [Display(Name = "Open Date & Time (UTC)")]
    public DateTime? OpenAtUtc { get; set; }

    [Display(Name = "Close Date & Time (UTC)")]
    public DateTime? CloseAtUtc { get; set; }

    [Display(Name = "Response Quota")]
    [Range(1, int.MaxValue, ErrorMessage = "Response quota must be at least 1")]
    public int? ResponseQuota { get; set; }

    [Display(Name = "Quota Behavior")]
    [StringLength(50)]
    public string? QuotaBehavior { get; set; }

    // Helper properties for local time display
    public DateTime? OpenAtLocal => OpenAtUtc?.ToLocalTime();
    public DateTime? CloseAtLocal => CloseAtUtc?.ToLocalTime();
}