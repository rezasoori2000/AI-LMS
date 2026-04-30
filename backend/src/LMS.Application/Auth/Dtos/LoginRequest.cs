using System.ComponentModel.DataAnnotations;

namespace LMS.Application.Auth.Dtos;

/// <summary>
/// Request body for <c>POST /api/auth/login</c>.
/// Data annotations serve as both documentation and the first validation layer;
/// FluentValidation (Phase 2) adds richer rules.
/// </summary>
public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);
