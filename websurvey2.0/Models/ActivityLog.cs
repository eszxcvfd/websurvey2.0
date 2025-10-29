using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

[Table("ActivityLog")]
public partial class ActivityLog
{
    [Key]
    public long LogId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? SurveyId { get; set; }

    public Guid? ResponseId { get; set; }

    [StringLength(100)]
    public string ActionType { get; set; } = null!;

    public string? ActionDetail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    [ForeignKey("ResponseId")]
    [InverseProperty("ActivityLogs")]
    public virtual SurveyResponse? Response { get; set; }

    [ForeignKey("SurveyId")]
    [InverseProperty("ActivityLogs")]
    public virtual Survey? Survey { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ActivityLogs")]
    public virtual User? User { get; set; }
}
