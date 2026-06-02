using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SWeetJelly.Serialization.Contract;

namespace SWeetJelly.Serialization.App
{
    public sealed class AesStringEncryptor : IStringEncryptor
    {
        private static readonly byte[] Salt =
        {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65,
            0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        };

        public string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Encryption key cannot be empty.", nameof(key));

            byte[] clearBytes = Encoding.Unicode.GetBytes(plainText);

            using Aes encryptor = Aes.Create();
            using var pdb = new Rfc2898DeriveBytes(key, Salt);

            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using var memoryStream = new MemoryStream();

            using (var cryptoStream = new CryptoStream(
                memoryStream,
                encryptor.CreateEncryptor(),
                CryptoStreamMode.Write))
            {
                cryptoStream.Write(clearBytes, 0, clearBytes.Length);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Encryption key cannot be empty.", nameof(key));

            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using Aes encryptor = Aes.Create();
            using var pdb = new Rfc2898DeriveBytes(key, Salt);

            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using var memoryStream = new MemoryStream();

            using (var cryptoStream = new CryptoStream(
                memoryStream,
                encryptor.CreateDecryptor(),
                CryptoStreamMode.Write))
            {
                cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
            }

            return Encoding.Unicode.GetString(memoryStream.ToArray());
        }
    }
}