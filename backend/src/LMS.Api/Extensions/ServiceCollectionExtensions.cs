using Microsoft.OpenApi.Models;

namespace LMS.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all API-layer services: controllers, Swagger (dev only), CORS, health checks.
    /// </summary>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddControllers();

        // Swagger only in development — never expose API schema publicly in production
        if (environment.IsDevelopment())
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "LMS API",
                    Version = "v1",
                    Description = "AI-Powered Learning Management System API"
                });

                // TODO (Phase 2): Add JWT bearer security definition
                // options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
            });
        }

        // CORS — origins come from config, never hardcoded
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Built-in health checks endpoint (no sensitive details exposed)
        services.AddHealthChecks();
        // TODO (Phase 3): Add database health check
        //   .AddNpgsql(configuration.GetConnectionString("Default")!)

        return services;
    }

    /// <summary>
    /// Registers Application layer services (MediatR, validators, etc.).
    /// Deferred to Phase 2.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // TODO (Phase 2): Register MediatR and FluentValidation pipeline behaviors
        // services.AddMediatR(cfg =>
        //     cfg.RegisterServicesFromAssembly(typeof(IApplicationAssemblyMarker).Assembly));
        // services.AddValidatorsFromAssembly(typeof(IApplicationAssemblyMarker).Assembly);

        return services;
    }

    /// <summary>
    /// Registers Infrastructure layer services (DbContext, repositories, external clients).
    /// Deferred to Phase 3.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO (Phase 3): Register EF Core DbContext, repository implementations,
        //   email sender, file storage client, etc.
        // services.AddInfrastructure(configuration);

        return services;
    }
}
