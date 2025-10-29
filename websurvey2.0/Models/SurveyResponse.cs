using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

public partial class SurveyResponse
{
    [Key]
    public Guid ResponseId { get; set; }

    public Guid SurveyId { get; set; }

    public Guid? ChannelId { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public DateTime LastUpdatedAtUtc { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    [StringLength(200)]
    public string? AnonToken { get; set; }

    [StringLength(255)]
    public string? RespondentEmail { get; set; }

    [Column("RespondentIP")]
    [StringLength(64)]
    public string? RespondentIp { get; set; }

    [StringLength(200)]
    public string? AntiSpamToken { get; set; }

    public bool IsLocked { get; set; }

    [InverseProperty("Response")]
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    [ForeignKey("ChannelId")]
    [InverseProperty("SurveyResponses")]
    public virtual SurveyChannel? Channel { get; set; }

    [InverseProperty("Response")]
    public virtual ICollection<ResponseAnswer> ResponseAnswers { get; set; } = new List<ResponseAnswer>();

    [ForeignKey("SurveyId")]
    [InverseProperty("SurveyResponses")]
    public virtual Survey Survey { get; set; } = null!;
}
