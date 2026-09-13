namespace Dharmatlas.Import;

/// <summary>
/// Region-by-type coverage report for a seed manifest. Coverage is advisory:
/// the validator stays backward compatible with the v1 placeholder seed, while
/// the publication-readiness gate requires a complete matrix for v2.
/// </summary>
public sealed record SeedCoverageReport
{
    public required IReadOnlySet<string> Regions { get; init; }
    public required IReadOnlySet<string> Types { get; init; }
    public required IReadOnlyList<string> MissingRegions { get; init; }
    public required IReadOnlyList<string> MissingTypes { get; init; }
    public bool IsComplete => MissingRegions.Count == 0 && MissingTypes.Count == 0;
}

public static class SeedCoverage
{
    public static readonly IReadOnlyList<string> RequiredRegions = new[]
    {
        "India", "Central Asia", "China", "Sri Lanka", "Tibet", "Korea", "Japan"
    };

    public static readonly IReadOnlyList<string> RequiredTypes = new[]
    {
        "Person", "Place", "Institution", "Text", "Tradition", "Event"
    };

    public static SeedCoverageReport Report(SeedManifest manifest)
    {
        var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in manifest.Entities)
        {
            if (!string.IsNullOrWhiteSpace(entity.Region)) regions.Add(entity.Region);
        }

        var types = new HashSet<string>(manifest.Entities.Select(e => e.Type), StringComparer.OrdinalIgnoreCase);

        return new SeedCoverageReport
        {
            Regions = regions,
            Types = types,
            MissingRegions = RequiredRegions.Where(r => !regions.Contains(r)).ToList(),
            MissingTypes = RequiredTypes.Where(t => !types.Contains(t)).ToList()
        };
    }
}
