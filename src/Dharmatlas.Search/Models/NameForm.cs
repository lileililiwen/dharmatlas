namespace Dharmatlas.Search.Models;

/// <summary>
/// The kind of name form that produced a search hit, so results can explain
/// which recorded name matched the query (provenance requirement).
/// </summary>
public enum NameForm
{
    /// <summary>The entity's primary/canonical name in some language.</summary>
    Canonical,

    /// <summary>A Latin-script romanization/transliteration (e.g. IAST, Pinyin, Wylie).</summary>
    Romanization,

    /// <summary>A name in a non-Latin alternate script (e.g. Devanagari, Han).</summary>
    AlternateScript
}
