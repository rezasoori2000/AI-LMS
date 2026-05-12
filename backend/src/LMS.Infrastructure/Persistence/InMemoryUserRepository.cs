using System.Collections.Concurrent;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Users;

namespace LMS.Infrastructure.Persistence;

/// <summary>
/// In-memory user store — REPLACED by <see cref="UserRepository"/> (EF Core / PostgreSQL).
///
/// Retained for reference and as a fallback if a test or tool needs a zero-infrastructure
/// IUserRepository.  No longer registered in <see cref="DependencyInjection"/>.
///
/// To reinstate (e.g. for a standalone in-process test):
///   services.AddSingleton&lt;IUserRepository, InMemoryUserRepository&gt;();
/// </summary>
[Obsolete("Use UserRepository (EF Core) instead. " +
          "This class exists as a reference/fallback only and is not registered in DI.")]
public sealed class InMemoryUserRepository : IUserRepository
{
    // Primary store: UserId → User
    private readonly ConcurrentDictionary<Guid, User> _byId =
        new();

    // Email index: normalised email → UserId (avoids a full scan on login)
    private readonly ConcurrentDictionary<string, Guid> _emailIndex =
        new(StringComparer.OrdinalIgnoreCase);

    // ── IUserRepository ───────────────────────────────────────────────────────

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        _byId.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        User? user = null;
        if (_emailIndex.TryGetValue(email, out var id))
            _byId.TryGetValue(id, out user);

        return Task.FromResult(user);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_emailIndex.ContainsKey(email));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _byId[user.Id] = user;
        _emailIndex[user.Email] = user.Id;
        return Task.CompletedTask;
    }

    /// <summary>No-op: mutations are applied immediately in <see cref="AddAsync"/>.</summary>
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        Task.CompletedTask;
}
