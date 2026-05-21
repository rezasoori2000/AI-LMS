using System.Text;
using System.Text.Json.Serialization;
using LMS.Application.Admin.Students;
using LMS.Application.AiTutor;
using LMS.Application.Auth;
using LMS.Application.Content.Chapters;
using LMS.Application.Content.Grades;
using LMS.Application.Content.Lessons;
using LMS.Application.Content.Questions;
using LMS.Application.Content.Subjects;
using LMS.Application.Parent;
using LMS.Application.Student;
using LMS.Application.Teacher;
using LMS.Infrastructure;
using LMS.Infrastructure.AI;
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

        // Guard: a JWT secret of fewer than 32 characters is either missing or too short to
        // be cryptographically safe.  In Production (and any other non-Development, non-Testing
        // environment) this is a hard startup failure — allowing the application to start with
        // validation disabled would permit any crafted token to be accepted as authenticated.
        // In Testing the integration-test factory overrides TokenValidationParameters via
        // PostConfigure, so the IConfiguration key is intentionally absent; we skip silently.
        // In Development a critical log warns the developer without blocking startup.
        if (secretKey.Length < 32)
        {
            var isLocalEnv = environment.IsDevelopment() || environment.IsEnvironment("Testing");
            if (!isLocalEnv)
                throw new InvalidOperationException(
                    "Jwt:SecretKey must be at least 32 characters. " +
                    "Supply it via the Jwt__SecretKey environment variable or a secrets manager. " +
                    "Never commit a real key to source control.");

            if (environment.IsDevelopment())
            {
                var startupLogger = LoggerFactory
                    .Create(lb => lb.AddConsole())
                    .CreateLogger("LMS.Api.Extensions.ServiceCollectionExtensions");
                startupLogger.LogCritical(
                    "[Security] Jwt:SecretKey is missing or shorter than 32 characters. " +
                    "JWT signature validation is disabled. Acceptable only in local Development.");
            }
        }

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

        // ── Student portal services ───────────────────────────────────────
        services.AddScoped<IStudentService, StudentService>();

        // ── Teacher portal services ───────────────────────────────────────
        services.AddScoped<ITeacherService, TeacherService>();

        // ── AI Tutor services ─────────────────────────────────────────────
        // StubTutorProvider returns a placeholder response (no LLM call).
        // Phase 2: swap to services.AddScoped<ITutorProvider, OllamaTutorProvider>();
        services.AddScoped<ITutorProvider, StubTutorProvider>();
        services.AddScoped<ITutorService,  TutorService>();

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
