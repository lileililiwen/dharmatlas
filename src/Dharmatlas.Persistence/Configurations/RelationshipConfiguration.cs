using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// Typed, directional, sourceable edges. Indexed for traversal from either
/// endpoint and by relationship type.
/// </summary>
public class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("relationships");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(r => r.FromEntityId).HasColumnName("from_entity_id").HasConversion(new EntityIdConverter());
        builder.Property(r => r.ToEntityId).HasColumnName("to_entity_id").HasConversion(new EntityIdConverter());
        builder.Property(r => r.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(r => r.Certainty).HasColumnName("certainty").HasConversion<string>();
        builder.Property(r => r.SourceIds).HasColumnName("source_ids")
            .HasColumnType("text").HasConversion(new SourceIdsConverter());

        builder.HasIndex(r => new { r.FromEntityId, r.ToEntityId }, "ix_relationships_endpoints");
        builder.HasIndex(r => r.Type, "ix_relationships_type");
        builder.HasIndex(r => new { r.FromEntityId, r.Id }, "ix_relationships_from_id");
        builder.HasIndex(r => new { r.ToEntityId, r.Id }, "ix_relationships_to_id");
    }
}
