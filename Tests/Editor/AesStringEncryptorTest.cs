using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using SWeetJelly.Serialization.Framework.Serialization;

namespace SWeetJelly.Serialization.Tests.EditMode
{
    public sealed class AesStringEncryptorTest
    {
        private const string Key = "legacy-fixture-key";
        private const string Text = "{\"Id\":73,\"Name\":\"Legacy 한글\"}";
        private readonly AesStringEncryptor encryptor = new AesStringEncryptor();
        private string encrypted;

        [OneTimeSetUp]
        public void CreateCiphertext() => encrypted = encryptor.Encrypt(Text, Key);

        [Test]
        public void NewFormat_RoundTripsUnicodeAndRandomizesEachWrite()
        {
            string next = encryptor.Encrypt(Text, Key);
            Assert.That(encrypted, Does.StartWith("SJENC:1:"));
            Assert.That(next, Is.Not.EqualTo(encrypted));
            Assert.That(encryptor.Decrypt(encrypted, Key), Is.EqualTo(Text));
            Assert.That(encryptor.Decrypt(next, Key), Is.EqualTo(Text));
        }

        [Test]
        public void EmptyPlaintext_UsesAuthenticatedEnvelope()
        {
            string empty = encryptor.Encrypt(string.Empty, Key);
            Assert.That(empty, Does.StartWith("SJENC:1:"));
            Assert.That(encryptor.Decrypt(empty, Key), Is.Empty);
        }

        [TestCase(0)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(-1)]
        public void SaltIvCiphertextOrTagTampering_IsRejected(int offset)
        {
            byte[] bytes = Convert.FromBase64String(encrypted.Substring(8));
            bytes[offset < 0 ? bytes.Length - 1 : offset] ^= 1;
            Assert.Throws<CryptographicException>(() => encryptor.Decrypt("SJENC:1:" + Convert.ToBase64String(bytes), Key));
        }

        [Test]
        public void WrongKey_IsRejected() => Assert.Throws<CryptographicException>(() => encryptor.Decrypt(encrypted, "wrong-key"));

        [TestCase("SJENC:2:AAAA")]
        [TestCase("SJENC:1:AAAA")]
        public void UnknownVersionOrTruncatedEnvelope_IsRejected(string value) =>
            Assert.Throws<CryptographicException>(() => encryptor.Decrypt(value, Key));

        [Test]
        public void MalformedNewPayload_IsNotRetriedAsLegacy() =>
            Assert.Throws<FormatException>(() => encryptor.Decrypt("SJENC:1:not base64!", Key));

        [Test]
        public void LegacyCiphertext_FromVersionOneIsReadable()
        {
            string legacy = File.ReadAllText(SerializationRegressionTest.FixturePath("legacy-aes.txt"));
            Assert.That(encryptor.Decrypt(legacy, Key), Is.EqualTo(Text));
            Assert.That(encryptor.Decrypt(legacy.Replace('+', ' '), Key), Is.EqualTo(Text));
        }

        [Test]
        public void IndependentOpenSslFixture_IsReadable() =>
            Assert.That(encryptor.Decrypt(File.ReadAllText(SerializationRegressionTest.FixturePath("authenticated-v1.txt")), Key), Is.EqualTo(Text));

        [Test]
        public void EmptyKey_IsRejected() => Assert.Throws<ArgumentException>(() => encryptor.Encrypt(Text, ""));
    }
}
