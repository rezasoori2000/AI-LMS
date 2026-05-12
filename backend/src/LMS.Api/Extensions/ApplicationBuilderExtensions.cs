using LMS.Api.Middleware;
using LMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LMS.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the full HTTP middleware pipeline in the correct order.
    /// Order matters: exception handling must be outermost, auth before authorization.
    /// </summary>
    public static WebApplication UseApiPipeline(
        this WebApplication app,
        IWebHostEnvironment environment)
    {
        // 1. Global exception handler — must be first so it catches all downstream errors
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // 2. Swagger UI — development only
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS API v1");
                options.RoutePrefix = "swagger";
            });
        }

        // 3. HTTPS redirect
        app.UseHttpsRedirection();

        // 4. CORS — must precede authentication
        app.UseCors("DefaultPolicy");

        // 5. Tenant resolution — deferred to Phase 2
        // app.UseMiddleware<TenantMiddleware>();

        // 6. Authentication — validates JWT bearer tokens on incoming requests
        app.UseAuthentication();

        // 7. Authorization — enforces [Authorize] policies and role requirements
        app.UseAuthorization();

        // 8. Controllers and health endpoint
        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }

    /// <summary>
    /// Applies pending EF Core migrations and seeds development data.
    /// Only runs when the application is in the Development environment.
    /// Safe to call on every startup — the seeder is idempotent.
    ///
    /// If the database is unreachable (e.g. in integration tests that use
    /// <c>WebApplicationFactory</c> without a live PostgreSQL instance) the error
    /// is logged as a warning and startup continues normally.
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        using var scope  = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            // A connection failure here most commonly means PostgreSQL is not running
            // (e.g. integration tests that do not spin up a real database).
            // Log and continue — the API itself should still start.
            logger.LogWarning(ex,
                "Database seed was skipped. " +
                "If running locally, ensure PostgreSQL is running via 'docker compose up postgres'.");
        }
    }
}

