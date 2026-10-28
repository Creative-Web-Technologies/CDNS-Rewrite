namespace Cdns.Infrastructure.Security;

public interface IPasswordHasher
{
    (string Hash, string Salt) HashNewPassword(string plainPassword);
    bool Verify(string plainPassword, string storedHash, string storedSalt);
}
