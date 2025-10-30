using System.ComponentModel.DataAnnotations;
using websurvey2._0.Models;

namespace websurvey2._0.ViewModels;

public class RespondViewModel
{
    public Guid SurveyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsAnonymous { get; set; }
    public Guid? ChannelId { get; set; }
    public List<QuestionViewModel> Questions { get; set; } = new();

    // NEW: Branching rules for runtime
    public List<BranchLogicRuleViewModel> BranchLogics { get; set; } = new();
}

public class QuestionViewModel
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int QuestionOrder { get; set; }
    public List<QuestionOptionViewModel> Options { get; set; } = new();
}

public class BranchLogicRuleViewModel
{
    public Guid SourceQuestionId { get; set; }
    public string ConditionOperator { get; set; } = "equals";
    public string? ConditionValue { get; set; }
    public Guid? ConditionOptionId { get; set; }
    public string TargetAction { get; set; } = "SkipTo";
    public Guid? TargetQuestionId { get; set; }
    public int PriorityOrder { get; set; } = 1;
}