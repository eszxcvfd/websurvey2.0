using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

public partial class Survey
{
    [Key]
    public Guid SurveyId { get; set; }

    public Guid OwnerId { get; set; }

    [StringLength(255)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [StringLength(20)]
    public string? DefaultLanguage { get; set; }

    public bool IsAnonymous { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = null!;

    public DateTime? OpenAtUtc { get; set; }

    public DateTime? CloseAtUtc { get; set; }

    public int? ResponseQuota { get; set; }

    [StringLength(50)]
    public string? QuotaBehavior { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    [InverseProperty("Survey")]
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    [InverseProperty("Survey")]
    public virtual ICollection<BranchLogic> BranchLogics { get; set; } = new List<BranchLogic>();

    [ForeignKey("OwnerId")]
    [InverseProperty("Surveys")]
    public virtual User Owner { get; set; } = null!;

    [InverseProperty("Survey")]
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    [InverseProperty("Survey")]
    public virtual ICollection<SurveyChannel> SurveyChannels { get; set; } = new List<SurveyChannel>();

    [InverseProperty("Survey")]
    public virtual ICollection<SurveyCollaborator> SurveyCollaborators { get; set; } = new List<SurveyCollaborator>();

    [InverseProperty("Survey")]
    public virtual ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
