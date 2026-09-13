using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Search.Models;

namespace Dharmatlas.Search.Engine;

/// <summary>
/// Pure, side-effect-free multilingual search logic. Matches a query against an
/// entity's canonical name and all alternate scripts, transliterations, and
/// romanizations, ranks the matches, and returns one stable hit per entity so a
/// single identity never appears multiple times for its several aliases. Kept
/// independent of EF Core so it can be unit tested without a database.
/// Pipeline per name, best wins: exact normalized match, transliteration-key
/// match (Wade-Giles/Pinyin/Wylie tolerant), substring, then bounded
/// edit-distance fuzzy (distance at most 2). Primary names outrank aliases at
/// every layer. Results are capped at 100 items with stable ordering; ambiguous
/// terms surface every candidate as a separate hit instead of merging.
/// </summary>
public static class SearchEngine
{
    /// <summary>Hard result cap: no query returns more than 100 hits.</summary>
    public const int MaxResults = 100;

    private const int MaxFuzzyDistance = 2;
    private const int MinFuzzyLength = 3;

    private const int ExactPrimaryScore = 100;
    private const int ExactAliasScore = 80;
    private const int TransliterationPrimaryScore = 70;
    private const int TransliterationAliasScore = 60;
    private const int SubstringPrimaryScore = 40;
    private const int SubstringAliasScore = 30;
    private const int FuzzyPrimaryScore = 20;
    private const int FuzzyAliasScore = 15;

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

        var normalizedTerm = NameNormalizer.Normalize(term);
        var keyTerm = NameNormalizer.FoldTransliteration(normalizedTerm);
        var allowedTypes = query.Types;
        var limit = Math.Min(query.Limit ?? MaxResults, MaxResults);
        if (limit < 0)
        {
            limit = 0;
        }

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

            var best = BestMatch(entity, normalizedTerm, keyTerm);
            if (best is null)
            {
                continue;
            }

            hits.Add(new EntitySearchHit
            {
                Id = entity.Id,
                Type = entity.Type,
                CanonicalName = EntityNameReadModel.CanonicalName(entity.Names) ?? entity.Id.ToString(),
                MatchedName = best.Value.Name.Value,
                MatchedForm = Classify(best.Value.Name),
                MatchKind = best.Value.Kind,
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

        return new SearchResult { Term = term, Hits = hits.Take(limit).ToList() };
    }

    private static (EntityName Name, int Score, MatchKind Kind)? BestMatch(
        SearchEntity entity, string normalizedTerm, string keyTerm)
    {
        (EntityName Name, int Score, MatchKind Kind)? best = null;

        foreach (var name in entity.Names)
        {
            var normalizedName = NameNormalizer.Normalize(name.Value);
            if (normalizedName.Length == 0)
            {
                continue;
            }

            MatchKind kind;
            int score;
            var primary = name.IsPrimary;
            if (normalizedName == normalizedTerm)
            {
                kind = MatchKind.Exact;
                score = primary ? ExactPrimaryScore : ExactAliasScore;
            }
            else if (keyTerm.Length > 0 &&
                     NameNormalizer.FoldTransliteration(normalizedName) == keyTerm)
            {
                kind = MatchKind.Transliteration;
                score = primary ? TransliterationPrimaryScore : TransliterationAliasScore;
            }
            else if (normalizedName.Contains(normalizedTerm, StringComparison.Ordinal) ||
                     normalizedTerm.Contains(normalizedName, StringComparison.Ordinal))
            {
                kind = MatchKind.Substring;
                score = primary ? SubstringPrimaryScore : SubstringAliasScore;
            }
            else if (IsFuzzyMatch(normalizedName, normalizedTerm))
            {
                kind = MatchKind.Fuzzy;
                score = primary ? FuzzyPrimaryScore : FuzzyAliasScore;
            }
            else
            {
                continue;
            }

            if (best is null || score > best.Value.Score)
            {
                best = (name, score, kind);
            }
        }

        return best;
    }

    internal static bool IsFuzzyMatch(string normalizedName, string normalizedTerm)
    {
        if (normalizedName.Length < MinFuzzyLength || normalizedTerm.Length < MinFuzzyLength)
        {
            return false;
        }

        if (Math.Abs(normalizedName.Length - normalizedTerm.Length) > MaxFuzzyDistance)
        {
            return false;
        }

        return NameNormalizer.LevenshteinDistance(normalizedName, normalizedTerm) <= MaxFuzzyDistance;
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

    /// <summary>
    /// Case- and diacritic-insensitive normalization for matching across scripts
    /// and romanizations (e.g. "Hsuan-tsang" matches "Xuanzang", "玄奘").
    /// Delegates to <see cref="NameNormalizer"/> so persistence and search share
    /// one definition.
    /// </summary>
    internal static string Normalize(string value) => NameNormalizer.Normalize(value);
}
