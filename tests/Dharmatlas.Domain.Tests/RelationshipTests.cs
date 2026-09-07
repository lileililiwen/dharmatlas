using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class RelationshipTests
{
    private static readonly EntityId A = EntityId.New();
    private static readonly EntityId B = EntityId.New();
    private static readonly EntityId SourceId = EntityId.New();

    [Fact]
    public void Self_reference_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Relationship(A, A, "founded", Certainty.Documented, new[] { SourceId }));
    }

    [Fact]
    public void Relationship_requires_at_least_one_source()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Relationship(A, B, "authored", Certainty.Documented, Array.Empty<EntityId>()));
    }

    [Fact]
    public void Relationship_with_unknown_source_is_rejected_on_validation()
    {
        var relationship = new Relationship(A, B, "authored", Certainty.Documented, new[] { SourceId });

        Assert.Throws<InvalidReferenceException>(() =>
            relationship.ValidateReferences(
                new HashSet<EntityId> { A, B },
                new HashSet<EntityId>()));
    }

    [Fact]
    public void Valid_relationship_passes_reference_validation()
    {
        var relationship = new Relationship(A, B, "authored", Certainty.Documented, new[] { SourceId });

        relationship.ValidateReferences(
            new HashSet<EntityId> { A, B },
            new HashSet<EntityId> { SourceId });
    }
}
