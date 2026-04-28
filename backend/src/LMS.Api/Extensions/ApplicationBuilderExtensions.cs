using LMS.Api.Middleware;

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

        // 6. Authentication — deferred to Phase 2
        // app.UseAuthentication();

        // 7. Authorization — deferred to Phase 2
        // app.UseAuthorization();

        // 8. Controllers and health endpoint
        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }
}
