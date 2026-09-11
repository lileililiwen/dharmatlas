using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace Dharmatlas.Domain.Tests;

public sealed class HostSmokeTests : IClassFixture<HostSmokeTests.TestHostFactory>
{
    private readonly TestHostFactory _factory;

    public HostSmokeTests(TestHostFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_is_available_without_database_connectivity()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Public_meta_route_is_wired()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/meta");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_database_unavailable()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Search_requires_a_query_before_database_access()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Map_rejects_an_unbounded_or_invalid_year()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/map?year=9999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Contributions_require_authentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/contributions", new
        {
            type = "Event",
            summary = "Unauthenticated",
            payloadJson = "{\"Summary\":\"No\"}",
            sourceIds = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public sealed class TestHostFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Dharmatlas", "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test");
        }
    }
}
