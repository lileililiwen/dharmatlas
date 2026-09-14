using Dharmatlas.Host.Web;

namespace Dharmatlas.Domain.Tests;

public sealed class ProductizationTests
{
    [Fact]
    public void Entity_paths_parse_with_api_parity()
    {
        Assert.True(ProductizationEndpoints.TryParseEntityPath("/persons/abc", out var plural, out var id));
        Assert.Equal("persons", plural);
        Assert.Equal("abc", id);
        Assert.True(ProductizationEndpoints.TryParseEntityPath("/places/bodh-gaya", out _, out _));
        Assert.False(ProductizationEndpoints.TryParseEntityPath("/api/v1/persons/abc", out _, out _));
        Assert.False(ProductizationEndpoints.TryParseEntityPath("/drafts/abc", out _, out _));
    }

    [Fact]
    public void Sitemap_contains_only_supplied_published_routes()
    {
        var xml = SitemapBuilder.BuildXml(new[] { "/persons/a", "/places/b" }, "https://atlas.example");
        Assert.Contains("https://atlas.example/persons/a", xml, StringComparison.Ordinal);
        Assert.Contains("https://atlas.example/places/b", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("draft", xml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Meta_injection_sets_title_description_canonical_and_og()
    {
        const string shell = "<html><head><title>Old</title></head><body></body></html>";
        var injected = MetaInjector.Inject(shell, "Ashoka — Person | Dharmatlas", "A record.", "https://atlas.example/persons/x");
        Assert.Contains("Ashoka", injected, StringComparison.Ordinal);
        Assert.Contains("rel=\"canonical\"", injected, StringComparison.Ordinal);
        Assert.Contains("og:title", injected, StringComparison.Ordinal);
        Assert.Contains("https://atlas.example/persons/x", injected, StringComparison.Ordinal);
    }
}
