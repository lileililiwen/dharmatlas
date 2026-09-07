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
        builder.Property(r => r.Timestamp).HasColumnName("timestamp");
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
        builder.Property(c => c.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(c => c.Email).HasColumnName("email");
    }
}
