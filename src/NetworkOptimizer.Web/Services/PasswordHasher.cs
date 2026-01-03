using System.Security.Cryptography;

namespace NetworkOptimizer.Web.Services;

/// <summary>
/// Secure password hashing using PBKDF2-SHA256.
/// Passwords are one-way hashed (not reversible).
/// Format: {iterations}.{salt_base64}.{hash_base64}
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    // OWASP recommended: 600,000 iterations for PBKDF2-SHA256 (2023)
    // https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
    private const int Iterations = 600_000;
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits

    /// <summary>
    /// Hash a password using PBKDF2-SHA256 with random salt.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A formatted hash string containing iterations, salt, and hash in format: {iterations}.{salt_base64}.{hash_base64}</returns>
    /// <exception cref="ArgumentException">Thrown when password is null or empty.</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        // Generate random salt
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash password with PBKDF2-SHA256
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        // Format: iterations.salt.hash
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verify a password against a stored hash using constant-time comparison.
    /// </summary>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="storedHash">The stored hash string to compare against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        try
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 3)
                return false;

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);

            // Hash the input password with same parameters
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            // Any parsing error = invalid hash format
            return false;
        }
    }

    /// <summary>
    /// Check if a hash needs to be rehashed (e.g., iteration count increased).
    /// </summary>
    /// <param name="storedHash">The stored hash string to check.</param>
    /// <returns>True if the hash uses outdated parameters and should be rehashed; otherwise, false.</returns>
    public bool NeedsRehash(string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return true;

        try
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 3)
                return true;

            var iterations = int.Parse(parts[0]);
            return iterations < Iterations;
        }
        catch
        {
            return true;
        }
    }
}

/// <summary>
/// Interface for secure password hashing operations.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a password using a secure algorithm with random salt.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A formatted hash string suitable for storage.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a stored hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="storedHash">The stored hash string to compare against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool VerifyPassword(string password, string storedHash);

    /// <summary>
    /// Check if a hash needs to be rehashed due to outdated parameters.
    /// </summary>
    /// <param name="storedHash">The stored hash string to check.</param>
    /// <returns>True if the hash should be rehashed; otherwise, false.</returns>
    bool NeedsRehash(string storedHash);
}
