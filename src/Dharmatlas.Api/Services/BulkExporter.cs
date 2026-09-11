using System.Diagnostics.CodeAnalysis;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Api.Models;

namespace Dharmatlas.Api.Services;

/// <summary>
/// Pure builder that assembles a reproducible <see cref="ExportSnapshot"/> from the
/// published data model. It reads only the published tables (entities, names,
/// relationships, sources); it never touches submissions, contributors, or
/// revisions, so drafts, rejected contributions, and private contributor data are
/// structurally excluded. Output is ordered by stable id so the same dataset
/// always serializes identically.
/// </summary>
public static class BulkExporter
{
    public static ExportSnapshot Build(
        IReadOnlyList<Entity> entities,
        IReadOnlyList<EntityName> names,
        IReadOnlyList<Relationship> relationships,
        IReadOnlyList<Source> sources,
        DateTimeOffset generatedAt,
        string schemaVersion = ApiConstants.SchemaVersion,
        string license = ApiConstants.License,
        string licenseUrl = ApiConstants.LicenseUrl)
    {
        var namesByEntity = names
            .GroupBy(n => n.EntityId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EntityName>)EntityNameReadModel.Order(g));

        var placeRegions = entities
            .OfType<Place>()
            .Where(p => p.Region is not null)
            .ToDictionary(p => p.Id, p => p.Region!);

        var exportEntities = entities
            .OrderBy(e => e.Id.ToString())
            .Select(e => ToExportEntity(e, namesByEntity, placeRegions))
            .ToList();

        var exportRelationships = relationships
            .OrderBy(r => r.Id.ToString())
            .Select(ToExportRelationship)
            .ToList();

        var exportSources = sources
            .OrderBy(s => s.Id.ToString())
            .Select(ToExportSource)
            .ToList();

        var byType = exportEntities
            .GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ExportSnapshot
        {
            SchemaVersion = schemaVersion,
            License = license,
            LicenseUrl = licenseUrl,
            GeneratedAt = generatedAt,
            Index = new ExportIndex
            {
                Entities = exportEntities.Count,
                Relationships = exportRelationships.Count,
                Sources = exportSources.Count,
                ByType = byType
            },
            Entities = exportEntities,
            Relationships = exportRelationships,
            Sources = exportSources
        };
    }

    private static ExportEntity ToExportEntity(
        Entity entity,
        IReadOnlyDictionary<EntityId, IReadOnlyList<EntityName>> namesByEntity,
        IReadOnlyDictionary<EntityId, string> placeRegions)
    {
        namesByEntity.TryGetValue(entity.Id, out var names);
        names ??= Array.Empty<EntityName>();

        var canonical = EntityNameReadModel.CanonicalName(names) ?? entity.Id.ToString();
        var nameViews = names
            .Select(n => new NameView
            {
                Value = n.Value,
                Language = n.Language,
                Script = n.Script,
                Romanization = n.Romanization,
                IsPrimary = n.IsPrimary
            })
            .ToList();

        var sourceIds = entity switch
        {
            Place pl => pl.SourceIds,
            Institution inst => inst.SourceIds,
            _ => Array.Empty<EntityId>()
        };

        return new ExportEntity
        {
            Id = entity.Id.ToString(),
            Type = entity.Type.ToString(),
            CanonicalName = canonical,
            Names = nameViews,
            Summary = entity.Summary,
            Region = ResolveRegion(entity, placeRegions),
            Certainty = CertaintyOf(entity),
            Activity = ActivityOf(entity),
            When = entity is Event ev ? DateView.From(ev.When) : null,
            Category = entity is Event ev2 ? ev2.Category : null,
            Latitude = entity is Place p ? p.Latitude : null,
            Longitude = entity is Place p2 ? p2.Longitude : null,
            ModernName = entity is Place p3 ? p3.ModernName : null,
            Kind = entity is Place p4 ? p4.Kind.ToString() : null,
            OriginalLanguage = entity is Text t ? t.OriginalLanguage : null,
            SourceIds = sourceIds.Select(s => s.ToString()).ToList()
        };
    }

    private static ExportRelationship ToExportRelationship(Relationship r) => new()
    {
        Id = r.Id.ToString(),
        From = r.FromEntityId.ToString(),
        To = r.ToEntityId.ToString(),
        Type = r.Type,
        Certainty = r.Certainty.ToString(),
        SourceIds = r.SourceIds.Select(s => s.ToString()).ToList()
    };

    private static ExportSource ToExportSource(Source s) => new()
    {
        Id = s.Id.ToString(),
        Title = s.Title,
        Author = s.Author,
        Date = s.Date,
        Identifier = s.Identifier
    };

    private static string? ResolveRegion(Entity entity, IReadOnlyDictionary<EntityId, string> placeRegions) => entity switch
    {
        Place p => p.Region,
        Tradition t => t.Region,
        Institution i when i.PlaceId is { } pid && placeRegions.TryGetValue(pid, out var r) => r,
        _ => null
    };

    private static string? CertaintyOf(Entity entity) => entity switch
    {
        Place p => p.Certainty.ToString(),
        Institution i => i.Certainty.ToString(),
        _ => null
    };

    private static DateView? ActivityOf(Entity entity) => entity switch
    {
        Place p => DateView.From(p.Activity),
        Institution i => DateView.From(i.Activity),
        _ => null
    };

}
