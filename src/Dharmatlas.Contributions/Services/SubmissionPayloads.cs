using System.Text.Json;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Contributions.Services;

/// <summary>
/// Maps contribution payload JSON to/from the domain model. The payload is the
/// JSON of a small, deserialization-friendly projection of the proposed value; the
/// mapper reconstructs the immutable domain object (which uses factory methods)
/// from that projection.
/// </summary>
internal static class SubmissionPayloads
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public sealed record DatePayload(string DisplayExpression, string Kind, int? Lower, int? Upper);
    public sealed record SourcePayload(string Title, string? Author, string? Date, string? PublisherOrCollection, string? Identifier);
    public sealed record NamePayload(string Language, string Script, string Romanization, string Value, bool IsPrimary);
    public sealed record EventPayload(string? Summary, string? Category, string? Region, DatePayload? When);
    public sealed record InstitutionPayload(string? Summary, string? InstitutionalForm, string? Region, DatePayload? Activity);
    public sealed record RelationshipPayload(Guid FromEntityId, Guid ToEntityId, string Type, string Certainty, Guid[] SourceIds);

    public static HistoricalDate ReadDate(string json)
    {
        var p = JsonSerializer.Deserialize<DatePayload>(json, Options)!;
        var kind = Enum.Parse<HistoricalDateKind>(p.Kind, ignoreCase: true);
        return kind switch
        {
            HistoricalDateKind.Exact => HistoricalDate.Exact(p.DisplayExpression, p.Lower ?? 0),
            HistoricalDateKind.Approximate => HistoricalDate.Approximate(p.DisplayExpression, p.Lower, p.Upper),
            HistoricalDateKind.Century => HistoricalDate.Century(p.DisplayExpression, p.Lower ?? 0, p.Upper ?? 0),
            HistoricalDateKind.Interval => HistoricalDate.Interval(p.DisplayExpression, p.Lower ?? 0, p.Upper ?? 0),
            HistoricalDateKind.OpenInterval => HistoricalDate.OpenInterval(p.DisplayExpression, p.Lower, p.Upper),
            HistoricalDateKind.Traditional => HistoricalDate.Traditional(p.DisplayExpression, p.Lower, p.Upper),
            _ => throw new DomainValidationException($"Unsupported date kind {p.Kind}.")
        };
    }

    public static string WriteDate(HistoricalDate? date) =>
        date is null
            ? "null"
            : JsonSerializer.Serialize(
                new DatePayload(date.DisplayExpression, date.Kind.ToString(), date.NormalizedLowerBound, date.NormalizedUpperBound),
                Options);

    public static Source ReadSource(string json)
    {
        var p = JsonSerializer.Deserialize<SourcePayload>(json, Options)!;
        return new Source(p.Title)
        {
            Author = p.Author,
            Date = p.Date,
            PublisherOrCollection = p.PublisherOrCollection,
            Identifier = p.Identifier
        };
    }

    public static EntityName ReadName(EntityId targetId, string json)
    {
        var p = JsonSerializer.Deserialize<NamePayload>(json, Options)!;
        return new EntityName(targetId, p.Language, p.Script, p.Romanization, p.Value, p.IsPrimary);
    }

    public static Event ReadEvent(string json)
    {
        var p = JsonSerializer.Deserialize<EventPayload>(json, Options)!;
        var e = new Event
        {
            Summary = p.Summary,
            Category = p.Category,
            Region = p.Region,
            When = p.When is null ? null : ReadDate(JsonSerializer.Serialize(p.When, Options))
        };
        return e;
    }

    public static Institution ReadInstitution(string json)
    {
        var p = JsonSerializer.Deserialize<InstitutionPayload>(json, Options)!;
        return new Institution
        {
            Summary = p.Summary,
            InstitutionalForm = p.InstitutionalForm,
            Activity = p.Activity is null ? null : ReadDate(JsonSerializer.Serialize(p.Activity, Options))
        };
    }

    public static Relationship ReadRelationship(string json)
    {
        var p = JsonSerializer.Deserialize<RelationshipPayload>(json, Options)!;
        return new Relationship(
            EntityId.From(p.FromEntityId),
            EntityId.From(p.ToEntityId),
            p.Type,
            Enum.Parse<Certainty>(p.Certainty, ignoreCase: true),
            p.SourceIds.Select(EntityId.From).ToList());
    }
}
