using LMS.Domain.Common;

namespace LMS.Domain.Users;

/// <summary>
/// The User aggregate root — owns identity and authentication concerns.
///
/// Design notes:
/// - Private constructor + static <see cref="Create"/> factory enforces invariants at the boundary.
/// - Passwords are never stored or exposed here: only <see cref="PasswordHash"/> (PBKDF2) lives
///   on the entity; hashing is delegated to IPasswordHasher in the Application layer.
/// - <see cref="TenantId"/> is null for SuperAdmin (platform-level); required for all other roles.
///   This single nullable field is the hook for tenant isolation (Phase 3).
/// - Behavior methods keep mutation logic inside the aggregate rather than in application services.
/// </summary>
public sealed class User : AuditableEntity
{
    // Private parameterless constructor — EF Core and the Create factory are the only callers.
    private User() { }

    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// PBKDF2-SHA256 hash in the form "{base64(salt)}.{base64(hash)}".
    /// The raw password is never stored.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    /// <summary>
    /// null  → SuperAdmin (platform scope).
    /// Guid  → scoped to that tenant (all other roles).
    /// </summary>
    public Guid? TenantId { get; private set; }

    public bool IsActive { get; private set; } = true;

    public string? FirstName { get; private set; }
    public string? LastName  { get; private set; }

    /// <summary>UTC timestamp of the most recent successful login. null until first login.</summary>
    public DateTime? LastLoginAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new User aggregate.
    /// The <paramref name="passwordHash"/> must already be hashed by the caller via
    /// <c>IPasswordHasher.Hash(rawPassword)</c> — this method never accepts plaintext passwords.
    /// </summary>
    public static User Create(
        string    email,
        string    passwordHash,
        UserRole  role,
        Guid?     tenantId  = null,
        string?   firstName = null,
        string?   lastName  = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Email        = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            Role         = role,
            TenantId     = tenantId,
            FirstName    = firstName?.Trim(),
            LastName     = lastName?.Trim(),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>Records a successful login. Called by AuthService after token issuance.</summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt   = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft-deactivates the account. The row is retained for audit history.
    /// Active check is enforced in AuthService: deactivated users receive 401.
    /// </summary>
    public void Deactivate()
    {
        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Replaces the stored password hash. Called by the password-reset flow (Phase 3).
    /// The caller must hash the new raw password before passing it here.
    /// </summary>
    public void UpdatePasswordHash(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
        UpdatedAt    = DateTime.UtcNow;
    }
}
