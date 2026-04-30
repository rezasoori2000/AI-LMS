using LMS.Application.Auth.Dtos;

namespace LMS.Application.Auth;

/// <summary>
/// Application service contract for authentication operations.
/// Implementation in LMS.Application.Auth.AuthService (Phase 1 Section 3 Part 2).
///
/// Orchestration responsibility:
///   Login  → validate credentials → RecordLogin → issue token → return AuthResponse
///   Register → check duplicate → hash password → Create user → persist → issue token
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates credentials and returns a token response.
    /// Throws <see cref="UnauthorizedAccessException"/> for bad credentials
    /// or a deactivated account.
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Registers a new account and immediately returns a token response so
    /// the client can proceed without a separate login round-trip.
    /// Throws <see cref="InvalidOperationException"/> if the email is already taken.
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
