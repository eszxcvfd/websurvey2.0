using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

[Index("Email", Name = "UQ__Users__A9D10534559FF87A", IsUnique = true)]
public partial class User
{
    [Key]
    public Guid UserId { get; set; }

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [MaxLength(256)]
    public byte[] PasswordHash { get; set; } = null!;

    [StringLength(255)]
    public string? FullName { get; set; }

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    [StringLength(200)]
    public string? ResetToken { get; set; }

    public DateTime? ResetTokenExpiry { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    [InverseProperty("User")]
    public virtual ICollection<SurveyCollaborator> SurveyCollaborators { get; set; } = new List<SurveyCollaborator>();

    [InverseProperty("Owner")]
    public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
