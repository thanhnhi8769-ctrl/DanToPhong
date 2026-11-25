using System;
using System.Security.Cryptography;

namespace DanToPhong.Server
{
    public static class MatKhau
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 600_000;

        public static (string HashBase64, string SaltBase64) BamMatKhau(string plainPassword)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                plainPassword,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public static bool KiemTraMatKhau(string plainPassword, string storedHashBase64, string storedSaltBase64)
        {
            byte[] salt = Convert.FromBase64String(storedSaltBase64);
            byte[] expectedHash = Convert.FromBase64String(storedHashBase64);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                plainPassword,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}