using Dharmatlas.Domain;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Map.Models;

namespace Dharmatlas.Api.Models;

public sealed record SearchResponseView
{
    public required string Term { get; init; }
    public required IReadOnlyList<SearchHitView> Hits { get; init; }
}

public sealed record SearchHitView
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string CanonicalName { get; init; }
    public required string MatchedName { get; init; }
    public required string MatchedForm { get; init; }
    public required string MatchKind { get; init; }
    public string? Region { get; init; }
    public string? ActivePeriod { get; init; }
    public required string Certainty { get; init; }
    public int Score { get; init; }
    public required string DetailRoute { get; init; }
}

public sealed record TimelineResponseView
{
    public required TimelineQueryView Query { get; init; }
    public required IReadOnlyList<TimelineEventView> Events { get; init; }
}

public sealed record TimelineQueryView
{
    public int? FromYear { get; init; }
    public int? ToYear { get; init; }
    public string? Region { get; init; }
    public string? Category { get; init; }
    public bool IncludeUnknownDates { get; init; }
}

public sealed record TimelineEventView
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string DisplayDate { get; init; }
    public required string Certainty { get; init; }
    public string? Category { get; init; }
    public string? Region { get; init; }
    public required IReadOnlyList<string> LinkedEntityIds { get; init; }
    public required string DetailRoute { get; init; }
}

public sealed record MapResponseView
{
    public required int Year { get; init; }
    public required IReadOnlyList<MapFeatureView> Features { get; init; }
    public required ViewportView Viewport { get; init; }
}

public sealed record MapFeatureView
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string ActivityExpression { get; init; }
    public required string Certainty { get; init; }
    public required IReadOnlyList<string> SourceIds { get; init; }
    public required IReadOnlyList<GeoPoint> Geometry { get; init; }
    public string? Kind { get; init; }
    public string? Region { get; init; }
    public required string DetailRoute { get; init; }
}

public sealed record ViewportView
{
    public double MinLatitude { get; init; }
    public double MaxLatitude { get; init; }
    public double MinLongitude { get; init; }
    public double MaxLongitude { get; init; }
}

public sealed record SimpleEntityView : EntityRef
{
    public required IReadOnlyList<NameView> Names { get; init; }
    public string? Summary { get; init; }
    public string? Region { get; init; }
    public required string Certainty { get; init; }
    public required IReadOnlyList<SourceView> Sources { get; init; }
    public required IReadOnlyList<ClaimView> Claims { get; init; }
}
