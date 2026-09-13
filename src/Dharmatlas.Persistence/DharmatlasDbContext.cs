using Dharmatlas.Domain.Contracts;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Persistence;

/// <summary>
/// EF Core model for the historical data model. Maps the domain contracts to
/// PostgreSQL-friendly tables and indexes. Source-link collections are stored as
/// JSON strings; normalized date bounds are exposed as columns so the timeline
/// can filter by overlapping ranges.
/// </summary>
public class DharmatlasDbContext : DbContext
{
    public DharmatlasDbContext(DbContextOptions<DharmatlasDbContext> options) : base(options) { }

    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<EntityName> EntityNames => Set<EntityName>();
    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Revision> Revisions => Set<Revision>();
    public DbSet<Contributor> Contributors => Set<Contributor>();
    public DbSet<AiDraft> AiDrafts => Set<AiDraft>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PopulateNormalizedNames();
        ValidateEntityNames();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PopulateNormalizedNames();
        ValidateEntityNames();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DharmatlasDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private void PopulateNormalizedNames()
    {
        foreach (var entry in ChangeTracker.Entries<EntityName>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property("NormalizedValue").CurrentValue =
                    NameNormalizer.Normalize(entry.Entity.Value);
            }
        }
    }

    private void ValidateEntityNames()
    {
        foreach (var group in EntityNames.Local.GroupBy(n => n.EntityId))
        {
            EntityNameReadModel.Validate(group);
        }

        foreach (var claim in Claims.Local)
        {
            claim.Validate();
        }
    }
}
