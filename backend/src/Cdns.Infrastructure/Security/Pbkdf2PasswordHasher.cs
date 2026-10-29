using System.Security.Cryptography;

namespace Cdns.Infrastructure.Security;

// Matches legacy SimpleCrypto.NetCore PBKDF2 parameters: 100,000 iterations,
// 34-byte salt, 64-byte derived key.
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 34;
    private const int HashSize = 64;

    public (string Hash, string Salt) HashNewPassword(string plainPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(plainPassword, salt, Iterations, HashAlgorithmName.SHA1, HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string plainPassword, string storedHash, string storedSalt)
    {
        var salt = Convert.FromBase64String(storedSalt);
        var computed = Rfc2898DeriveBytes.Pbkdf2(plainPassword, salt, Iterations, HashAlgorithmName.SHA1, HashSize);
        return CryptographicOperations.FixedTimeEquals(computed, Convert.FromBase64String(storedHash));
    }
}
