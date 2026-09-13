using Dharmatlas.Api.Models;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Dharmatlas.Search.Models;
using Dharmatlas.Search.Services;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Api.Services;

/// <summary>
/// Read-only assembly of the public API surface from the published data model. It
/// queries only the published tables (entities, names, relationships, sources) so
/// drafts, rejected contributions, and private contributor data are never surfaced.
/// Every method is side-effect free.
/// </summary>
public sealed class ApiQueryService
{
    private readonly DharmatlasDbContext _db;
    private readonly EntityDetailService _details;

    public ApiQueryService(DharmatlasDbContext db)
    {
        _db = db;
        _details = new EntityDetailService(db);
    }

    public async Task<PersonView?> GetPersonAsync(EntityId id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Entities.OfType<Person>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var detail = await _details.GetAsync(id, cancellationToken);
        return new PersonView
        {
            Id = entity.Id.ToString(),
            Type = EntityType.Person.ToString(),
            CanonicalName = detail!.CanonicalName,
            Names = MapNames(detail.Names),
            Summary = entity.Summary,
            Region = detail.Region,
            Certainty = detail.Certainty.ToString(),
            ActivePeriod = detail.ActivePeriod,
            Sources = detail.Sources.Select(ToSourceView).ToList(),
            Claims = ToClaimViews(detail.Claims, detail.Sources),
            Relationships = detail.Relationships.Select(ToRelatedRef).ToList()
        };
    }

    public async Task<EventView?> GetEventAsync(EntityId id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Entities.OfType<Event>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var detail = await _details.GetAsync(id, cancellationToken);
        EntityRef? place = null;
        if (entity.PlaceId is { } pid)
        {
            var placeName = await CanonicalNameAsync(pid, cancellationToken);
            place = new EntityRef { Id = pid.ToString(), Type = EntityType.Place.ToString(), CanonicalName = placeName };
        }

        return new EventView
        {
            Id = entity.Id.ToString(),
            Type = EntityType.Event.ToString(),
            CanonicalName = detail!.CanonicalName,
            Names = MapNames(detail.Names),
            Summary = entity.Summary,
            Category = entity.Category,
            Region = entity.Region,
            Certainty = entity.Certainty.ToString(),
            When = DateView.From(entity.When),
            Place = place,
            Participants = detail.Relationships.Select(ToRelatedRef).ToList(),
            Sources = detail.Sources.Select(ToSourceView).ToList(),
            Claims = ToClaimViews(detail.Claims, detail.Sources)
        };
    }

    public async Task<PlaceView?> GetPlaceAsync(EntityId id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Entities.OfType<Place>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var detail = await _details.GetAsync(id, cancellationToken);
        return new PlaceView
        {
            Id = entity.Id.ToString(),
            Type = EntityType.Place.ToString(),
            CanonicalName = detail!.CanonicalName,
            Names = MapNames(detail.Names),
            Summary = entity.Summary,
            ModernName = entity.ModernName,
            Kind = entity.Kind.ToString(),
            Region = entity.Region,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Activity = DateView.From(entity.Activity),
            Certainty = entity.Certainty.ToString(),
            Sources = detail.Sources.Select(ToSourceView).ToList(),
            Claims = ToClaimViews(detail.Claims, detail.Sources)
        };
    }

    public async Task<TextView?> GetTextAsync(EntityId id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Entities.OfType<Text>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var detail = await _details.GetAsync(id, cancellationToken);
        return new TextView
        {
            Id = entity.Id.ToString(),
            Type = EntityType.Text.ToString(),
            CanonicalName = detail!.CanonicalName,
            Names = MapNames(detail.Names),
            Summary = entity.Summary,
            OriginalLanguage = entity.OriginalLanguage,
            Sources = detail.Sources.Select(ToSourceView).ToList(),
            Claims = ToClaimViews(detail.Claims, detail.Sources)
        };
    }

    public async Task<SimpleEntityView?> GetInstitutionAsync(EntityId id, CancellationToken cancellationToken = default) =>
        await GetSimpleEntityAsync<Institution>(id, cancellationToken);

    public async Task<SimpleEntityView?> GetTraditionAsync(EntityId id, CancellationToken cancellationToken = default) =>
        await GetSimpleEntityAsync<Tradition>(id, cancellationToken);

    public async Task<Paginated<EntityRef>> ListSimpleEntitiesAsync(
        EntityType type, Paging paging, CancellationToken cancellationToken = default)
    {
        var pageSize = Paginator.ClampLimit(paging.Limit);
        var after = ParseCursor(paging.After);
        var entities = await _db.Entities.AsNoTracking()
            .Where(e => e.Type == type && (after == null || e.Id.Value > after.Value.Value))
            .OrderBy(e => e.Id.Value)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);
        var names = await NamesForAsync(entities.Select(e => e.Id), cancellationToken);
        var rows = entities.Take(pageSize).Select(e => ToEntityRef(e, names)).ToList();
        return Page(rows, entities.Count > pageSize, pageSize);
    }

    public async Task<ClaimView?> GetClaimAsync(EntityId id, CancellationToken cancellationToken = default)
    {
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == id && c.Status == ClaimStatus.Published, cancellationToken);
        if (claim is null)
        {
            return null;
        }

        var sources = await _db.Sources.Where(s => claim.SourceIds.Contains(s.Id)).ToListAsync(cancellationToken);
        if (sources.Count != claim.SourceIds.Count)
        {
            return null;
        }

        return ToClaimViews(new[] { claim }, sources.Select(ToSearchSourceView).ToList()).Single();
    }

    public async Task<Paginated<ClaimView>> ListClaimsAsync(
        EntityId? subjectId, Paging paging, CancellationToken cancellationToken = default)
    {
        var pageSize = Paginator.ClampLimit(paging.Limit);
        var after = ParseCursor(paging.After);
        var claims = await _db.Claims.AsNoTracking()
            .Where(c => c.Status == ClaimStatus.Published && (subjectId == null || c.SubjectEntityId == subjectId))
            .Where(c => after == null || c.Id.Value > after.Value.Value)
            .OrderBy(c => c.Id.Value)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);
        var sourceIds = claims.SelectMany(c => c.SourceIds).Distinct().ToList();
        var sources = sourceIds.Count == 0
            ? new List<Source>()
            : await _db.Sources.Where(s => sourceIds.Contains(s.Id)).ToListAsync(cancellationToken);
        var sourceLookup = sources.Select(s => s.Id).ToHashSet();
        var rows = claims
            .Where(c => c.SourceIds.All(sourceLookup.Contains))
            .SelectMany(c => ToClaimViews(new[] { c }, sources.Select(ToSearchSourceView).ToList()))
            .ToList();
        return Page(rows.Take(pageSize).ToList(), rows.Count > pageSize, pageSize);
    }

    public async Task<Models.SourceView?> GetSourceAsync(EntityId id, CancellationToken cancellationToken = default)
    {
        var source = await _db.Sources.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return source is null ? null : ToSourceView(ToSearchSourceView(source));
    }

    public async Task<Paginated<EntityRef>> ListPersonsAsync(PersonQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Paginator.ClampLimit(query.Paging.Limit);
        var after = ParseCursor(query.Paging.After);
        var persons = await _db.Entities.OfType<Person>().AsNoTracking()
            .Where(p => (after == null || p.Id.Value > after.Value.Value) &&
                (string.IsNullOrWhiteSpace(query.Name) || _db.EntityNames.Any(n => n.EntityId == p.Id && EF.Functions.ILike(n.Value, $"%{query.Name!.Trim()}%"))))
            .OrderBy(p => p.Id.Value).Take(pageSize + 1).ToListAsync(cancellationToken);
        var names = await NamesForAsync(persons.Select(p => p.Id), cancellationToken);

        var rows = persons.Take(pageSize).Where(p => MatchesName(p, names, query.Name)).Select(p => ToEntityRef(p, names)).ToList();

        return Page(rows, persons.Count > pageSize, pageSize);
    }

    public async Task<Paginated<EntityRef>> ListPlacesAsync(PlaceQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Paginator.ClampLimit(query.Paging.Limit);
        var after = ParseCursor(query.Paging.After);
        var placesQuery = _db.Entities.OfType<Place>().AsNoTracking()
            .Where(p => (after == null || p.Id.Value > after.Value.Value));
        if (!string.IsNullOrWhiteSpace(query.Region)) placesQuery = placesQuery.Where(p => p.Region == query.Region);
        if (query.Kind is { } kind) placesQuery = placesQuery.Where(p => p.Kind == kind);
        var places = await placesQuery.OrderBy(p => p.Id.Value).Take(pageSize + 1).ToListAsync(cancellationToken);

        var names = await NamesForAsync(places.Select(p => p.Id), cancellationToken);
        var rows = places.Take(pageSize).Select(p => ToEntityRef(p, names)).ToList();
        return Page(rows, places.Count > pageSize, pageSize);
    }

    public async Task<Paginated<EntityRef>> ListTextsAsync(TextQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Paginator.ClampLimit(query.Paging.Limit);
        var after = ParseCursor(query.Paging.After);
        var texts = await _db.Entities.OfType<Text>().AsNoTracking()
            .Where(t => (after == null || t.Id.Value > after.Value.Value) &&
                (string.IsNullOrWhiteSpace(query.Language) || t.OriginalLanguage == query.Language))
            .OrderBy(t => t.Id.Value).Take(pageSize + 1).ToListAsync(cancellationToken);
        var names = await NamesForAsync(texts.Select(t => t.Id), cancellationToken);

        var rows = texts.Take(pageSize).Where(t => MatchesLanguage(t, names, query.Language)).Select(t => ToEntityRef(t, names)).ToList();

        return Page(rows, texts.Count > pageSize, pageSize);
    }

    public async Task<Paginated<EventSummaryView>> ListEventsAsync(EventQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Paginator.ClampLimit(query.Paging.Limit);
        var after = ParseCursor(query.Paging.After);
        var eventsQuery = _db.Entities.OfType<Event>().AsNoTracking()
            .Where(e => after == null || e.Id.Value > after.Value.Value);
        if (!string.IsNullOrWhiteSpace(query.Region)) eventsQuery = eventsQuery.Where(e => e.Region == query.Region);
        if (!string.IsNullOrWhiteSpace(query.Category)) eventsQuery = eventsQuery.Where(e => e.Category == query.Category);
        if (query.PlaceId is { } placeId) eventsQuery = eventsQuery.Where(e => e.PlaceId == placeId);
        if (query.FromYear is { } from) eventsQuery = eventsQuery.Where(e => e.When == null || e.When.NormalizedUpperBound == null || e.When.NormalizedUpperBound >= from);
        if (query.ToYear is { } to) eventsQuery = eventsQuery.Where(e => e.When == null || e.When.NormalizedLowerBound == null || e.When.NormalizedLowerBound <= to);
        var events = await eventsQuery.OrderBy(e => e.Id.Value).Take(pageSize + 1).ToListAsync(cancellationToken);
        var names = await NamesForAsync(events.Select(e => e.Id), cancellationToken);

        var rows = events.Take(pageSize)
            .Where(e => PassesEventFilters(e, query))
            .Select(e => new EventSummaryView
            {
                Id = e.Id.ToString(),
                Type = EntityType.Event.ToString(),
                CanonicalName = EntityNameReadModel.CanonicalName(names.GetValueOrDefault(e.Id) ?? Array.Empty<EntityName>()) ?? e.Id.ToString(),
                When = DateView.From(e.When),
                Region = e.Region,
                Category = e.Category,
                Certainty = e.Certainty.ToString()
            })
            .ToList();

        return Page(rows, events.Count > pageSize, pageSize);
    }

    public async Task<Paginated<RelationshipView>> ListRelationshipsAsync(
        RelationshipQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Paginator.ClampLimit(query.Paging.Limit);
        var after = ParseCursor(query.Paging.After);
        var relationshipQuery = _db.Relationships.AsNoTracking().Where(r => after == null || r.Id.Value > after.Value.Value);
        if (query.From is { } from) relationshipQuery = relationshipQuery.Where(r => r.FromEntityId == from);
        if (query.To is { } to) relationshipQuery = relationshipQuery.Where(r => r.ToEntityId == to);
        if (!string.IsNullOrWhiteSpace(query.Type)) relationshipQuery = relationshipQuery.Where(r => r.Type == query.Type);
        if (query.MinCertainty is { } min) relationshipQuery = relationshipQuery.Where(r => r.Certainty >= min);
        var relationships = await relationshipQuery.OrderBy(r => r.Id.Value).Take(pageSize + 1).ToListAsync(cancellationToken);

        var endpoints = await BuildEndpointLookupAsync(
            relationships.SelectMany(r => new[] { r.FromEntityId, r.ToEntityId }).Distinct(),
            cancellationToken);

        var rows = relationships
            .Select(r => new RelationshipView
            {
                Id = r.Id.ToString(),
                From = endpoints[r.FromEntityId],
                To = endpoints[r.ToEntityId],
                Type = r.Type,
                Certainty = r.Certainty.ToString(),
                SourceIds = r.SourceIds.Select(s => s.ToString()).ToList()
            })
            .ToList();

        return Page(rows, relationships.Count > pageSize, pageSize);
    }

    public async Task<ExportSnapshot> BuildExportAsync(
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken = default)
    {
        var entities = await _db.Entities.ToListAsync(cancellationToken);
        var names = await _db.EntityNames.ToListAsync(cancellationToken);
        var relationships = await _db.Relationships.ToListAsync(cancellationToken);
        var sources = await _db.Sources.ToListAsync(cancellationToken);
        var claims = await _db.Claims.ToListAsync(cancellationToken);

        return BulkExporter.Build(entities, names, relationships, sources, generatedAt, claims);
    }

    public ApiMetaView GetMeta() => ApiMeta.Describe();

    private static EntityId? ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        return EntityId.TryParse(cursor, out var id)
            ? id
            : throw ApiException.BadRequest("The pagination cursor must be a stable entity id.");
    }

    private static Paginated<T> Page<T>(IReadOnlyList<T> items, bool hasMore, int limit)
    {
        return new Paginated<T>
        {
            Items = items,
            HasMore = hasMore,
            NextCursor = hasMore && items.Count > 0 ? GetId(items[^1]) : null,
            Limit = limit
        };
    }

    private static string GetId<T>(T item) => item switch
    {
        EntityRef entity => entity.Id,
        RelationshipView relationship => relationship.Id,
        ClaimView claim => claim.Id,
        _ => throw new InvalidOperationException($"No cursor projection exists for {typeof(T).Name}.")
    };

    private bool PassesEventFilters(Event e, EventQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Region) &&
            !string.Equals(e.Region, q.Region, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(q.Category) &&
            !string.Equals(e.Category, q.Category, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (q.PlaceId is { } pid && e.PlaceId != pid)
        {
            return false;
        }

        if ((q.FromYear is not null || q.ToYear is not null) && !Overlaps(e.When, q.FromYear, q.ToYear))
        {
            return false;
        }

        return true;
    }

    private static bool Overlaps(HistoricalDate? when, int? from, int? to)
    {
        if (when is null)
        {
            return false;
        }

        var lower = when.NormalizedLowerBound;
        var upper = when.NormalizedUpperBound;
        if (lower is null && upper is null)
        {
            return false;
        }

        if (from is not null && (upper is null || upper.Value < from.Value))
        {
            return false;
        }

        if (to is not null && (lower is null || lower.Value > to.Value))
        {
            return false;
        }

        return true;
    }

    private static bool MatchesName(Entity entity, IReadOnlyDictionary<EntityId, IReadOnlyList<EntityName>> names, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return true;
        }

        var canonical = EntityNameReadModel.CanonicalName(names.GetValueOrDefault(entity.Id) ?? Array.Empty<EntityName>()) ?? entity.Id.ToString();
        return canonical.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesLanguage(
        Entity entity, IReadOnlyDictionary<EntityId, IReadOnlyList<EntityName>> names, string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return true;
        }

        var ns = names.GetValueOrDefault(entity.Id);
        return ns is not null && ns.Any(n => string.Equals(n.Language, language.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> CanonicalNameAsync(EntityId id, CancellationToken cancellationToken)
    {
        var names = await _db.EntityNames.Where(n => n.EntityId == id).ToListAsync(cancellationToken);
        return EntityNameReadModel.CanonicalName(names) ?? id.ToString();
    }

    private async Task<IReadOnlyDictionary<EntityId, IReadOnlyList<EntityName>>> NamesForAsync(
        IEnumerable<EntityId> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<EntityId, IReadOnlyList<EntityName>>();
        }

        var names = await _db.EntityNames.Where(n => idList.Contains(n.EntityId)).ToListAsync(cancellationToken);
        return names.GroupBy(n => n.EntityId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EntityName>)EntityNameReadModel.Order(g));
    }

    private async Task<IReadOnlyDictionary<EntityId, EntityRef>> BuildEndpointLookupAsync(
        IEnumerable<EntityId> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<EntityId, EntityRef>();
        }

        var entities = await _db.Entities.Where(e => idList.Contains(e.Id)).ToListAsync(cancellationToken);
        var names = await NamesForAsync(idList, cancellationToken);
        return entities.ToDictionary(e => e.Id, e => ToEntityRef(e, names));
    }

    private static EntityRef ToEntityRef(Entity entity, IReadOnlyDictionary<EntityId, IReadOnlyList<EntityName>> names) => new()
    {
        Id = entity.Id.ToString(),
        Type = entity.Type.ToString(),
        CanonicalName = EntityNameReadModel.CanonicalName(names.GetValueOrDefault(entity.Id) ?? Array.Empty<EntityName>()) ?? entity.Id.ToString()
    };

    private static IReadOnlyList<Models.NameView> MapNames(IReadOnlyList<Search.Models.NameView> names) =>
        names.Select(n => new Models.NameView
        {
            Value = n.Value,
            Language = n.Language,
            Script = n.Script,
            Romanization = n.Romanization,
            IsPrimary = n.IsPrimary
        }).ToList();

    private static RelatedRef ToRelatedRef(RelatedEntity r) => new()
    {
        Id = r.Id.ToString(),
        Type = r.Type.ToString(),
        CanonicalName = r.Name,
        RelationType = r.RelationType ?? string.Empty,
        Direction = r.Direction.ToString().ToLowerInvariant(),
        Certainty = r.Certainty.ToString(),
        SourceIds = r.SourceIds.Select(s => s.ToString()).ToList()
    };

    private static Models.SourceView ToSourceView(Search.Models.SourceView s) => new()
    {
        Id = s.Id.ToString(),
        Title = s.Title,
        Author = s.Author,
        Date = s.Date,
        Identifier = s.Identifier,
        Tier = s.Tier
    };

    private static IReadOnlyList<Models.ClaimView> ToClaimViews(
        IReadOnlyList<Claim> claims,
        IReadOnlyList<Search.Models.SourceView> sources)
    {
        var sourceLookup = sources.ToDictionary(s => s.Id);
        return claims.Select(claim => new Models.ClaimView
        {
            Id = claim.Id.ToString(),
            SubjectEntityId = claim.SubjectEntityId?.ToString(),
            Statement = claim.Statement,
            Certainty = claim.Certainty.ToString(),
            Interpretation = claim.Interpretation.ToString(),
            SourceLocator = claim.SourceLocator,
            Sources = claim.SourceIds
                .Where(sourceLookup.ContainsKey)
                .Select(sourceId => ToSourceView(sourceLookup[sourceId]))
                .ToList()
        }).ToList();
    }

    private async Task<SimpleEntityView?> GetSimpleEntityAsync<TEntity>(
        EntityId id, CancellationToken cancellationToken)
        where TEntity : Entity
    {
        var entity = await _db.Entities.OfType<TEntity>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var detail = await _details.GetAsync(id, cancellationToken);
        return new SimpleEntityView
        {
            Id = entity.Id.ToString(),
            Type = entity.Type.ToString(),
            CanonicalName = detail!.CanonicalName,
            Names = detail.Names.Select(n => new Models.NameView
            {
                Value = n.Value,
                Language = n.Language,
                Script = n.Script,
                Romanization = n.Romanization,
                IsPrimary = n.IsPrimary
            }).ToList(),
            Summary = entity.Summary,
            Region = detail.Region,
            Certainty = detail.Certainty.ToString(),
            Sources = detail.Sources.Select(ToSourceView).ToList(),
            Claims = ToClaimViews(detail.Claims, detail.Sources)
        };
    }

    private static Search.Models.SourceView ToSearchSourceView(Source s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Author = s.Author,
        Date = s.Date,
        Identifier = s.Identifier,
        Tier = s.Tier?.ToString()
    };

}
