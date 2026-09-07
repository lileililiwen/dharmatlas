using System.Diagnostics.CodeAnalysis;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Dharmatlas.Search.Engine;
using Dharmatlas.Search.Models;
using Dharmatlas.Search.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dharmatlas.Domain.Tests;

[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public class EntityDetailTests
{
    private static EntityName Name(EntityId owner, string value, bool primary = false, string script = "latin") =>
        new(owner, "eng", script, string.Empty, value, primary);

    [Fact]
    public void Detail_surfaces_names_relationships_and_sources()
    {
        var personId = EntityId.New();
        var teacherId = EntityId.New();
        var influencerId = EntityId.New();
        var textId = EntityId.New();
        var sourceId = EntityId.New();

        var person = new Person { Id = personId };
        var names = new List<EntityName>
        {
            Name(personId, "Xuanzang", primary: true, script: "han"),
            Name(personId, "Hsuan-tsang", primary: false)
        };
        var relationships = new List<Relationship>
        {
            new(personId, teacherId, "teacher-of", Certainty.Documented, new[] { sourceId }),
            new(personId, textId, "authored", Certainty.TraditionalAccount, new[] { sourceId }),
            new(influencerId, personId, "influenced", Certainty.Probable, new[] { sourceId })
        };
        var sources = new List<SourceView>
        {
            new() { Id = sourceId, Title = "Records of the Western Regions" }
        };
        var lookup = new Dictionary<EntityId, EntityDetailAssembler.RelatedInfo>
        {
            [teacherId] = new(EntityType.Person, "Śīlabhadra"),
            [influencerId] = new(EntityType.Person, "Buddhaghosa"),
            [textId] = new(EntityType.Text, "Great Tang Records")
        };

        var detail = EntityDetailAssembler.Build(person, names, relationships, sources, lookup, region: null, activePeriod: null);

        Assert.Equal("Xuanzang", detail.CanonicalName);
        Assert.Equal(2, detail.Names.Count);
        Assert.Equal(3, detail.Relationships.Count);

        var teacher = Assert.Single(detail.RelatedPeople, r => r.Id == teacherId);
        Assert.Equal(RelationshipDirection.Outgoing, teacher.Direction);
        Assert.Equal("teacher-of", teacher.RelationType);

        var influencer = Assert.Single(detail.RelatedPeople, r => r.Id == influencerId);
        Assert.Equal(RelationshipDirection.Incoming, influencer.Direction);

        Assert.Single(detail.RelatedTexts, r => r.Id == textId);
        Assert.Single(detail.Sources);
    }

    [Fact]
    public void Missing_fields_are_omitted_not_invented()
    {
        var person = new Person();
        var detail = EntityDetailAssembler.Build(
            person,
            Array.Empty<EntityName>(),
            Array.Empty<Relationship>(),
            Array.Empty<SourceView>(),
            new Dictionary<EntityId, EntityDetailAssembler.RelatedInfo>(),
            region: null,
            activePeriod: null);

        Assert.Empty(detail.Relationships);
        Assert.Empty(detail.Sources);
        Assert.Empty(detail.RelatedPeople);
        Assert.Empty(detail.RelatedPlaces);
        Assert.Empty(detail.RelatedTexts);
        Assert.Null(detail.Region);
        Assert.Null(detail.ActivePeriod);
        Assert.Equal(Certainty.Unknown, detail.Certainty);
        Assert.Equal(person.Id.ToString(), detail.CanonicalName);
    }

    [Fact]
    public void Place_certainty_and_active_period_are_surfaced()
    {
        var place = new Place
        {
            Region = "India",
            Certainty = Certainty.Documented,
            Activity = HistoricalDate.Exact("400 CE", 400)
        };
        var detail = EntityDetailAssembler.Build(
            place,
            new[] { Name(place.Id, "Nalanda", primary: true) },
            Array.Empty<Relationship>(),
            Array.Empty<SourceView>(),
            new Dictionary<EntityId, EntityDetailAssembler.RelatedInfo>(),
            region: "India",
            activePeriod: place.Activity.DisplayExpression);

        Assert.Equal(Certainty.Documented, detail.Certainty);
        Assert.Equal("India", detail.Region);
        Assert.Equal("400 CE", detail.ActivePeriod);
    }

    [Fact]
    public async Task Service_resolves_names_and_shows_only_referenced_sources()
    {
        var db = NewDb();
        var personId = EntityId.New();
        var placeId = EntityId.New();
        var sourceId = EntityId.New();
        var unusedSourceId = EntityId.New();

        db.Entities.Add(new Person { Id = personId });
        db.Entities.Add(new Place { Id = placeId, Region = "India" });
        db.EntityNames.Add(Name(personId, "Xuanzang", primary: true));
        db.EntityNames.Add(Name(placeId, "Nalanda", primary: true));
        db.Sources.Add(new Source("Great Tang Records") { Id = sourceId });
        db.Sources.Add(new Source("Unrelated Chronicle") { Id = unusedSourceId });
        db.Relationships.Add(new Relationship(personId, placeId, "visited", Certainty.Documented, new[] { sourceId }));
        await db.SaveChangesAsync();

        var service = new EntityDetailService(db);
        var detail = await service.GetAsync(personId);

        Assert.NotNull(detail);
        Assert.Equal("Xuanzang", detail!.CanonicalName);
        Assert.Single(detail.RelatedPlaces, r => r.Name == "Nalanda");
        Assert.Single(detail.Sources);
        Assert.Equal("Great Tang Records", detail.Sources[0].Title);
        Assert.DoesNotContain(detail.Sources, s => s.Id == unusedSourceId);
    }

    private static DharmatlasDbContext NewDb() =>
        new(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("entity-detail-" + Guid.NewGuid())
            .Options);
}
