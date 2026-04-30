using System.Text;
using LMS.Application.Auth;
using LMS.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace LMS.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all API-layer services: controllers, Swagger (dev only), CORS, health checks,
    /// and JWT bearer authentication.
    /// </summary>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddControllers();

        // ── Authentication (JWT bearer) ────────────────────────────────────────
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>();

        if (jwtSettings is { SecretKey.Length: >= 32 })
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey  = true,
                        ValidIssuer              = jwtSettings.Issuer,
                        ValidAudience            = jwtSettings.Audience,
                        IssuerSigningKey         = new SymmetricSecurityKey(
                                                       Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        // No clock-skew tolerance — tokens expire exactly at exp claim.
                        // Increase to TimeSpan.FromMinutes(1) if distributing across
                        // slightly-skewed clocks (e.g. multiple API instances).
                        ClockSkew                = TimeSpan.Zero,
                    };
                });
        }
        else
        {
            // JWT secret not configured (test/bootstrap environment).
            // Authentication middleware is registered but no scheme is active;
            // [Authorize] endpoints will return 401 until Jwt:SecretKey is supplied.
            services.AddAuthentication();
        }

        services.AddAuthorization();

        // ── Swagger ───────────────────────────────────────────────────────────
        if (environment.IsDevelopment())
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title       = "LMS API",
                    Version     = "v1",
                    Description = "AI-Powered Learning Management System API"
                });

                // JWT bearer security definition — lets Swagger UI send Authorization headers
                const string scheme = "Bearer";
                options.AddSecurityDefinition(scheme, new OpenApiSecurityScheme
                {
                    Name         = "Authorization",
                    Type         = SecuritySchemeType.Http,
                    Scheme       = scheme,
                    BearerFormat = "JWT",
                    In           = ParameterLocation.Header,
                    Description  = "Enter your JWT token. Example: eyJhbGci...",
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id   = scheme,
                            }
                        }
                    ] = []
                });
            });
        }

        // ── CORS ──────────────────────────────────────────────────────────────
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

        // ── Health checks ─────────────────────────────────────────────────────
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
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        return services;
    }
}
