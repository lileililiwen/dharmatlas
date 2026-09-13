using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.AI.Engine;

/// <summary>One automated citation finding. Review-only: it never publishes, merges, or mutates records.</summary>
public sealed record CitationFlag(string SubjectId, string Kind, string Detail)
{
    public const string MissingSource = "missing-source";
    public const string TierMismatch = "tier-mismatch";
}

/// <summary>Outcome of checking a set of claims and relationships against known source tiers.</summary>
public sealed record CitationCheckReport
{
    public required IReadOnlyList<CitationFlag> Flags { get; init; }
    public bool IsClean => Flags.Count == 0;
}

/// <summary>
/// Pure, deterministic citation verification over supplied material. Flags
/// unresolvable source references and tier mismatches (a Documented claim
/// backed only by traditional sources, or a TraditionalAccount claim with no
/// traditional source behind it). It returns a report for human review; only a
/// reviewer decision changes record state.
/// </summary>
public static class CitationChecker
{
    public static CitationCheckReport Check(
        IReadOnlyList<(string SubjectId, Certainty Certainty, ClaimInterpretation Interpretation, IReadOnlyList<EntityId> SourceIds)> subjects,
        IReadOnlyDictionary<EntityId, SourceTier?> sourceTiers)
    {
        var flags = new List<CitationFlag>();
        foreach (var (subjectId, certainty, interpretation, sourceIds) in subjects)
        {
            var resolved = sourceIds.Where(sourceTiers.ContainsKey).ToList();
            if (resolved.Count == 0)
            {
                flags.Add(new CitationFlag(subjectId, CitationFlag.MissingSource,
                    "No cited source resolves to a known record."));
                continue;
            }

            var tiers = resolved.Select(id => sourceTiers[id]).ToList();
            var hasNonTraditional = tiers.Any(t => t is null || t != SourceTier.Traditional);
            var hasTraditional = tiers.Any(t => t == SourceTier.Traditional);

            if (certainty == Certainty.Documented && !hasNonTraditional)
            {
                flags.Add(new CitationFlag(subjectId, CitationFlag.TierMismatch,
                    "A Documented claim is backed only by traditional-tier sources."));
            }

            if (interpretation == ClaimInterpretation.TraditionalAccount && !hasTraditional)
            {
                flags.Add(new CitationFlag(subjectId, CitationFlag.TierMismatch,
                    "A TraditionalAccount claim cites no traditional-tier source."));
            }
        }

        return new CitationCheckReport { Flags = flags };
    }
}
