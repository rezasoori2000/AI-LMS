using LMS.Application.Common.Interfaces;
using LMS.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Persistence;

/// <summary>
/// EF Core / PostgreSQL implementation of <see cref="IUserRepository"/>.
///
/// Replaces <see cref="InMemoryUserRepository"/> which was a development bootstrap.
/// This implementation is scoped per HTTP request so it shares the same
/// <see cref="LmsDbContext"/> instance as any other scoped service — i.e. writes made here
/// are visible to other services in the same request scope without an extra round-trip.
///
/// Unit-of-work pattern:
///   - <see cref="AddAsync"/> stages the entity in the EF Core change tracker only.
///   - <see cref="SaveChangesAsync"/> flushes all staged changes to PostgreSQL.
///   - Callers (AuthService) must call SaveChangesAsync to persist.
///   This matches the contract defined on <see cref="IUserRepository"/>.
/// </summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly LmsDbContext _db;

    public UserRepository(LmsDbContext db) => _db = db;

    // ── Queries ───────────────────────────────────────────────────────────────

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users
              .AsNoTracking()
              .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users
              .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users
              .AnyAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Stages the new user in the change tracker.
    /// Caller must call <see cref="SaveChangesAsync"/> to persist.
    /// </summary>
    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    /// <summary>
    /// Flushes all pending change-tracker entries to PostgreSQL.
    /// Delegates to LmsDbContext.SaveChangesAsync so the audit hook runs.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
