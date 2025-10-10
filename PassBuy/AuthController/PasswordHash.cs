using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Swift;
using System.Text;
using System.Security.Cryptography;

namespace PassBuy.AuthController
{
    public class PasswordManager
    {
        // salt used for hashing password
        private readonly byte[] salt = RandomNumberGenerator.GetBytes(16);

        //use during signup
        public string HashPassword(string password)
        {
            // Pbkdf2 for hashing
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32
            );

            return (Convert.ToBase64String(hash), salt); // store hash as Base64 string, salt as raw bytes
        }

        // used during login to verify password
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