using Dharmatlas.Domain.Entities;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class EntityNameTests
{
    [Fact]
    public void Adding_an_alias_preserves_stable_identity()
    {
        var person = new Person();
        var originalId = person.Id;

        person
            .AddName(new EntityName(person.Id, "san", "devanagari", "IAST", "Buddha"))
            .AddName(new EntityName(person.Id, "chn", "han", "Pinyin", "Fó"));

        Assert.Equal(originalId, person.Id);
        Assert.Equal(2, person.Names.Count);
    }

    [Fact]
    public void Only_one_primary_name_per_language_is_allowed()
    {
        var person = new Person();

        person.AddName(new EntityName(person.Id, "eng", "latin", "Wylie", "Buddha", isPrimary: true));

        Assert.Throws<DomainValidationException>(() =>
            person.AddName(new EntityName(person.Id, "eng", "latin", "Wylie", "The Awakened One", isPrimary: true)));
    }

    [Fact]
    public void Empty_name_value_is_rejected()
    {
        var person = new Person();

        Assert.Throws<DomainValidationException>(() =>
            person.AddName(new EntityName(person.Id, "eng", "latin", "Wylie", "   ")));
    }
}
