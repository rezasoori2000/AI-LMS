using LMS.Domain.Users;

namespace LMS.Application.Common.Interfaces;

/// <summary>
/// Data access contract for User persistence.
/// Implementation lives in LMS.Infrastructure (EF Core / PostgreSQL — Phase 3).
///
/// Only auth-relevant query operations are defined here for Phase 1 Section 3.
/// Additional queries (paged lists, tenant-filtered lookups) are added when needed.
/// </summary>
public interface IUserRepository
{
    /// <summary>Returns the user with the given ID, or null if not found.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the user whose email matches (case-insensitive), or null.
    /// Used by login and registration duplicate checks.
    /// </summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns true if any user already has the given email address.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Adds a new user to the underlying store.</summary>
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>Persists any pending unit-of-work changes.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
