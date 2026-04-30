using LMS.Domain.Users;

namespace LMS.Application.Auth.Dtos;

/// <summary>
/// Returned by both <c>POST /api/auth/login</c> and <c>POST /api/auth/register</c>.
///
/// The frontend stores <see cref="AccessToken"/> and attaches it as
/// <c>Authorization: Bearer {AccessToken}</c> on subsequent requests.
///
/// Refresh token strategy is deferred. When added, a <c>RefreshToken</c> field
/// will be included and the client will call <c>POST /api/auth/refresh</c>.
/// </summary>
public sealed record AuthResponse(
    string   AccessToken,
    string   TokenType,
    int      ExpiresIn,   // seconds
    Guid     UserId,
    string   Email,
    UserRole Role,
    Guid?    TenantId
);
