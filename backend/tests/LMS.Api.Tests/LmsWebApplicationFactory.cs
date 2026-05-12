using LMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LMS.Api.Tests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for integration tests.
///
/// What it does:
///   1. Overrides the PostgreSQL DbContext registration with EF Core InMemory so tests
///      run without a live database.
///   2. Sets the environment to "Testing" (not "Development") so the development seed
///      (<c>SeedDatabaseAsync</c>) is skipped entirely — each test starts with an empty
///      store, making tests fully deterministic and self-contained.
///
/// Usage:
///   public class MyTests(LmsWebApplicationFactory factory)
///       : IClassFixture&lt;LmsWebApplicationFactory&gt;
///   {
///       private HttpClient CreateClient() => factory.CreateClient();
///   }
///
/// Notes:
///   - The InMemory database is shared across all tests within one factory instance
///     (the IClassFixture scope).  Tests that write data should use unique identifiers
///     (e.g. distinct email addresses) to avoid state leaking between test cases.
///   - If a test needs a pre-populated database, add a helper that seeds the InMemory
///     context through the factory's service scope:
///
///       using var scope = factory.Services.CreateScope();
///       var db = scope.ServiceProvider.GetRequiredService&lt;LmsDbContext&gt;();
///       db.Grades.Add(Grade.Create("Grade 5", 5));
///       await db.SaveChangesAsync();
/// </summary>
public sealed class LmsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use "Testing" so IsDevelopment() returns false.
        // This prevents SeedDatabaseAsync from running (and trying to call MigrateAsync
        // on the InMemory provider, which would throw).
        builder.UseEnvironment("Testing");

        // Inject test-only configuration values.
        // "Testing" environment does not load appsettings.Development.json, so we must
        // supply the JWT secret (and other dev-only values) explicitly here.
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // 64-char key — well above the 32-char minimum for HS256
                ["Jwt:SecretKey"] = "test-integration-secret-key-at-least-32-characters-long!!",
                ["Jwt:Issuer"]    = "lms-api",
                ["Jwt:Audience"]  = "lms-client",
                ["Jwt:ExpiresInMinutes"] = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContextOptions<LmsDbContext> registered by AddDbContext
            // in DependencyInjection.cs (the Npgsql/PostgreSQL version).
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LmsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Replace with EF Core InMemory — no PostgreSQL required in tests.
            services.AddDbContext<LmsDbContext>(options =>
                options.UseInMemoryDatabase("LmsIntegrationTests"));

            // Ensure JWT bearer uses the test secret for token validation.
            // This overrides whatever was bound from appsettings.json at startup.
            const string testSecret = "test-integration-secret-key-at-least-32-characters-long!!";
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = "lms-api",
                    ValidAudience            = "lms-client",
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                   Encoding.UTF8.GetBytes(testSecret)),
                    ClockSkew                = TimeSpan.Zero,
                };
            });
        });
    }
}
