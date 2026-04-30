using LMS.Domain.Users;

namespace LMS.Application.Common.Interfaces;

/// <summary>
/// Issues JWT tokens for authenticated users.
/// Implementation in LMS.Infrastructure uses HMAC-SHA256 (HS256) signing via
/// <c>System.IdentityModel.Tokens.Jwt</c>.
///
/// Claims written into the access token:
///   sub   — user GUID
///   email — user email
///   role  — UserRole enum name (e.g. "Student", "Teacher")
///   tid   — tenant GUID (empty string for SuperAdmin)
///   jti   — unique token ID (for future revocation list)
///   iat   — issued-at (Unix seconds)
///   exp   — expiry (from JwtSettings.ExpiryMinutes)
/// </summary>
public interface ITokenService
{
    /// <summary>Generates a signed JWT access token for the given user.</summary>
    string GenerateAccessToken(User user);

    // Refresh token strategy is deferred to Phase 1 Section 3 Part 3+.
    // When added, the signature will be:
    //   Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(User user);
}
