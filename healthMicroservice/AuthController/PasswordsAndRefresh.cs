using System.Security.Cryptography;
using System.Text;

namespace HealthApi.AuthController
{
    public interface IPasswordHasher
    {
        void HashPassword(string password, out byte[] salt, out byte[] hash);
        bool VerifyPassword(string password, byte[] salt, byte[] hash);
    }

    public class PasswordHasher : IPasswordHasher
    {
        public void HashPassword(string password, out byte[] salt, out byte[] hash)
        {
            salt = RandomNumberGenerator.GetBytes(16);
            hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32
            );
        }

        public bool VerifyPassword(string password, byte[] salt, byte[] hash)
        {
            var calc = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32
            );
            return CryptographicOperations.FixedTimeEquals(calc, hash);
        }
    }

    public interface IRefreshTokenGenerator
    {
        string NewRefreshToken();
    }

    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        public string NewRefreshToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
