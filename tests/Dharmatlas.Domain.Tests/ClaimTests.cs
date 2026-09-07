using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class ClaimTests
{
    private static readonly EntityId SourceId = EntityId.New();

    [Fact]
    public void Draft_claim_may_be_unsourced()
    {
        var draft = Claim.Draft("The council was held at Pataliputra", Certainty.TraditionalAccount);

        Assert.Equal(ClaimStatus.Draft, draft.Status);
        Assert.Empty(draft.SourceIds);
    }

    [Fact]
    public void Published_claim_requires_at_least_one_source()
    {
        Assert.Throws<DomainValidationException>(() =>
            Claim.Publish("The council was held at Pataliputra", Certainty.Documented,
                Array.Empty<EntityId>()));
    }

    [Fact]
    public void Published_claim_carries_sources_and_certainty()
    {
        var claim = Claim.Publish("The council was held at Pataliputra", Certainty.Documented,
            new[] { SourceId });

        Assert.Equal(ClaimStatus.Published, claim.Status);
        Assert.Single(claim.SourceIds);
    }

    [Fact]
    public void Claim_referencing_unknown_source_is_rejected()
    {
        var claim = Claim.Publish("A disputed assertion", Certainty.Disputed, new[] { SourceId });

        Assert.Throws<InvalidReferenceException>(() =>
            claim.ValidateReferences(new HashSet<EntityId>()));
    }

    [Fact]
    public void Claim_with_known_source_passes_reference_validation()
    {
        var claim = Claim.Publish("A documented assertion", Certainty.Documented, new[] { SourceId });

        claim.ValidateReferences(new HashSet<EntityId> { SourceId });
    }
}
