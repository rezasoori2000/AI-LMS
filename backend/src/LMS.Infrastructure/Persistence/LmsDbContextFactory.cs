using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LMS.Infrastructure.Persistence;

/// <summary>
/// Provides a <see cref="LmsDbContext"/> at design time so EF Core migration tools
/// (<c>dotnet ef</c>) can run without the full application host.
///
/// This factory is discovered automatically by the EF Core tooling because it implements
/// <see cref="IDesignTimeDbContextFactory{TContext}"/> and lives in the same assembly
/// as <see cref="LmsDbContext"/>.
///
/// The hardcoded connection string here is used ONLY by the migration tools.
/// The application always reads its connection string from appsettings / environment variables.
///
/// Migration workflow:
/// <code>
///   # From the backend/ folder:
///   dotnet ef migrations add InitialCreate \
///     --project src/LMS.Infrastructure    \
///     --startup-project src/LMS.Api
///
///   dotnet ef database update             \
///     --project src/LMS.Infrastructure    \
///     --startup-project src/LMS.Api
/// </code>
/// </summary>
public sealed class LmsDbContextFactory : IDesignTimeDbContextFactory<LmsDbContext>
{
    public LmsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LmsDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=lms_dev;Username=lms;Password=lms",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .Options;

        return new LmsDbContext(options);
    }
}
