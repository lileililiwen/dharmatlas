using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dharmatlas.Persistence.Configurations;

/// <summary>
/// First-class bibliographic sources, referenced by claims and relationships.
/// </summary>
public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("sources");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasConversion(new EntityIdConverter());
        builder.Property(s => s.Title).HasColumnName("title").IsRequired();
        builder.Property(s => s.Author).HasColumnName("author");
        builder.Property(s => s.Date).HasColumnName("date");
        builder.Property(s => s.PublisherOrCollection).HasColumnName("publisher_or_collection");
        builder.Property(s => s.Identifier).HasColumnName("identifier");
        builder.Property(s => s.Tier).HasColumnName("tier").HasConversion<string?>();
        builder.HasIndex(s => s.Identifier, "ix_sources_identifier").IsUnique(false);
    }
}
