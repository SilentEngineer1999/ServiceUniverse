using System;
using System.Text;
using System.Security.Cryptography;

namespace backendServices.AuthController
{
    public class PasswordManager
    {
        // use during signup
        public (string Hash, byte[] Salt) HashPassword(string password)
        {
            // generate salt
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            // hash with salt
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );

            return (Convert.ToBase64String(hash), salt); // store hash as Base64 string, salt as raw bytes
        }

        // used during login
        public bool VerifyPassword(string password, string storedHash, byte[] storedSalt)
        {
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                storedSalt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );

            return Convert.ToBase64String(hash) == storedHash;
        }
    }
}
