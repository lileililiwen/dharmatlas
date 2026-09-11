using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Api.Models;

/// <summary>One recorded name form of an entity in API responses.</summary>
public sealed record NameView
{
    public required string Value { get; init; }
    public required string Language { get; init; }
    public required string Script { get; init; }
    public required string Romanization { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>A citable source, stripped of any private contributor information.</summary>
public sealed record SourceView
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Author { get; init; }
    public string? Date { get; init; }
    public string? Identifier { get; init; }
}

/// <summary>Public evidence attached to a published historical assertion.</summary>
public sealed record ClaimView
{
    public required string Id { get; init; }
    public string? SubjectEntityId { get; init; }
    public required string Statement { get; init; }
    public required string Certainty { get; init; }
    public required string Interpretation { get; init; }
    public string? SourceLocator { get; init; }
    public required IReadOnlyList<SourceView> Sources { get; init; }
}

/// <summary>Uncertainty-aware date with its authored expression preserved.</summary>
public sealed record DateView
{
    public required string Kind { get; init; }
    public required string DisplayExpression { get; init; }
    public int? LowerBound { get; init; }
    public int? UpperBound { get; init; }

    public static DateView? From(HistoricalDate? date) => date is null
        ? null
        : new DateView
        {
            Kind = date.Kind.ToString(),
            DisplayExpression = date.DisplayExpression,
            LowerBound = date.NormalizedLowerBound,
            UpperBound = date.NormalizedUpperBound
        };
}

/// <summary>Minimal stable reference to an entity. Base for richer entity views.</summary>
public record EntityRef
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string CanonicalName { get; init; }
}

/// <summary>A related entity with the relationship that connects it.</summary>
public sealed record RelatedRef : EntityRef
{
    public required string RelationType { get; init; }
    public required string Direction { get; init; }
    public required string Certainty { get; init; }
    public required IReadOnlyList<string> SourceIds { get; init; }
}

/// <summary>Read-only person payload.</summary>
public sealed record PersonView : EntityRef
{
    public required IReadOnlyList<NameView> Names { get; init; }
    public string? Summary { get; init; }
    public string? Region { get; init; }
    public string Certainty { get; init; } = Dharmatlas.Domain.ValueObjects.Certainty.Unknown.ToString();
    public string? ActivePeriod { get; init; }
    public required IReadOnlyList<SourceView> Sources { get; init; }
    public required IReadOnlyList<ClaimView> Claims { get; init; }
    public required IReadOnlyList<RelatedRef> Relationships { get; init; }
}

/// <summary>Read-only event payload with date bounds, location, participants, and sources.</summary>
public sealed record EventView : EntityRef
{
    public required IReadOnlyList<NameView> Names { get; init; }
    public string? Summary { get; init; }
    public string? Category { get; init; }
    public string? Region { get; init; }
    public string Certainty { get; init; } = Dharmatlas.Domain.ValueObjects.Certainty.Unknown.ToString();
    public DateView? When { get; init; }
    public EntityRef? Place { get; init; }
    public required IReadOnlyList<RelatedRef> Participants { get; init; }
    public required IReadOnlyList<SourceView> Sources { get; init; }
    public required IReadOnlyList<ClaimView> Claims { get; init; }
}

/// <summary>Read-only place payload with geography, kind, activity, and sources.</summary>
public sealed record PlaceView : EntityRef
{
    public required IReadOnlyList<NameView> Names { get; init; }
    public string? Summary { get; init; }
    public string? ModernName { get; init; }
    public string Kind { get; init; } = "City";
    public string? Region { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateView? Activity { get; init; }
    public string Certainty { get; init; } = Dharmatlas.Domain.ValueObjects.Certainty.Unknown.ToString();
    public required IReadOnlyList<SourceView> Sources { get; init; }
    public required IReadOnlyList<ClaimView> Claims { get; init; }
}

/// <summary>Read-only text payload with language and sources.</summary>
public sealed record TextView : EntityRef
{
    public required IReadOnlyList<NameView> Names { get; init; }
    public string? Summary { get; init; }
    public string? OriginalLanguage { get; init; }
    public required IReadOnlyList<SourceView> Sources { get; init; }
    public required IReadOnlyList<ClaimView> Claims { get; init; }
}

/// <summary>Read-only relationship payload with resolved endpoints and sources.</summary>
public sealed record RelationshipView
{
    public required string Id { get; init; }
    public required EntityRef From { get; init; }
    public required EntityRef To { get; init; }
    public required string Type { get; init; }
    public required string Certainty { get; init; }
    public required IReadOnlyList<string> SourceIds { get; init; }
}

/// <summary>Lighter event shape used in list responses.</summary>
public sealed record EventSummaryView : EntityRef
{
    public DateView? When { get; init; }
    public string? Region { get; init; }
    public string? Category { get; init; }
    public string Certainty { get; init; } = Dharmatlas.Domain.ValueObjects.Certainty.Unknown.ToString();
}

/// <summary>Discovery metadata describing the public surface.</summary>
public sealed record ApiMetaView
{
    public required string ApiVersion { get; init; }
    public required string SchemaVersion { get; init; }
    public required string License { get; init; }
    public required string LicenseUrl { get; init; }
    public int CacheMaxAgeSeconds { get; init; }
    public int RateLimitPerMinute { get; init; }
    public required IReadOnlyList<EndpointView> Endpoints { get; init; }
}

/// <summary>One endpoint declared by the public API.</summary>
public sealed record EndpointView
{
    public required string Method { get; init; }
    public required string Path { get; init; }
    public required string Description { get; init; }
}
