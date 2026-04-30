namespace LMS.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies passwords.
/// Implementation in LMS.Infrastructure uses PBKDF2-SHA256 with a random salt
/// (no external packages required — <see cref="System.Security.Cryptography"/>).
///
/// The interface lives in Application so AppServices can depend on the abstraction
/// without referencing any crypto library directly.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Produces a hash string in the form "{base64(salt)}.{base64(hash)}".
    /// Each call generates a fresh random salt, so hashing the same password twice
    /// produces different output — this is correct and expected.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Returns true when <paramref name="password"/> produces the same hash as
    /// <paramref name="storedHash"/>. Uses constant-time comparison to prevent
    /// timing side-channel attacks.
    /// </summary>
    bool Verify(string password, string storedHash);
}
