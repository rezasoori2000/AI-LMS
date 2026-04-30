using LMS.Application.Auth.Dtos;
using LMS.Application.Common.Interfaces;
using LMS.Domain.Users;
using Microsoft.Extensions.Options;

namespace LMS.Application.Auth;

/// <summary>
/// Orchestrates user registration and login.
///
/// Lives in the Application layer so it has no HTTP or crypto dependencies —
/// those are injected via interfaces from the Infrastructure layer.
///
/// Error signalling conventions (caught by ExceptionHandlingMiddleware):
///   - Duplicate email on register  → <see cref="EmailAlreadyRegisteredException"/>
///   - Bad credentials / inactive   → <see cref="InvalidCredentialsException"/>
///
/// These are intentionally separate exception types so the middleware can map
/// them to the correct HTTP status codes (409 / 401) without any business-logic
/// coupling in the API layer.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository  _users;
    private readonly IPasswordHasher  _hasher;
    private readonly ITokenService    _tokens;
    private readonly JwtSettings      _jwtSettings;

    public AuthService(
        IUserRepository  users,
        IPasswordHasher  hasher,
        ITokenService    tokens,
        IOptions<JwtSettings> jwtOptions)
    {
        _users       = users;
        _hasher      = hasher;
        _tokens      = tokens;
        _jwtSettings = jwtOptions.Value;
    }

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        // 1. Duplicate-email guard
        if (await _users.ExistsByEmailAsync(request.Email, ct))
            throw new EmailAlreadyRegisteredException(request.Email);

        // 2. Hash the raw password — plaintext never touches the entity
        var passwordHash = _hasher.Hash(request.Password);

        // 3. Create the aggregate via its factory (invariants enforced inside)
        var user = User.Create(
            email:        request.Email,
            passwordHash: passwordHash,
            role:         request.Role,
            tenantId:     request.TenantId,
            firstName:    request.FirstName,
            lastName:     request.LastName);

        // 4. Persist
        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        // 5. Issue token and return
        return BuildResponse(user);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        // 1. Lookup by email — intentionally same error for "not found" and "bad password"
        //    to prevent user-enumeration attacks.
        var user = await _users.FindByEmailAsync(request.Email, ct);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        // 2. Deactivated account — same 401; do not reveal why.
        if (!user.IsActive)
            throw new InvalidCredentialsException();

        // 3. Record the login timestamp on the aggregate
        user.RecordLogin();
        await _users.SaveChangesAsync(ct);

        // 4. Issue token and return
        return BuildResponse(user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AuthResponse BuildResponse(User user) =>
        new(
            AccessToken: _tokens.GenerateAccessToken(user),
            TokenType:   "Bearer",
            ExpiresIn:   _jwtSettings.ExpiryMinutes * 60,
            UserId:      user.Id,
            Email:       user.Email,
            Role:        user.Role,
            TenantId:    user.TenantId);
}
