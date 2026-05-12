using System.Text;
using System.Text.Json.Serialization;
using LMS.Application.Admin.Students;
using LMS.Application.Auth;
using LMS.Application.Content.Chapters;
using LMS.Application.Content.Grades;
using LMS.Application.Content.Lessons;
using LMS.Application.Content.Questions;
using LMS.Application.Content.Subjects;
using LMS.Application.Parent;
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
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // IHttpContextAccessor -- required by HttpCurrentUserService
        services.AddHttpContextAccessor();

        // -- Authentication (JWT bearer) ------------------------------------
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>();

        var secretKey = jwtSettings?.SecretKey ?? string.Empty;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = secretKey.Length >= 32,
                    ValidateAudience         = secretKey.Length >= 32,
                    ValidateLifetime         = secretKey.Length >= 32,
                    ValidateIssuerSigningKey  = secretKey.Length >= 32,
                    ValidIssuer              = jwtSettings?.Issuer ?? string.Empty,
                    ValidAudience            = jwtSettings?.Audience ?? string.Empty,
                    IssuerSigningKey         = secretKey.Length >= 32
                        ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                        : new SymmetricSecurityKey(new byte[32]),
                    ClockSkew                = TimeSpan.Zero,
                };
            });

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
    /// Registers Application layer services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ── Admin content services ────────────────────────────────────────
        services.AddScoped<IGradeService,   GradeService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<IChapterService, ChapterService>();
        services.AddScoped<ILessonService,  LessonService>();
        services.AddScoped<IQuestionService, QuestionService>();

        // ── Admin student linkage services ────────────────────────────────
        services.AddScoped<IStudentAdminService, StudentAdminService>();

        // ── Parent portal services ────────────────────────────────────────
        services.AddScoped<IParentService, ParentService>();

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
