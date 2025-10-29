using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

public partial class QuestionOption
{
    [Key]
    public Guid OptionId { get; set; }

    public Guid QuestionId { get; set; }

    public int OptionOrder { get; set; }

    [StringLength(500)]
    public string OptionText { get; set; } = null!;

    [StringLength(200)]
    public string? OptionValue { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey("QuestionId")]
    [InverseProperty("QuestionOptions")]
    public virtual Question Question { get; set; } = null!;
}
