using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Infrastructure;

/// <summary>
/// Infrastructure layer DI registration entry point.
/// Called from <c>LMS.Api</c>'s service collection extension as the composition root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO (Phase 3): Register EF Core DbContext
        //   services.AddDbContext<LmsDbContext>(options =>
        //       options.UseNpgsql(configuration.GetConnectionString("Default")));

        // TODO (Phase 3): Register repository implementations
        //   services.AddScoped<IUserRepository, UserRepository>();

        // TODO (Phase 2): Register ICurrentUserService implementation
        //   services.AddHttpContextAccessor();
        //   services.AddScoped<ICurrentUserService, CurrentUserService>();

        // TODO (Phase 3): Register external service clients (email, file storage, etc.)

        return services;
    }
}
