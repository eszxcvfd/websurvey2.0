using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class UpdateResponseViewModel
{
    [Required]
    public Guid ResponseId { get; set; }

    [Required]
    public Guid SurveyId { get; set; }

    // Key: QuestionId, Value: AnswerText
    [Required]
    public Dictionary<Guid, string> Answers { get; set; } = new();
}