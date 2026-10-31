using System;
using System.Security.Cryptography;
using System.Text;
using SWeetJelly.Serialization.Contract;

namespace SWeetJelly.Serialization.Framework.Serialization
{
    /// <summary>Authenticated encryption for new data; reads 1.x ciphertext for migration.</summary>
    public sealed class AesStringEncryptor : IStringEncryptor
    {
        private const string Prefix = "SJENC:1:";
        private const int Iterations = 220000;
        private const int SaltSize = 16;
        private const int IvSize = 16;
        private const int TagSize = 32;
        private static readonly byte[] Header = Encoding.ASCII.GetBytes(Prefix);
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public string Encrypt(string plainText, string key)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            RequireKey(key);
            var salt = new byte[SaltSize];
            var iv = new byte[IvSize];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
                random.GetBytes(iv);
            }
            byte[] keys = DeriveKeys(key, salt);
            try
            {
                byte[] encrypted;
                using (var aes = CreateAes(keys, iv))
                using (var transform = aes.CreateEncryptor())
                {
                    byte[] clear = Utf8.GetBytes(plainText);
                    try { encrypted = transform.TransformFinalBlock(clear, 0, clear.Length); }
                    finally { Array.Clear(clear, 0, clear.Length); }
                }
                var payload = new byte[SaltSize + IvSize + encrypted.Length + TagSize];
                Buffer.BlockCopy(salt, 0, payload, 0, SaltSize);
                Buffer.BlockCopy(iv, 0, payload, SaltSize, IvSize);
                Buffer.BlockCopy(encrypted, 0, payload, SaltSize + IvSize, encrypted.Length);
                byte[] tag = Authenticate(keys, payload, payload.Length - TagSize);
                Buffer.BlockCopy(tag, 0, payload, payload.Length - TagSize, TagSize);
                return Prefix + Convert.ToBase64String(payload);
            }
            finally { Array.Clear(keys, 0, keys.Length); }
        }

        public string Decrypt(string cipherText, string key)
        {
            if (cipherText == null) throw new ArgumentNullException(nameof(cipherText));
            RequireKey(key);
            if (!cipherText.StartsWith("SJENC:", StringComparison.Ordinal))
                return LegacyAesDecryptor.Decrypt(cipherText, key);
            if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal))
                throw new CryptographicException("Unsupported encrypted data version.");

            byte[] payload = Convert.FromBase64String(cipherText.Substring(Prefix.Length));
            int ciphertextLength = payload.Length - SaltSize - IvSize - TagSize;
            if (ciphertextLength < 16 || ciphertextLength % 16 != 0)
                throw new CryptographicException("Invalid encrypted data.");
            var salt = new byte[SaltSize];
            var iv = new byte[IvSize];
            Buffer.BlockCopy(payload, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(payload, SaltSize, iv, 0, IvSize);
            byte[] keys = DeriveKeys(key, salt);
            try
            {
                byte[] tag = Authenticate(keys, payload, payload.Length - TagSize);
                if (!CryptographicOperations.FixedTimeEquals(tag,
                    new ReadOnlySpan<byte>(payload, payload.Length - TagSize, TagSize)))
                    throw new CryptographicException("Invalid encrypted data or key.");
                // Authenticate the header, salt, IV and ciphertext before touching CBC padding.
                using var aes = CreateAes(keys, iv);
                using var transform = aes.CreateDecryptor();
                byte[] clear = transform.TransformFinalBlock(payload, SaltSize + IvSize, ciphertextLength);
                try { return Utf8.GetString(clear); }
                finally { Array.Clear(clear, 0, clear.Length); }
            }
            finally { Array.Clear(keys, 0, keys.Length); }
        }

        private static byte[] DeriveKeys(string key, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(key, salt, Iterations, HashAlgorithmName.SHA512);
            return kdf.GetBytes(64);
        }

        private static Aes CreateAes(byte[] keys, byte[] iv)
        {
            var aes = Aes.Create();
            var encryptionKey = new byte[32];
            Buffer.BlockCopy(keys, 0, encryptionKey, 0, encryptionKey.Length);
            try
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;
                return aes;
            }
            catch { aes.Dispose(); throw; }
            finally { Array.Clear(encryptionKey, 0, encryptionKey.Length); }
        }

        private static byte[] Authenticate(byte[] keys, byte[] payload, int count)
        {
            var authenticationKey = new byte[32];
            Buffer.BlockCopy(keys, 32, authenticationKey, 0, authenticationKey.Length);
            try
            {
                using var hmac = new HMACSHA256(authenticationKey);
                var authenticated = new byte[Header.Length + count];
                Buffer.BlockCopy(Header, 0, authenticated, 0, Header.Length);
                Buffer.BlockCopy(payload, 0, authenticated, Header.Length, count);
                return hmac.ComputeHash(authenticated);
            }
            finally { Array.Clear(authenticationKey, 0, authenticationKey.Length); }
        }

        private static void RequireKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Encryption key cannot be empty.", nameof(key));
        }
    }
}
