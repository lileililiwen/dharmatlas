using System.Net;
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

    public sealed class TestHostFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Dharmatlas", "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test");
        }
    }
}
