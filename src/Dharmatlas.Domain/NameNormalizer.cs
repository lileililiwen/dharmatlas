using System.Globalization;
using System.Text;

namespace Dharmatlas.Domain;

/// <summary>
/// Pure, dependency-free script-aware normalization for multilingual name
/// matching. The authored display form is never altered here; callers keep the
/// original value for display and index or compare only the normalized forms.
/// Layers, in order: Unicode NFKC, casefold, diacritic strip, compatibility
/// folds (ß/æ/œ/ł/đ/ð/þ/ŋ), transliteration-tolerant folding (Wade-Giles and
/// Pinyin initials, separator removal), and CJK bigram tokenization for
/// full-text indexing. Ambiguous inputs are never merged silently: the engine
/// surfaces every candidate as a separate hit with its matched form shown.
/// </summary>
public static class NameNormalizer
{
    /// <summary>
    /// Base normalization: NFKC, lower-invariant casefold, diacritic strip, and
    /// compatibility folds. Pinyin tone marks (Xuánzàng), IAST macrons (Nālandā),
    /// and fullwidth forms all fold to plain ASCII where a plain equivalent
    /// exists. CJK and other non-decomposable scripts pass through unchanged
    /// apart from NFKC compatibility folding. Null or whitespace yields "".
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var form = value.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        // Strip diacritics per character. Hiragana/Katakana are appended whole:
        // decomposing them would drop voicing marks (が → か) and merge
        // distinct kana. Other scripts decompose; spacing vowel signs (Mc, e.g.
        // Devanagari matras) survive, while non-spacing marks fold on both the
        // query and the stored side, so either side may omit them.
        var builder = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (IsKana(ch))
            {
                builder.Append(ch);
                continue;
            }

            var decomposed = ch.ToString().Normalize(NormalizationForm.FormD);
            foreach (var part in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(part) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(part);
                }
            }
        }

        return FoldCompatibility(builder.ToString().Normalize(NormalizationForm.FormC));
    }

    /// <summary>
    /// Transliteration-tolerant key over an already base-normalized string:
    /// removes word separators (hyphen, apostrophe, middle dot, whitespace) and
    /// folds documented Wade-Giles/Pinyin initial and rime equivalences
    /// (hs→x, ts/tz→z, -ien→-ian). Two names sharing a key are a
    /// transliteration match, never an exact match; the engine ranks it below
    /// exact and shows the matched form.
    /// </summary>
    public static string FoldTransliteration(string normalized)
    {
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (ch is '-' or '\'' or '’' or '‘' or '·' or '.' or '/')
            {
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString()
            .Replace("hs", "x", StringComparison.Ordinal)
            .Replace("tz", "z", StringComparison.Ordinal)
            .Replace("ts", "z", StringComparison.Ordinal)
            .Replace("ien", "ian", StringComparison.Ordinal);
    }

    /// <summary>
    /// Full search key for a raw name value: base normalization followed by
    /// transliteration folding. Null or whitespace yields "".
    /// </summary>
    public static string SearchKey(string? value) => FoldTransliteration(Normalize(value));

    /// <summary>
    /// Overlapping bigram tokens over the CJK runs of a raw value, for
    /// trigram/full-text indexing of unsegmented Han text. Non-CJK input
    /// yields no tokens. Single CJK characters yield the character itself so
    /// short queries remain retrievable.
    /// </summary>
    public static IReadOnlyList<string> CjkBigrams(string? value)
    {
        var normalized = Normalize(value);
        var runs = new List<string>();
        var current = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (IsCjk(ch))
            {
                current.Append(ch);
            }
            else if (current.Length > 0)
            {
                runs.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            runs.Add(current.ToString());
        }

        var tokens = new List<string>();
        foreach (var run in runs)
        {
            if (run.Length == 1)
            {
                tokens.Add(run);
                continue;
            }

            for (var i = 0; i + 1 < run.Length; i++)
            {
                tokens.Add(run.Substring(i, 2));
            }
        }

        return tokens;
    }

    /// <summary>
    /// True for CJK unified ideographs, Hiragana, Katakana, Hangul syllables
    /// and jamo, Bopomofo, and Tibetan block characters.
    /// </summary>
    public static bool IsCjk(char ch) => ch switch
    {
        >= '\u4E00' and <= '\u9FFF' => true, // CJK Unified Ideographs
        >= '\u3400' and <= '\u4DBF' => true, // Extension A
        >= '\u3040' and <= '\u309F' => true, // Hiragana
        >= '\u30A0' and <= '\u30FF' => true, // Katakana
        >= '\uAC00' and <= '\uD7AF' => true, // Hangul syllables
        >= '\u1100' and <= '\u11FF' => true, // Hangul jamo
        >= '\u3130' and <= '\u318F' => true, // Hangul compatibility jamo
        >= '\u3100' and <= '\u312F' => true, // Bopomofo
        >= '\u0F00' and <= '\u0FFF' => true, // Tibetan
        _ => false
    };

    /// <summary>
    /// True for Hiragana and Katakana. Voicing marks on these kana are
    /// contrastive, so normalization must not decompose them.
    /// </summary>
    internal static bool IsKana(char ch) =>
        (ch >= '\u3040' && ch <= '\u309F') || (ch >= '\u30A0' && ch <= '\u30FF');

    /// <summary>
    /// Classic Levenshtein edit distance over already-normalized strings, used
    /// for threshold-gated fuzzy matching. Inputs are short name forms; callers
    /// cap the allowed distance (at most 2) and the result count (at most 100).
    /// </summary>
    public static int LevenshteinDistance(string? a, string? b)
    {
        a ??= string.Empty;
        b ??= string.Empty;
        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    private static string FoldCompatibility(string value)
    {
        // Characters whose compatibility equivalents survive NFKC + mark-strip.
        return value
            .Replace("ß", "ss", StringComparison.Ordinal)
            .Replace("æ", "ae", StringComparison.Ordinal)
            .Replace("œ", "oe", StringComparison.Ordinal)
            .Replace("ł", "l", StringComparison.Ordinal)
            .Replace("đ", "d", StringComparison.Ordinal)
            .Replace("ð", "d", StringComparison.Ordinal)
            .Replace("þ", "th", StringComparison.Ordinal)
            .Replace("ŋ", "n", StringComparison.Ordinal)
            .Replace("ĳ", "ij", StringComparison.Ordinal);
    }
}
