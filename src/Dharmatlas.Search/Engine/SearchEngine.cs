using System.Globalization;
using System.Text;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Search.Models;

namespace Dharmatlas.Search.Engine;

/// <summary>
/// Pure, side-effect-free multilingual search logic. Matches a query against an
/// entity's canonical name and all alternate scripts, transliterations, and
/// romanizations, ranks the matches, and returns one stable hit per entity so a
/// single identity never appears multiple times for its several aliases. Kept
/// independent of EF Core so it can be unit tested without a database.
/// </summary>
public static class SearchEngine
{
    private const int ExactPrimaryScore = 100;
    private const int ExactAliasScore = 80;
    private const int SubstringScore = 40;

    /// <summary>
    /// Runs a search over the supplied (already materialized) entities. Returns a
    /// ranked, de-duplicated result. An empty/whitespace term yields no hits.
    /// </summary>
    public static SearchResult Search(SearchQuery query, IReadOnlyList<SearchEntity> entities)
    {
        var term = (query.Term ?? string.Empty).Trim();
        if (term.Length == 0)
        {
            return new SearchResult { Term = term, Hits = Array.Empty<EntitySearchHit>() };
        }

        var normalizedTerm = Normalize(term);
        var allowedTypes = query.Types;

        var hits = new List<EntitySearchHit>();

        foreach (var entity in entities)
        {
            if (allowedTypes is { Count: > 0 } && !allowedTypes.Contains(entity.Type))
            {
                continue;
            }

            if (query.Region is not null &&
                !string.Equals(entity.Region, query.Region, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var best = BestMatch(entity, normalizedTerm);
            if (best is null)
            {
                continue;
            }

            hits.Add(new EntitySearchHit
            {
                Id = entity.Id,
                Type = entity.Type,
                CanonicalName = CanonicalNameOf(entity),
                MatchedName = best.Value.Name.Value,
                MatchedForm = Classify(best.Value.Name),
                Region = entity.Region,
                ActivePeriod = entity.ActivePeriod,
                Certainty = entity.Certainty,
                Score = best.Value.Score
            });
        }

        hits.Sort((a, b) =>
        {
            var byScore = b.Score.CompareTo(a.Score);
            if (byScore != 0)
            {
                return byScore;
            }

            // Stable, informative tie-break: type, then region, then canonical name.
            var byType = a.Type.CompareTo(b.Type);
            if (byType != 0)
            {
                return byType;
            }

            var byRegion = string.CompareOrdinal(a.Region ?? string.Empty, b.Region ?? string.Empty);
            if (byRegion != 0)
            {
                return byRegion;
            }

            return string.CompareOrdinal(a.CanonicalName, b.CanonicalName);
        });

        var limited = query.Limit is { } limit && limit >= 0
            ? hits.Take(limit).ToList()
            : hits;

        return new SearchResult { Term = term, Hits = limited };
    }

    private static (EntityName Name, int Score)? BestMatch(SearchEntity entity, string normalizedTerm)
    {
        (EntityName Name, int Score)? best = null;

        foreach (var name in entity.Names)
        {
            var normalizedName = Normalize(name.Value);
            if (normalizedName.Length == 0)
            {
                continue;
            }

            int score;
            if (normalizedName == normalizedTerm)
            {
                score = name.IsPrimary ? ExactPrimaryScore : ExactAliasScore;
            }
            else if (normalizedName.Contains(normalizedTerm, StringComparison.Ordinal) ||
                     normalizedTerm.Contains(normalizedName, StringComparison.Ordinal))
            {
                score = SubstringScore;
            }
            else
            {
                continue;
            }

            if (best is null || score > best.Value.Score)
            {
                best = (name, score);
            }
        }

        return best;
    }

    private static NameForm Classify(EntityName name)
    {
        if (name.IsPrimary)
        {
            return NameForm.Canonical;
        }

        return string.Equals(name.Script, "latin", StringComparison.OrdinalIgnoreCase)
            ? NameForm.Romanization
            : NameForm.AlternateScript;
    }

    private static string CanonicalNameOf(SearchEntity entity)
    {
        var primary = entity.Names.FirstOrDefault(n => n.IsPrimary) ?? entity.Names.FirstOrDefault();
        return primary?.Value ?? entity.Id.ToString();
    }

    /// <summary>
    /// Case- and diacritic-insensitive normalization for matching across scripts
    /// and romanizations (e.g. "Hsuan-tsang" matches "Xuanzang", "玄奘").
    /// </summary>
    internal static string Normalize(string value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
