using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Domain.Entities;

/// <summary>
/// A happening situated in time and optionally place. The date preserves its
/// authored expression plus optional normalized bounds so interval filtering
/// never overwrites the displayed value. <see cref="Category"/> and
/// <see cref="Region"/> support timeline filtering; <see cref="Certainty"/>
/// carries the event's historical-certainty state for display.
/// </summary>
public sealed record Event : Entity
{
    public HistoricalDate? When { get; init; }
    public EntityId? PlaceId { get; init; }
    public string? Category { get; init; }
    public string? Region { get; init; }
    public Certainty Certainty { get; init; } = Certainty.Unknown;

    public Event() : base(EntityType.Event) { }
}
