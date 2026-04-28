using Microsoft.AspNetCore.Mvc.Testing;

namespace LMS.Api.Tests;

/// <summary>
/// Integration tests for the /health endpoint.
/// <see cref="WebApplicationFactory{TEntryPoint}"/> spins up the full ASP.NET Core
/// pipeline in-memory so these tests exercise middleware, DI, and routing together.
/// </summary>
public class HealthIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_Returns200()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Avoid chasing HTTPS redirects in the test client
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_BodyContainsHealthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }
}
