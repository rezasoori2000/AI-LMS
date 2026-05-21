using LMS.Application.Auth;
using LMS.Application.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Api.Controllers;

/// <summary>
/// Authentication endpoints — login, register, and (future) token refresh.
///
/// All endpoints are <see cref="AllowAnonymousAttribute"/> by design: callers do not
/// yet have a token when they reach these routes.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>
    /// Authenticates a user and returns a JWT access token.
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var response = await _auth.LoginAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Registers a new account and returns a JWT access token.
    /// POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var response = await _auth.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // POST /api/auth/refresh  — deferred to Section 3 Part 3
    // POST /api/auth/logout   — deferred to Section 3 Part 3 (token revocation)
}
