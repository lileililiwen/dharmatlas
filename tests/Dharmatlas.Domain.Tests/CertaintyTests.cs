using Dharmatlas.Domain.ValueObjects;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class CertaintyTests
{
    [Theory]
    [InlineData("Documented", Certainty.Documented)]
    [InlineData("probable", Certainty.Probable)]
    [InlineData("Traditional Account", Certainty.TraditionalAccount)]
    [InlineData("disputed", Certainty.Disputed)]
    [InlineData("UNKNOWN", Certainty.Unknown)]
    public void Supported_values_parse_case_insensitively(string input, Certainty expected)
    {
        Assert.Equal(expected, CertaintyParser.Parse(input));
        Assert.True(CertaintyParser.IsSupported(input));
    }

    [Theory]
    [InlineData("maybe")]
    [InlineData("confirmed")]
    [InlineData("")]
    public void Unsupported_values_are_rejected(string input)
    {
        Assert.Throws<DomainValidationException>(() => CertaintyParser.Parse(input));
        Assert.False(CertaintyParser.IsSupported(input));
    }
}
