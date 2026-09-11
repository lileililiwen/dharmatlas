using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dharmatlas.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DharmatlasDbContext>
{
    public DharmatlasDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DHARMATLAS_DATABASE_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=dharmatlas;Username=dharmatlas;Password=dharmatlas";
        var options = new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new DharmatlasDbContext(options);
    }
}
