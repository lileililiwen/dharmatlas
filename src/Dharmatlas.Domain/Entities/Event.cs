using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Domain.Entities;

/// <summary>
/// A happening situated in time and optionally place. The date preserves its
/// authored expression plus optional normalized bounds so interval filtering
/// never overwrites the displayed value.
/// </summary>
public sealed record Event : Entity
{
    public HistoricalDate? When { get; init; }
    public EntityId? PlaceId { get; init; }

    public Event() : base(EntityType.Event) { }
}
