using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class SurveySettingsViewModel
{
    [Required]
    public Guid SurveyId { get; set; }

    [Required, StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public bool IsAnonymous { get; set; }

    [StringLength(20)]
    public string? DefaultLanguage { get; set; }
}