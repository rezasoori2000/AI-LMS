using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LMS.Application.Auth;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Infrastructure.Auth;

/// <summary>
/// Produces signed HS256 JWT access tokens.
///
/// Standard claims issued:
///   sub   — user GUID (RFC 7519 subject)
///   email — user email
///   role  — UserRole enum name (e.g. "Student") — matched by ASP.NET ClaimTypes.Role
///   tid   — tenant GUID, empty string for SuperAdmin
///   jti   — unique token ID per issuance (hook for future revocation)
///   iat   — issued-at Unix timestamp
///   nbf   — not-before = issued-at (no pre-issued tokens)
///   exp   — expiry = issued-at + JwtSettings.ExpiryMinutes
///
/// Algorithm: HMAC-SHA256 (HS256) — single-key symmetric signing.
/// Asymmetric (RS256) is better for multi-service jwt consumers but adds key
/// management overhead. Switch to RS256 when an API gateway is introduced.
/// </summary>
internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now         = DateTime.UtcNow;
        var expiry      = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role,               user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                      DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                      ClaimValueTypes.Integer64),
            // "tid" (tenant id) — empty string for SuperAdmin so the claim is always present
            new Claim("tid", user.TenantId?.ToString() ?? string.Empty),
        };

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            notBefore:          now,
            expires:            expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
