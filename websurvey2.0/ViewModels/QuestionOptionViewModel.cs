using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class QuestionOptionViewModel
{
    public Guid? OptionId { get; set; } // null => new option

    [Required, StringLength(500)]
    public string OptionText { get; set; } = string.Empty;

    [StringLength(200)]
    public string? OptionValue { get; set; }

    public int OptionOrder { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 1;
}