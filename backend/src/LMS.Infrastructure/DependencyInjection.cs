using LMS.Application.Auth;
using LMS.Application.Common.Interfaces;
using LMS.Infrastructure.Auth;
using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Infrastructure;

/// <summary>
/// Infrastructure layer DI registration entry point.
/// Called from <c>LMS.Api</c>'s <c>AddInfrastructureServices</c> extension.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Auth ──────────────────────────────────────────────────────────────

        // Bind JwtSettings from appsettings.json "Jwt" section
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        // Password hashing — stateless, singleton is safe
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // JWT token generation — stateless, singleton is safe
        services.AddSingleton<ITokenService, JwtTokenService>();

        // EF Core user repository — scoped to the request, shares LmsDbContext with the
        // rest of the request scope.  Replaces InMemoryUserRepository from Phase 1/2.
        services.AddScoped<IUserRepository, UserRepository>();

        // Real auth service — depends on IUserRepository, IPasswordHasher, ITokenService
        services.AddScoped<IAuthService, AuthService>();

        // ── Persistence ───────────────────────────────────────────────────────

        // PostgreSQL via EF Core.
        // Connection string is read from appsettings.json "ConnectionStrings:Default".
        // Override with the environment variable ConnectionStrings__Default in CI/prod.
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Required connection string 'Default' is not configured. " +
                "Set ConnectionStrings:Default in appsettings or via " +
                "the ConnectionStrings__Default environment variable.");

        services.AddDbContext<LmsDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Expose LmsDbContext via ILmsDbContext — resolves the same scoped instance,
        // so Application services that inject ILmsDbContext share state with UserRepository.
        services.AddScoped<ILmsDbContext>(sp => sp.GetRequiredService<LmsDbContext>());

        // Development seed data — scoped so it can resolve IPasswordHasher (singleton) and
        // LmsDbContext (scoped).  The Api startup calls this only in the Development environment.
        services.AddScoped<DatabaseSeeder>();

        // ── Deferred ─────────────────────────────────────────────────────────

        // ICurrentUserService reads HttpContext.User claims.
        // IHttpContextAccessor is registered in AddApiServices (API layer).
        services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

        return services;
    }
}

