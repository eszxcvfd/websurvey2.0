using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

[Table("BranchLogic")]
public partial class BranchLogic
{
    [Key]
    public Guid LogicId { get; set; }

    public Guid SurveyId { get; set; }

    public Guid SourceQuestionId { get; set; }

    [StringLength(1000)]
    public string ConditionExpr { get; set; } = null!;

    [StringLength(50)]
    public string TargetAction { get; set; } = null!;

    public Guid? TargetQuestionId { get; set; }

    public int PriorityOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    [ForeignKey("SourceQuestionId")]
    [InverseProperty("BranchLogicSourceQuestions")]
    public virtual Question SourceQuestion { get; set; } = null!;

    [ForeignKey("SurveyId")]
    [InverseProperty("BranchLogics")]
    public virtual Survey Survey { get; set; } = null!;

    [ForeignKey("TargetQuestionId")]
    [InverseProperty("BranchLogicTargetQuestions")]
    public virtual Question? TargetQuestion { get; set; }
}
