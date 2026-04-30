using LMS.Application.Auth;
using LMS.Application.Auth.Dtos;

namespace LMS.Infrastructure.Auth;

/// <summary>
/// Placeholder IAuthService — registered until the real implementation is built in
/// Phase 1 Section 3 Part 2 (when IUserRepository is available).
///
/// Returns HTTP 501 for every call so the API starts cleanly and the /health endpoint
/// works during integration tests before Phase 3 DB wiring is in place.
/// </summary>
internal sealed class NotImplementedAuthService : IAuthService
{
    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => throw new NotImplementedException(
            "IAuthService.LoginAsync is implemented in Phase 1 Section 3 Part 2.");

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => throw new NotImplementedException(
            "IAuthService.RegisterAsync is implemented in Phase 1 Section 3 Part 2.");
}
