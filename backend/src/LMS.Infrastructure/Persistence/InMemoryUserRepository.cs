using System.Collections.Concurrent;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Users;

namespace LMS.Infrastructure.Persistence;

/// <summary>
/// In-memory user store for local development and unit/integration testing.
/// Replaces the real PostgreSQL-backed repository until Phase 3 adds EF Core.
///
/// Thread-safety: two <see cref="ConcurrentDictionary{TKey,TValue}"/> instances
/// act as the primary store (by ID) and an email index (by normalised email).
/// Registering this as a <b>singleton</b> keeps the state alive for the process
/// lifetime — suitable for development but never for production.
///
/// <see cref="SaveChangesAsync"/> is a no-op here because every mutation is
/// applied immediately inside <see cref="AddAsync"/>. The method exists to
/// satisfy the unit-of-work contract that the EF Core implementation will fulfil.
/// </summary>
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
