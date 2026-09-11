using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// Audit trail of changes: prior value, contributor, reason, and timestamp.
/// </summary>
public class RevisionConfiguration : IEntityTypeConfiguration<Revision>
{
    public void Configure(EntityTypeBuilder<Revision> builder)
    {
        builder.ToTable("revisions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(r => r.TargetId).HasColumnName("target_id").HasConversion(new EntityIdConverter());
        builder.Property(r => r.PriorValueJson).HasColumnName("prior_value_json").IsRequired();
        builder.Property(r => r.ContributorId).HasColumnName("contributor_id").HasConversion(new EntityIdConverter());
        builder.Property(r => r.Reason).HasColumnName("reason").IsRequired();
        builder.Property(r => r.ReviewerId).HasColumnName("reviewer_id").HasConversion(new EntityIdConverter());
        builder.Property(r => r.ChangedFieldsJson).HasColumnName("changed_fields_json");
        builder.Property(r => r.SourceIds).HasColumnName("source_ids")
            .HasColumnType("text").HasConversion(new SourceIdsConverter());
        builder.Property(r => r.Timestamp).HasColumnName("timestamp");
        builder.Property(r => r.CorrelationId).HasColumnName("correlation_id");
        builder.HasIndex(r => r.TargetId, "ix_revisions_target");
    }
}

public class ContributorConfiguration : IEntityTypeConfiguration<Contributor>
{
    public void Configure(EntityTypeBuilder<Contributor> builder)
    {
        builder.ToTable("contributors");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(c => c.ExternalSubject).HasColumnName("external_subject");
        builder.Property(c => c.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(c => c.Email).HasColumnName("email");
        builder.Property(c => c.Roles).HasColumnName("roles").HasColumnType("text")
            .HasConversion(new ContributorRolesConverter());
        builder.HasIndex(c => c.ExternalSubject).IsUnique().HasFilter("external_subject IS NOT NULL");
    }
}
