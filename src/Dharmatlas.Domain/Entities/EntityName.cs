namespace Dharmatlas.Domain.Entities;

/// <summary>
/// One linguistic form of an entity's name. Equivalent forms across scripts and
/// romanizations resolve to the same canonical entity, enabling multilingual
/// search (see the project-foundation contract).
/// </summary>
/// <param name="Id">Stable identifier for the name record.</param>
/// <param name="EntityId">Owning entity.</param>
/// <param name="Language">ISO 639 code, e.g. "san", "chn", "eng".</param>
/// <param name="Script">Writing system, e.g. "devanagari", "han", "latin".</param>
/// <param name="Romanization">Transliteration scheme, e.g. IAST, Pinyin, Wylie.</param>
/// <param name="Value">The name string in the given script/romanization.</param>
/// <param name="IsPrimary">At most one primary name per language.</param>
public sealed record EntityName(
    EntityId Id,
    EntityId EntityId,
    string Language,
    string Script,
    string Romanization,
    string Value,
    bool IsPrimary = false)
{
    public EntityName(EntityId entityId, string language, string script, string romanization, string value, bool isPrimary = false)
        : this(EntityId.New(), entityId, language, script, romanization, value, isPrimary) { }
}
