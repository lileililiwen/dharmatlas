using Dharmatlas.Import;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

var file = string.Empty;
var dryRun = false;
for (var index = 0; index < args.Length; index++)
{
    if (string.Equals(args[index], "--file", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
    {
        file = args[++index];
    }
    else if (string.Equals(args[index], "--dry-run", StringComparison.OrdinalIgnoreCase))
    {
        dryRun = true;
    }
}

if (string.IsNullOrWhiteSpace(file))
{
    Console.Error.WriteLine("Usage: dotnet run --project src/Dharmatlas.Import -- --file data/seed/v2/manifest.json [--dry-run]");
    return 2;
}

try
{
    var manifest = SeedManifestJson.Parse(await File.ReadAllTextAsync(file));

    if (dryRun)
    {
        var validation = SeedManifestValidator.Validate(manifest);
        foreach (var error in validation.Errors) Console.Error.WriteLine($"validation: {error}");

        var readiness = SeedPublicationReadiness.Check(manifest);
        foreach (var error in readiness) Console.Error.WriteLine($"readiness: {error}");

        var coverage = SeedCoverage.Report(manifest);
        Console.WriteLine($"event=seed_coverage entities={manifest.Entities.Count} names={manifest.Names.Count} sources={manifest.Sources.Count} claims={manifest.Claims.Count} relationships={manifest.Relationships.Count} regions={string.Join(",", coverage.Regions.OrderBy(r => r))} types={string.Join(",", coverage.Types.OrderBy(t => t))} complete={coverage.IsComplete}");

        if (!validation.IsValid || readiness.Count > 0)
        {
            Console.Error.WriteLine($"event=seed_dry_run_rejected validation_errors={validation.Errors.Count} readiness_errors={readiness.Count}");
            return 1;
        }

        Console.WriteLine("event=seed_dry_run_accepted");
        return 0;
    }

    var connectionString = Environment.GetEnvironmentVariable("DHARMATLAS_DATABASE_CONNECTION");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("DHARMATLAS_DATABASE_CONNECTION is required.");
        return 2;
    }

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
    Console.Error.WriteLine($"event=seed_import_rejected error_type={ex.GetType().Name} message={ex.Message.Split('\n')[0]} inner={ex.InnerException?.Message.Split('\n')[0]}");
    return 1;
}
