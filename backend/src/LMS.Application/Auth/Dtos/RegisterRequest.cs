using System.ComponentModel.DataAnnotations;
using LMS.Domain.Users;

namespace LMS.Application.Auth.Dtos;

/// <summary>
/// Request body for <c>POST /api/auth/register</c>.
///
/// Notes:
/// - <see cref="Role"/> is accepted in the request for Phase 1 flexibility.
///   In production (Phase 3+) self-registration will only allow <c>Parent</c> or <c>Student</c>;
///   admin/teacher roles require an invite flow.
/// - <see cref="TenantId"/> is null for SuperAdmin registration (platform bootstrap only).
/// </summary>
public sealed record RegisterRequest(
    [Required, EmailAddress]         string   Email,
    [Required, MinLength(8)]         string   Password,
    [Required, MinLength(1)]         string   FirstName,
    [Required, MinLength(1)]         string   LastName,
    [Required]                       UserRole Role,
                                     Guid?    TenantId = null
);
