using LMS.Application.Auth;
using LMS.Application.Common.Interfaces;
using LMS.Infrastructure.Auth;
using LMS.Infrastructure.Persistence;
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

        // In-memory user store for Phase 1/2 development (replaced by EF Core repository in Phase 3).
        // Singleton keeps the dictionary alive for the process lifetime.
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        // Real auth service — depends on IUserRepository, IPasswordHasher, ITokenService
        services.AddScoped<IAuthService, AuthService>();

        // ── Deferred ─────────────────────────────────────────────────────────

        // TODO (Phase 3): Replace InMemoryUserRepository with the EF Core implementation.
        //   services.AddDbContext<LmsDbContext>(o =>
        //       o.UseNpgsql(configuration.GetConnectionString("Default")));
        //   services.AddScoped<IUserRepository, UserRepository>();

        // TODO (Phase 2): Register ICurrentUserService (reads HttpContext.User claims)
        //   services.AddHttpContextAccessor();
        //   services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}

