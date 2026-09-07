using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// Stores each multilingual name in its own row, linked to the owning entity,
/// with indexes that support alternate-script and romanization search.
/// </summary>
public class EntityNameConfiguration : IEntityTypeConfiguration<EntityName>
{
    public void Configure(EntityTypeBuilder<EntityName> builder)
    {
        builder.ToTable("entity_names");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(n => n.EntityId).HasColumnName("entity_id").HasConversion(new EntityIdConverter());
        builder.Property(n => n.Language).HasColumnName("language").HasMaxLength(16).IsRequired();
        builder.Property(n => n.Script).HasColumnName("script").HasMaxLength(32).IsRequired();
        builder.Property(n => n.Romanization).HasColumnName("romanization").HasMaxLength(32).IsRequired();
        builder.Property(n => n.Value).HasColumnName("value").IsRequired();
        builder.Property(n => n.IsPrimary).HasColumnName("is_primary");

        builder.HasIndex(n => new { n.Language, n.Value }, "ix_entity_names_lang_value");
        builder.HasIndex(n => n.Value, "ix_entity_names_value");
        // Support multilingual search: lookups by writing system and romanization
        // scheme let the engine match alternate scripts and transliterations.
        builder.HasIndex(n => n.Script, "ix_entity_names_script");
        builder.HasIndex(n => n.Romanization, "ix_entity_names_romanization");
    }
}
