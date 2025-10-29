using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class QuestionEditViewModel
{
    [Required]
    public Guid SurveyId { get; set; }

    public Guid? QuestionId { get; set; } // null when creating

    // Supported types (SurveyMonkey-like)
    // ShortText, LongText, Email, Phone, Url, Number, Date, Time, DateTime,
    // YesNo, Rating, NPS, Slider, Ranking, MultipleChoice, Checkboxes,
    // Dropdown, MultiSelectDropdown, Likert, Matrix, Section, PageBreak
    [Required, StringLength(50)]
    public string QuestionType { get; set; } = "ShortText";

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    // Generic UI sugar
    [StringLength(500)]
    public string? HelpText { get; set; }

    // Placeholder for text-like input
    [StringLength(200)]
    public string? Placeholder { get; set; }

    // Regex if needed (for text-like)
    [StringLength(500)]
    public string? RegexPattern { get; set; }

    // Numeric/Slider controls
    public double? NumberMin { get; set; }
    public double? NumberMax { get; set; }
    public double? NumberStep { get; set; }

    // Rating (stars)
    public int? RatingMax { get; set; } = 5;

    // Slider specifics
    public double? SliderMin { get; set; }
    public double? SliderMax { get; set; }
    public double? SliderStep { get; set; }

    // NPS labels (0-10 always)
    [StringLength(100)]
    public string? NpsLowLabel { get; set; }
    [StringLength(100)]
    public string? NpsHighLabel { get; set; }

    // Choice controls
    public bool AllowOther { get; set; }
    public bool RandomizeOptions { get; set; }

    // Likert scale labels (comma-separated), statements in Options
    [StringLength(500)]
    public string? LikertScaleCsv { get; set; }

    // Matrix columns (comma-separated), rows in Options
    [StringLength(500)]
    public string? MatrixColumnsCsv { get; set; }

    // DefaultValue kept for legacy/simple defaults
    [StringLength(500)]
    public string? DefaultValue { get; set; }

    public int QuestionOrder { get; set; } = 1;

    // Options for option-based questions:
    // MultipleChoice, Checkboxes, Dropdown, MultiSelectDropdown, Ranking,
    // Likert (statements), Matrix (rows)
    public List<QuestionOptionViewModel> Options { get; set; } = new();

    public bool SupportsOptions =>
        string.Equals(QuestionType, "MultipleChoice", StringComparison.OrdinalIgnoreCase)
        || string.Equals(QuestionType, "Checkboxes", StringComparison.OrdinalIgnoreCase)
        || string.Equals(QuestionType, "Dropdown", StringComparison.OrdinalIgnoreCase)
        || string.Equals(QuestionType, "MultiSelectDropdown", StringComparison.OrdinalIgnoreCase)
        || string.Equals(QuestionType, "Ranking", StringComparison.OrdinalIgnoreCase)
        || string.Equals(QuestionType, "Likert", StringComparison.OrdinalIgnoreCase)
        || string.Equals(QuestionType, "Matrix", StringComparison.OrdinalIgnoreCase);
}