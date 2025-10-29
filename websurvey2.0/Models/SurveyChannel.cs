using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

public partial class SurveyChannel
{
    [Key]
    public Guid ChannelId { get; set; }

    public Guid SurveyId { get; set; }

    [StringLength(50)]
    public string ChannelType { get; set; } = null!;

    [StringLength(200)]
    public string? PublicUrlSlug { get; set; }

    [StringLength(500)]
    public string? FullUrl { get; set; }

    [StringLength(500)]
    public string? QrImagePath { get; set; }

    [StringLength(255)]
    public string? EmailSubject { get; set; }

    public string? EmailBody { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    [ForeignKey("SurveyId")]
    [InverseProperty("SurveyChannels")]
    public virtual Survey Survey { get; set; } = null!;

    [InverseProperty("Channel")]
    public virtual ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
