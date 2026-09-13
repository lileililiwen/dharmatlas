namespace Dharmatlas.Import;

/// <summary>
/// Publication-readiness gate for the genuine historical corpus (v2). Stricter
/// than <see cref="SeedManifestValidator"/> so the v1 placeholder seed stays
/// valid: every source needs a tier, every entity needs a resolvable source
/// linkage (direct sourceIds, or at least one claim with resolvable sources),
/// no placeholder tokens survive, and the region/type coverage matrix is
/// complete. Failures are named rejections; callers must abort without writing.
/// </summary>
public static class SeedPublicationReadiness
{
    public static IReadOnlyList<string> Check(SeedManifest manifest)
    {
        var errors = new List<string>();
        var knownSources = new HashSet<string>(manifest.Sources.Select(s => s.Id), StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < manifest.Sources.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(manifest.Sources[i].Tier))
                errors.Add($"sources[{i}] has no sourceTier (code: tier).");
        }

        var claimsBySubject = manifest.Claims
            .GroupBy(c => c.SubjectEntityId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        bool Resolves(IEnumerable<string> ids) => ids.Any(id => knownSources.Contains(id));

        for (var i = 0; i < manifest.Entities.Count; i++)
        {
            var entity = manifest.Entities[i];
            var direct = Resolves(entity.SourceIds);
            var viaClaim = claimsBySubject.TryGetValue(entity.Id, out var claims)
                && claims.Any(c => Resolves(c.SourceIds));
            if (!direct && !viaClaim)
                errors.Add($"entities[{i}] '{entity.Id}' has no resolvable source linkage (code: resolvability).");
        }

        foreach (var (claim, index) in manifest.Claims.Select((value, index) => (value, index)))
        {
            if (!Resolves(claim.SourceIds))
                errors.Add($"claims[{index}] has zero resolvable source IDs (code: resolvability).");
        }

        foreach (var (relationship, index) in manifest.Relationships.Select((value, index) => (value, index)))
        {
            if (!Resolves(relationship.SourceIds))
                errors.Add($"relationships[{index}] has zero resolvable source IDs (code: resolvability).");
        }

        var texts = manifest.Entities.Select(e => e.Summary)
            .Concat(manifest.Claims.Select(c => c.Statement))
            .Concat(manifest.Names.Select(n => n.Value));
        foreach (var text in texts)
        {
            foreach (var token in SeedEditorialRules.PlaceholderHits(text))
                errors.Add($"placeholder token '{token}' survives (code: tone).");
            foreach (var denied in SeedEditorialRules.ToneViolations(text))
                errors.Add($"denied tone '{denied}' in '{text}' (code: tone).");
        }

        var coverage = SeedCoverage.Report(manifest);
        foreach (var region in coverage.MissingRegions)
            errors.Add($"coverage missing region '{region}' (code: coverage).");
        foreach (var type in coverage.MissingTypes)
            errors.Add($"coverage missing entity type '{type}' (code: coverage).");

        return errors;
    }
}
