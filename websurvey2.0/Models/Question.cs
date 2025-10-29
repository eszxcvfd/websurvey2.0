using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

public partial class Question
{
    [Key]
    public Guid QuestionId { get; set; }

    public Guid SurveyId { get; set; }

    public int QuestionOrder { get; set; }

    public string QuestionText { get; set; } = null!;

    [StringLength(50)]
    public string QuestionType { get; set; } = null!;

    public bool IsRequired { get; set; }

    [StringLength(500)]
    public string? ValidationRule { get; set; }

    [StringLength(500)]
    public string? HelpText { get; set; }

    [StringLength(500)]
    public string? DefaultValue { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    [InverseProperty("SourceQuestion")]
    public virtual ICollection<BranchLogic> BranchLogicSourceQuestions { get; set; } = new List<BranchLogic>();

    [InverseProperty("TargetQuestion")]
    public virtual ICollection<BranchLogic> BranchLogicTargetQuestions { get; set; } = new List<BranchLogic>();

    [InverseProperty("Question")]
    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();

    [InverseProperty("Question")]
    public virtual ICollection<ResponseAnswer> ResponseAnswers { get; set; } = new List<ResponseAnswer>();

    [ForeignKey("SurveyId")]
    [InverseProperty("Questions")]
    public virtual Survey Survey { get; set; } = null!;
}
