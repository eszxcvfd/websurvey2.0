using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace websurvey2._0.Models;

public partial class SurveyDbContext : DbContext
{
    public SurveyDbContext()
    {
    }

    public SurveyDbContext(DbContextOptions<SurveyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<BranchLogic> BranchLogics { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionOption> QuestionOptions { get; set; }

    public virtual DbSet<ResponseAnswer> ResponseAnswers { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<SurveyChannel> SurveyChannels { get; set; }

    public virtual DbSet<SurveyCollaborator> SurveyCollaborators { get; set; }

    public virtual DbSet<SurveyResponse> SurveyResponses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Activity__5E5486485438AD28");

            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Response).WithMany(p => p.ActivityLogs).HasConstraintName("FK_ActivityLog_Responses");

            entity.HasOne(d => d.Survey).WithMany(p => p.ActivityLogs).HasConstraintName("FK_ActivityLog_Surveys");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs).HasConstraintName("FK_ActivityLog_Users");
        });

        modelBuilder.Entity<BranchLogic>(entity =>
        {
            entity.HasKey(e => e.LogicId).HasName("PK__BranchLo__4A718C1DDA872353");

            entity.Property(e => e.LogicId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PriorityOrder).HasDefaultValue(1);

            entity.HasOne(d => d.SourceQuestion).WithMany(p => p.BranchLogicSourceQuestions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchLogic_SourceQ");

            entity.HasOne(d => d.Survey).WithMany(p => p.BranchLogics).HasConstraintName("FK_BranchLogic_Surveys");

            entity.HasOne(d => d.TargetQuestion).WithMany(p => p.BranchLogicTargetQuestions).HasConstraintName("FK_BranchLogic_TargetQ");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FACCD651F63");

            entity.Property(e => e.QuestionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Survey).WithMany(p => p.Questions).HasConstraintName("FK_Questions_Surveys");
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(e => e.OptionId).HasName("PK__Question__92C7A1FFB7B84025");

            entity.Property(e => e.OptionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionOptions).HasConstraintName("FK_QuestionOptions_Questions");
        });

        modelBuilder.Entity<ResponseAnswer>(entity =>
        {
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Question).WithMany(p => p.ResponseAnswers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResponseAnswers_Questions");

            entity.HasOne(d => d.Response).WithMany(p => p.ResponseAnswers).HasConstraintName("FK_ResponseAnswers_Responses");
        });

        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasKey(e => e.SurveyId).HasName("PK__Surveys__A5481F7DD5B087E5");

            entity.Property(e => e.SurveyId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Draft");
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Owner).WithMany(p => p.Surveys)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Surveys_Users");
        });

        modelBuilder.Entity<SurveyChannel>(entity =>
        {
            entity.HasKey(e => e.ChannelId).HasName("PK__SurveyCh__38C3E8142425123F");

            entity.Property(e => e.ChannelId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Survey).WithMany(p => p.SurveyChannels).HasConstraintName("FK_SurveyChannels_Surveys");
        });

        modelBuilder.Entity<SurveyCollaborator>(entity =>
        {
            entity.Property(e => e.GrantedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Survey).WithMany(p => p.SurveyCollaborators).HasConstraintName("FK_SurveyCollaborators_Surveys");

            entity.HasOne(d => d.User).WithMany(p => p.SurveyCollaborators).HasConstraintName("FK_SurveyCollaborators_Users");
        });

        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.HasKey(e => e.ResponseId).HasName("PK__SurveyRe__1AAA646C5AFFADE2");

            entity.Property(e => e.ResponseId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LastUpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Submitted");

            entity.HasOne(d => d.Channel).WithMany(p => p.SurveyResponses).HasConstraintName("FK_SurveyResponses_Channels");

            entity.HasOne(d => d.Survey).WithMany(p => p.SurveyResponses).HasConstraintName("FK_SurveyResponses_Surveys");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CC3149C2C");

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
