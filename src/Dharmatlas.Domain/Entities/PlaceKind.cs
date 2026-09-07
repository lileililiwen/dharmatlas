namespace Dharmatlas.Domain.Entities;

/// <summary>
/// Distinguishes georeferenced feature kinds shown on the historical map so the
/// surface can render typed layers (e.g. cities vs archaeological sites vs
/// political regions).
/// </summary>
public enum PlaceKind
{
    City,
    Monastery,
    ArchaeologicalSite,
    Region,
    Mountain,
    Other
}
