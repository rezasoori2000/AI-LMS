namespace LMS.Application.Auth;

/// <summary>
/// Thrown when login credentials are missing, wrong, or the account is inactive.
/// Maps to HTTP 401 Unauthorized.
///
/// A deliberately generic message is used so callers cannot distinguish
/// "email not found" from "wrong password" (credential enumeration mitigation).
/// </summary>
public sealed class InvalidCredentialsException()
    : Exception("Invalid email or password.");
