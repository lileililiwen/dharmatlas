using Dharmatlas.Import;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

var file = string.Empty;
for (var index = 0; index < args.Length; index++)
{
    if (string.Equals(args[index], "--file", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
    {
        file = args[++index];
    }
}

if (string.IsNullOrWhiteSpace(file))
{
    Console.Error.WriteLine("Usage: dotnet run --project src/Dharmatlas.Import -- --file data/seed/v1/manifest.json");
    return 2;
}

var connectionString = Environment.GetEnvironmentVariable("DHARMATLAS_DATABASE_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("DHARMATLAS_DATABASE_CONNECTION is required.");
    return 2;
}

try
{
    var manifest = SeedManifestJson.Parse(await File.ReadAllTextAsync(file));
    var options = new DbContextOptionsBuilder<DharmatlasDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    await using var db = new DharmatlasDbContext(options);
    var result = await SeedImporter.ImportAsync(db, manifest);
    Console.WriteLine($"event=seed_import_completed entities={result.Entities} names={result.Names} sources={result.Sources} claims={result.Claims} relationships={result.Relationships}");
    return 0;
}
catch (Exception ex) when (ex is SeedManifestException or IOException or DbUpdateException)
{
    Console.Error.WriteLine($"event=seed_import_rejected error_type={ex.GetType().Name}");
    return 1;
}
