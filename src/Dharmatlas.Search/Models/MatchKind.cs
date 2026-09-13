namespace Dharmatlas.Search.Models;

/// <summary>
/// How a query term matched a stored name. Exact ranks above transliteration,
/// transliteration above substring, substring above edit-distance fuzzy. The
/// response carries this value so clients can show why a hit matched; it never
/// merges distinct identities.
/// </summary>
public enum MatchKind
{
    /// <summary>Base-normalized forms are identical.</summary>
    Exact,

    /// <summary>Transliteration-tolerant keys match (e.g. Wade-Giles/Pinyin).</summary>
    Transliteration,

    /// <summary>One normalized form contains the other.</summary>
    Substring,

    /// <summary>Bounded edit-distance match (distance at most 2).</summary>
    Fuzzy
}
