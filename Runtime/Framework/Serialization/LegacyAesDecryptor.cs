using System;
using System.Security.Cryptography;
using System.Text;

namespace SWeetJelly.Serialization.Framework.Serialization
{
    // Read-only compatibility with 1.x. This format has no authentication tag.
    internal static class LegacyAesDecryptor
    {
        private static readonly byte[] Salt =
        {
            0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65,
            0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        };

        public static string Decrypt(string text, string key)
        {
            if (text.Length == 0) return string.Empty;
            byte[] bytes = Convert.FromBase64String(text.Replace(" ", "+"));
            using var kdf = new Rfc2898DeriveBytes(key, Salt, 1000, HashAlgorithmName.SHA1);
            using var aes = Aes.Create();
            aes.Key = kdf.GetBytes(32);
            aes.IV = kdf.GetBytes(16);
            using var decryptor = aes.CreateDecryptor();
            byte[] clear = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            try { return Encoding.Unicode.GetString(clear); }
            finally { Array.Clear(clear, 0, clear.Length); }
        }
    }
}
