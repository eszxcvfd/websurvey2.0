using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

[PrimaryKey("ResponseId", "QuestionId")]
public partial class ResponseAnswer
{
    [Key]
    public Guid ResponseId { get; set; }

    [Key]
    public Guid QuestionId { get; set; }

    public string? AnswerText { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal? NumericValue { get; set; }

    public DateTime? DateValue { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    [ForeignKey("QuestionId")]
    [InverseProperty("ResponseAnswers")]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey("ResponseId")]
    [InverseProperty("ResponseAnswers")]
    public virtual SurveyResponse Response { get; set; } = null!;
}
