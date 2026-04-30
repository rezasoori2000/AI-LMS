using System.Security.Cryptography;
using System.Text;
using LMS.Application.Common.Interfaces;

namespace LMS.Infrastructure.Auth;

/// <summary>
/// PBKDF2-SHA256 password hasher.
///
/// Storage format: "{base64(salt)}.{base64(hash)}"
/// Salt:   16 bytes (128 bits), cryptographically random per call.
/// Hash:   32 bytes (256 bits).
/// Iter:   310,000 — NIST SP 800-132 recommended minimum for PBKDF2-SHA256 (2023).
///
/// No external packages — uses System.Security.Cryptography exclusively.
/// Constant-time comparison (CryptographicOperations.FixedTimeEquals) prevents
/// timing side-channel attacks on the Verify path.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltBytes  = 16;
    private const int HashBytes  = 32;
    private const int Iterations = 310_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashBytes);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedHash);

        var parts = storedHash.Split('.');
        if (parts.Length != 2)
            return false;

        byte[] salt, expectedHash;
        try
        {
            salt         = Convert.FromBase64String(parts[0]);
            expectedHash = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashBytes);

        // Constant-time equals — prevents attackers from inferring partial matches
        // via response timing differences.
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
