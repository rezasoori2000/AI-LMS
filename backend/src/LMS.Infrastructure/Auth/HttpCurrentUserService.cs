using System.Security.Claims;
using LMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LMS.Infrastructure.Auth;

/// <summary>
/// Reads the current user's identity from the JWT claims in <see cref="IHttpContextAccessor"/>.
///
/// Claim layout (set by <see cref="JwtTokenService"/>):
///   sub   → UserId (Guid)
///   email → Email
///   role  → Role string matching UserRole enum name, e.g. "SuperAdmin"
///   tid   → TenantId Guid, empty string for SuperAdmin
/// </summary>
internal sealed class HttpCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Role =>
        User?.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? TenantId
    {
        get
        {
            var tid = User?.FindFirst("tid")?.Value;
            // SuperAdmin tokens store empty string; return null in that case.
            return string.IsNullOrEmpty(tid) ? null : Guid.TryParse(tid, out var id) ? id : null;
        }
    }
}
