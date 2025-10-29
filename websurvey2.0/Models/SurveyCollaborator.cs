using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

[PrimaryKey("SurveyId", "UserId")]
public partial class SurveyCollaborator
{
    [Key]
    public Guid SurveyId { get; set; }

    [Key]
    public Guid UserId { get; set; }

    [StringLength(50)]
    public string Role { get; set; } = null!;

    public DateTime GrantedAtUtc { get; set; }

    public Guid? GrantedBy { get; set; }

    [ForeignKey("SurveyId")]
    [InverseProperty("SurveyCollaborators")]
    public virtual Survey Survey { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("SurveyCollaborators")]
    public virtual User User { get; set; } = null!;
}
