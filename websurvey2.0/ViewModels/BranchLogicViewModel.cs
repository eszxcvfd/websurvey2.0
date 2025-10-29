using System.ComponentModel.DataAnnotations;

namespace websurvey2._0.ViewModels;

public class BranchLogicViewModel
{
    public Guid? LogicId { get; set; }

    [Required]
    public Guid SurveyId { get; set; }

    [Required]
    public Guid SourceQuestionId { get; set; }

    /// <summary>
    /// Operator cho điều kiện: equals, notEquals, contains, greaterThan, lessThan, optionSelected, etc.
    /// </summary>
    [Required, StringLength(50)]
    public string ConditionOperator { get; set; } = "equals";

    /// <summary>
    /// Giá trị để so sánh (cho text-based questions)
    /// </summary>
    [StringLength(500)]
    public string? ConditionValue { get; set; }

    /// <summary>
    /// Option ID được chọn (cho choice-based questions)
    /// </summary>
    public Guid? ConditionOptionId { get; set; }

    /// <summary>
    /// JSON đầy đủ (được build tự động từ các fields trên)
    /// </summary>
    [Required, StringLength(1000)]
    public string ConditionExpr { get; set; } = string.Empty;

    /// <summary>
    /// Hành động khi điều kiện đúng:
    /// - "SkipTo": Nhảy đến câu hỏi khác
    /// - "EndSurvey": Kết thúc khảo sát
    /// - "ShowQuestion": Hiển thị câu hỏi ẩn
    /// </summary>
    [Required, StringLength(50)]
    public string TargetAction { get; set; } = "SkipTo";

    /// <summary>
    /// ID câu hỏi đích (nếu TargetAction = "SkipTo" hoặc "ShowQuestion")
    /// </summary>
    public Guid? TargetQuestionId { get; set; }

    /// <summary>
    /// Thứ tự ưu tiên (số nhỏ hơn được xử lý trước)
    /// </summary>
    public int PriorityOrder { get; set; } = 1;
}

public class BranchLogicListViewModel
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<BranchLogicItemViewModel> Logics { get; set; } = new();
}

public class BranchLogicItemViewModel
{
    public Guid LogicId { get; set; }
    public string ConditionDescription { get; set; } = string.Empty;
    public string TargetAction { get; set; } = string.Empty;
    public string? TargetQuestionText { get; set; }
    public int PriorityOrder { get; set; }
}