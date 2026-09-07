using Dharmatlas.Domain.Contracts;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DharmatlasDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
