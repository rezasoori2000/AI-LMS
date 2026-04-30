namespace LMS.Application.Auth;

/// <summary>
/// Thrown when a registration attempt uses an email that already exists.
/// Maps to HTTP 409 Conflict.
///
/// The email is stored for logging purposes only — it is included in the server
/// log but never surfaced in the HTTP response body (to balance debuggability
/// against information leakage).
/// </summary>
public sealed class EmailAlreadyRegisteredException(string email)
    : Exception($"The email address is already registered.")
{
    /// <summary>The duplicate email (for structured logging only).</summary>
    public string Email { get; } = email;
}
