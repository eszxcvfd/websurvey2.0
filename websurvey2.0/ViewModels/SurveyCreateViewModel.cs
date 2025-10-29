using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class SurveyCreateViewModel
{
    [Required, StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Lang { get; set; } // ví dụ: "vi", "en"
}