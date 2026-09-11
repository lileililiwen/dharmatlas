using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Import;

public sealed record SeedImportResult(int Sources, int Entities, int Names, int Claims, int Relationships);

public static class SeedImporter
{
    public static async Task<SeedImportResult> ImportAsync(
        DharmatlasDbContext db,
        SeedManifest manifest,
        CancellationToken cancellationToken = default)
    {
        var validation = SeedManifestValidator.Validate(manifest);
        if (!validation.IsValid) throw new SeedManifestException("Seed manifest validation failed:\n" + string.Join('\n', validation.Errors));

        var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var entityIds = manifest.Entities.Select(x => EntityId.From(Guid.Parse(x.Id))).ToList();
            var sourceIds = manifest.Sources.Select(x => EntityId.From(Guid.Parse(x.Id))).ToList();
            var nameIds = manifest.Names.Select(x => EntityId.From(Guid.Parse(x.Id))).ToList();
            var claimIds = manifest.Claims.Select(x => EntityId.From(Guid.Parse(x.Id))).ToList();
            var relationshipIds = manifest.Relationships.Select(x => EntityId.From(Guid.Parse(x.Id))).ToList();

            db.EntityNames.RemoveRange(await db.EntityNames.Where(x => nameIds.Contains(x.Id)).ToListAsync(cancellationToken));
            db.Claims.RemoveRange(await db.Claims.Where(x => claimIds.Contains(x.Id)).ToListAsync(cancellationToken));
            db.Relationships.RemoveRange(await db.Relationships.Where(x => relationshipIds.Contains(x.Id)).ToListAsync(cancellationToken));
            db.Entities.RemoveRange(await db.Entities.Where(x => entityIds.Contains(x.Id)).ToListAsync(cancellationToken));
            db.Sources.RemoveRange(await db.Sources.Where(x => sourceIds.Contains(x.Id)).ToListAsync(cancellationToken));
            await db.SaveChangesAsync(cancellationToken);

            db.Sources.AddRange(manifest.Sources.Select(ToSource));
            db.Entities.AddRange(manifest.Entities.Select(ToEntity));
            db.EntityNames.AddRange(manifest.Names.Select(ToName));
            db.Claims.AddRange(manifest.Claims.Select(ToClaim));
            db.Relationships.AddRange(manifest.Relationships.Select(ToRelationship));
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new SeedImportResult(manifest.Sources.Count, manifest.Entities.Count, manifest.Names.Count, manifest.Claims.Count, manifest.Relationships.Count);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static Source ToSource(SeedSource source) => new(source.Title)
    {
        Id = EntityId.From(Guid.Parse(source.Id)), Author = source.Author, Date = source.Date,
        PublisherOrCollection = source.PublisherOrCollection, Identifier = source.Identifier
    };

    private static EntityName ToName(SeedName name) => new(
        EntityId.From(Guid.Parse(name.Id)), EntityId.From(Guid.Parse(name.EntityId)), name.Language,
        name.Script, name.Romanization, name.Value, name.IsPrimary);

    private static Entity ToEntity(SeedEntity seed)
    {
        var id = EntityId.From(Guid.Parse(seed.Id));
        var certainty = seed.Certainty is null ? Certainty.Unknown : CertaintyParser.Parse(seed.Certainty);
        return seed.Type.ToLowerInvariant() switch
        {
            "person" => new Person { Id = id, Summary = seed.Summary },
            "place" => new Place { Id = id, Summary = seed.Summary, Region = seed.Region, ModernName = seed.ModernName, Kind = Enum.Parse<PlaceKind>(seed.Kind ?? nameof(PlaceKind.Other), true), Latitude = seed.Latitude, Longitude = seed.Longitude, Activity = ToDate(seed.Activity), Certainty = certainty, SourceIds = Ids(seed.SourceIds) },
            "institution" => new Institution { Id = id, Summary = seed.Summary, InstitutionalForm = seed.InstitutionalForm, PlaceId = seed.PlaceId is null ? null : EntityId.From(Guid.Parse(seed.PlaceId)), Activity = ToDate(seed.Activity), Certainty = certainty, SourceIds = Ids(seed.SourceIds) },
            "text" => new Text { Id = id, Summary = seed.Summary, OriginalLanguage = seed.OriginalLanguage },
            "tradition" => new Tradition { Id = id, Summary = seed.Summary, Region = seed.Region },
            "event" => new Event { Id = id, Summary = seed.Summary, Region = seed.Region, Category = seed.Category, PlaceId = seed.PlaceId is null ? null : EntityId.From(Guid.Parse(seed.PlaceId)), When = ToDate(seed.Date), Certainty = certainty },
            _ => throw new SeedManifestException($"Unsupported entity type '{seed.Type}'.")
        };
    }

    private static Claim ToClaim(SeedClaim claim) => Claim.Publish(
        claim.Statement, CertaintyParser.Parse(claim.Certainty), Ids(claim.SourceIds), EntityId.From(Guid.Parse(claim.SubjectEntityId))) with { Id = EntityId.From(Guid.Parse(claim.Id)) };

    private static Relationship ToRelationship(SeedRelationship relationship) => new Relationship(
        EntityId.From(Guid.Parse(relationship.FromEntityId)), EntityId.From(Guid.Parse(relationship.ToEntityId)), relationship.Type,
        CertaintyParser.Parse(relationship.Certainty), Ids(relationship.SourceIds)) with { Id = EntityId.From(Guid.Parse(relationship.Id)) };

    private static HistoricalDate? ToDate(SeedDate? date)
    {
        if (date is null) return null;
        var kind = Enum.Parse<HistoricalDateKind>(date.Kind, true);
        return kind switch
        {
            HistoricalDateKind.Exact => HistoricalDate.Exact(date.DisplayExpression, date.Lower ?? throw new SeedManifestException("Exact dates require lower.")),
            HistoricalDateKind.Approximate => HistoricalDate.Approximate(date.DisplayExpression, date.Lower, date.Upper),
            HistoricalDateKind.Century => HistoricalDate.Century(date.DisplayExpression, date.Lower ?? throw new SeedManifestException("Century dates require lower."), date.Upper ?? throw new SeedManifestException("Century dates require upper.")),
            HistoricalDateKind.Interval => HistoricalDate.Interval(date.DisplayExpression, date.Lower!.Value, date.Upper!.Value),
            HistoricalDateKind.OpenInterval => HistoricalDate.OpenInterval(date.DisplayExpression, date.Lower, date.Upper),
            HistoricalDateKind.Traditional => HistoricalDate.Traditional(date.DisplayExpression, date.Lower, date.Upper),
            _ => throw new SeedManifestException($"Unsupported date kind '{date.Kind}'.")
        };
    }

    private static IReadOnlyList<EntityId> Ids(IEnumerable<string> ids) => ids.Select(id => EntityId.From(Guid.Parse(id))).ToList();
}
