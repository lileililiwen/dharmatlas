using Dharmatlas.Api.Models;
using Dharmatlas.Api.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Domain.Tests;

public sealed class ProvenanceTests
{
    [Fact]
    public void Published_claim_retains_locator_and_traditional_interpretation()
    {
        var sourceId = EntityId.New();
        var claim = Claim.Publish(
            "The Buddha taught at Vulture Peak.",
            Certainty.TraditionalAccount,
            new[] { sourceId },
            interpretation: ClaimInterpretation.TraditionalAccount,
            sourceLocator: "folio 12r");

        Assert.Equal(ClaimStatus.Published, claim.Status);
        Assert.Equal(ClaimInterpretation.TraditionalAccount, claim.Interpretation);
        Assert.Equal("folio 12r", claim.SourceLocator);
        Assert.Single(ClaimReadModel.Published(new[] { claim }));
    }

    [Fact]
    public void Public_claim_projection_excludes_non_published_claims_and_preserves_conflicts()
    {
        var entityId = EntityId.New();
        var sourceA = new Source("Chronicle A") { Id = EntityId.New() };
        var sourceB = new Source("Chronicle B") { Id = EntityId.New() };
        var claims = new[]
        {
            Claim.Publish("Date: 250 BCE", Certainty.Probable, new[] { sourceA.Id }, entityId),
            Claim.Publish("Date: 260 BCE", Certainty.Disputed, new[] { sourceB.Id }, entityId),
            Claim.Draft("Private editorial note", Certainty.Unknown, subjectEntityId: entityId) with { Status = ClaimStatus.Rejected }
        };

        var published = ClaimReadModel.Published(claims);
        var snapshot = BulkExporter.Build(
            new Entity[] { new Person { Id = entityId } },
            Array.Empty<EntityName>(),
            Array.Empty<Relationship>(),
            new[] { sourceA, sourceB },
            DateTimeOffset.UnixEpoch,
            claims: claims);

        Assert.Equal(2, published.Count);
        Assert.Equal(2, snapshot.Claims.Count);
        Assert.Contains(snapshot.Claims, c => c.Statement == "Date: 250 BCE");
        Assert.Contains(snapshot.Claims, c => c.Statement == "Date: 260 BCE");
    }

    [Fact]
    public async Task Person_api_exposes_published_evidence_and_hides_rejected_claims()
    {
        var person = new Person { Id = EntityId.New() };
        var source = new Source("Inscription catalogue") { Id = EntityId.New() };
        var published = Claim.Publish(
            "The dedication names the monastery.",
            Certainty.Documented,
            new[] { source.Id },
            person.Id,
            ClaimInterpretation.ScholarlyInterpretation,
            "entry 42");
        var rejected = Claim.Draft("Unpublished editorial assertion", Certainty.Unknown, subjectEntityId: person.Id)
            with { Status = ClaimStatus.Rejected };

        await using var db = new DharmatlasDbContext(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        db.Entities.Add(person);
        db.Sources.Add(source);
        db.Claims.AddRange(published, rejected);
        await db.SaveChangesAsync();

        var view = await new ApiQueryService(db).GetPersonAsync(person.Id);

        var evidence = Assert.Single(view!.Claims);
        Assert.Equal("entry 42", evidence.SourceLocator);
        Assert.Equal("ScholarlyInterpretation", evidence.Interpretation);
        Assert.Equal("Inscription catalogue", Assert.Single(evidence.Sources).Title);
    }
}
