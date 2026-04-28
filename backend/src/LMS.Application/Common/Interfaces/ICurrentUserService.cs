namespace LMS.Application.Common.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user's context.
/// Implemented in the API layer via <c>HttpContext</c> claims (Phase 2).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The authenticated user's ID, or <c>null</c> if anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>The authenticated user's email, or <c>null</c> if anonymous.</summary>
    string? Email { get; }

    bool IsAuthenticated { get; }
}
