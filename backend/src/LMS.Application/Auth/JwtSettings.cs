namespace LMS.Application.Auth;

/// <summary>
/// JWT configuration — bound from <c>appsettings.json</c> via the Options pattern:
/// <code>services.Configure&lt;JwtSettings&gt;(configuration.GetSection(JwtSettings.SectionName));</code>
///
/// Production: supply <c>Jwt__SecretKey</c> (double-underscore = nested) via environment
/// variable or a secrets manager (Azure Key Vault, AWS SSM, Docker secrets).
/// Never commit a real secret to source control.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing secret key.
    /// Minimum 32 characters (256 bits) for HS256.
    /// Leave empty in appsettings.json; provide via environment/secrets only.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Token issuer — typically the API's base URL.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Token audience — typically the frontend's base URL.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Access token lifetime in minutes. Default: 60.</summary>
    public int ExpiryMinutes { get; init; } = 60;
}
