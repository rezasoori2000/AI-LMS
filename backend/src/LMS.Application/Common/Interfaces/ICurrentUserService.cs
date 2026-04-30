namespace LMS.Application.Common.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user's context.
/// Implemented in the API layer via <c>HttpContext</c> claims (Phase 2).
///
/// Each property reads from the JWT claims injected by the bearer middleware;
/// returns null for anonymous requests.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>JWT <c>sub</c> claim — the user's GUID, or null if anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>JWT <c>email</c> claim, or null if anonymous.</summary>
    string? Email { get; }

    /// <summary>
    /// JWT <c>role</c> claim (e.g. "Student", "Teacher").
    /// Matches <see cref="LMS.Domain.Users.UserRole"/> enum name.
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// JWT <c>tid</c> claim — the tenant GUID, or null for platform-level users (SuperAdmin)
    /// and anonymous requests.
    /// </summary>
    Guid? TenantId { get; }

    bool IsAuthenticated { get; }
}
