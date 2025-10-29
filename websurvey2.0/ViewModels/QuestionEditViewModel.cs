using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class QuestionEditViewModel
{
    public Guid? QuestionId { get; set; }

    [Required]
    public Guid SurveyId { get; set; }

    [Required, StringLength(1000)]
    public string QuestionText { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string QuestionType { get; set; } = "ShortText";

    public bool IsRequired { get; set; }

    [StringLength(500)]
    public string? HelpText { get; set; }

    [StringLength(500)]
    public string? DefaultValue { get; set; }

    public int QuestionOrder { get; set; }

    // Common fields
    [StringLength(200)]
    public string? Placeholder { get; set; }

    [StringLength(500)]
    public string? RegexPattern { get; set; }

    // Number
    public double? NumberMin { get; set; }
    public double? NumberMax { get; set; }
    public double? NumberStep { get; set; }

    // Rating
    public int? RatingMax { get; set; }

    // Slider
    public double? SliderMin { get; set; }
    public double? SliderMax { get; set; }
    public double? SliderStep { get; set; }

    // NPS
    public string? NpsLowLabel { get; set; }
    public string? NpsHighLabel { get; set; }

    // Choice options
    public bool AllowOther { get; set; }
    public bool RandomizeOptions { get; set; }

    // Likert & Matrix
    public string? LikertScaleCsv { get; set; }
    public string? MatrixColumnsCsv { get; set; }

    public List<QuestionOptionViewModel> Options { get; set; } = new();

    // NEW: Branching logic
    public List<BranchLogicViewModel> BranchLogics { get; set; } = new();

    // NEW: Available questions for branching target
    public List<QuestionSummaryViewModel> AvailableQuestions { get; set; } = new();
}

// Helper class for question dropdown
public class QuestionSummaryViewModel
{
    public Guid QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
}