using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// Source-linked claims. Certainty and status are indexed together so the
/// review surface can isolate disputed or traditional accounts, and
/// subject-scoped lookups are supported by the subject index.
/// </summary>
public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(c => c.Statement).HasColumnName("statement").IsRequired();
        builder.Property(c => c.Certainty).HasColumnName("certainty").HasConversion<string>();
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(c => c.SourceIds).HasColumnName("source_ids")
            .HasColumnType("text").HasConversion(new SourceIdsConverter());
        builder.Property(c => c.SubjectEntityId).HasColumnName("subject_entity_id")
            .HasConversion(new EntityIdConverter());
        builder.Property(c => c.Interpretation).HasColumnName("interpretation").HasConversion<string>();
        builder.Property(c => c.SourceLocator).HasColumnName("source_locator").HasMaxLength(256);

        builder.HasIndex(c => new { c.Certainty, c.Status }, "ix_claims_certainty_status");
        builder.HasIndex(c => c.SubjectEntityId, "ix_claims_subject");
        builder.HasIndex(c => new { c.SubjectEntityId, c.Status }, "ix_claims_subject_publication");
    }
}
