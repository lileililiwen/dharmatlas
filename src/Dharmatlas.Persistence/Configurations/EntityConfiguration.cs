using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// Table-per-hierarchy mapping for all entity kinds. A single "entities" table
/// carries a discriminator plus the shared identity; type-specific columns live
/// on the same row. Names are stored in their own table and ignored here.
/// </summary>
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable("entities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>();
        builder.Property(e => e.Summary).HasColumnName("summary");
        builder.Ignore(e => e.Names);

        builder.HasDiscriminator<string>("entity_type")
            .HasValue<Person>("Person")
            .HasValue<Place>("Place")
            .HasValue<Institution>("Institution")
            .HasValue<Text>("Text")
            .HasValue<Tradition>("Tradition")
            .HasValue<Event>("Event");

        builder.HasIndex("entity_type").HasDatabaseName("ix_entities_type");
    }
}

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder) => builder.HasBaseType<Entity>().ToTable("entities");
}

public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder) => builder.HasBaseType<Entity>().ToTable("entities");
}

public class TextConfiguration : IEntityTypeConfiguration<Text>
{
    public void Configure(EntityTypeBuilder<Text> builder)
    {
        builder.HasBaseType<Entity>().ToTable("entities");
        builder.Property(e => e.OriginalLanguage).HasColumnName("original_language");
    }
}

public class TraditionConfiguration : IEntityTypeConfiguration<Tradition>
{
    public void Configure(EntityTypeBuilder<Tradition> builder)
    {
        builder.HasBaseType<Entity>().ToTable("entities");
        builder.Property(e => e.Region).HasColumnName("region");
    }
}

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.HasBaseType<Entity>().ToTable("entities");
        builder.Property(e => e.Latitude).HasColumnName("latitude");
        builder.Property(e => e.Longitude).HasColumnName("longitude");
        builder.Property(e => e.ModernName).HasColumnName("modern_name");
        builder.HasIndex(e => new { e.Latitude, e.Longitude }, "ix_places_coords");
    }
}

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasBaseType<Entity>().ToTable("entities");
        builder.Property(e => e.PlaceId).HasColumnName("place_id").HasConversion(new EntityIdConverter());

        builder.OwnsOne(e => e.When, nav =>
        {
            nav.Property(d => d.Kind).HasColumnName("when_kind").HasConversion<string>();
            nav.Property(d => d.DisplayExpression).HasColumnName("when_display");
            nav.Property(d => d.NormalizedLowerBound).HasColumnName("when_lower");
            nav.Property(d => d.NormalizedUpperBound).HasColumnName("when_upper");
            // Supports timeline queries filtering by overlapping normalized bounds
            // without replacing the displayed interval.
            nav.HasIndex(d => new { d.NormalizedLowerBound, d.NormalizedUpperBound }, "ix_events_when_range");
        });
    }
}
