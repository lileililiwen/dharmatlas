using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// Stores contributions as their own table with the proposed payload as JSON and
/// the decision trail. The status column is the single source of truth for whether
/// a contribution has reached the published state.
/// </summary>
public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(s => s.ContributorId).HasColumnName("contributor_id").HasConversion(new EntityIdConverter());
        builder.Property(s => s.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(s => s.TargetId).HasColumnName("target_id").HasConversion(new EntityIdConverter());
        builder.Property(s => s.Summary).HasColumnName("summary").IsRequired();
        builder.Property(s => s.PayloadJson).HasColumnName("payload_json").IsRequired();
        builder.Property(s => s.SourceIds).HasColumnName("source_ids")
            .HasColumnType("text").HasConversion(new SourceIdsConverter());
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.OwnsMany(s => s.Decisions, nav =>
        {
            nav.ToTable("submission_decisions");
            nav.Property(d => d.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
            nav.Property(d => d.ReviewerId).HasColumnName("reviewer_id").HasConversion(new EntityIdConverter());
            nav.Property(d => d.Decision).HasColumnName("decision").HasConversion<string>();
            nav.Property(d => d.Reason).HasColumnName("reason").IsRequired();
            nav.Property(d => d.Timestamp).HasColumnName("timestamp");
            nav.WithOwner().HasForeignKey("submission_id");
        });

        builder.HasIndex(s => s.Status, "ix_submissions_status");
        builder.HasIndex(s => s.TargetId, "ix_submissions_target");
    }
}
