namespace SWeetJelly.Serialization.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using SWeetJelly.Serialization.App;
    using SWeetJelly.Serialization.Contract;

    public sealed class SerializationServiceTest
    {
        [Test]
        public void SerializeToText_WithJson_CanRoundTripValue()
        {
            var service = CreateService();
            var value = new SerializationTestData
            {
                Id = 7,
                Name = "Sweet"
            };

            var text = service.SerializeToText(value, SerializationFormat.Json);
            var loaded = service.DeserializeFromText<SerializationTestData>(text.Value, SerializationFormat.Json);

            Assert.That(text.IsSuccess, Is.True);
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.Id, Is.EqualTo(value.Id));
            Assert.That(loaded.Value.Name, Is.EqualTo(value.Name));
        }

        [Test]
        public void SerializeToText_WithXml_CanRoundTripValue()
        {
            var service = CreateService();
            var value = new SerializationTestData
            {
                Id = 13,
                Name = "Jelly"
            };

            var text = service.SerializeToText(value, SerializationFormat.Xml);
            var loaded = service.DeserializeFromText<SerializationTestData>(text.Value, SerializationFormat.Xml);

            Assert.That(text.IsSuccess, Is.True);
            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.Id, Is.EqualTo(value.Id));
            Assert.That(loaded.Value.Name, Is.EqualTo(value.Name));
        }

        [Test]
        public void SaveToPersistentDataPathAsync_WithJson_WritesAndLoadsValue()
        {
            var storage = new MemoryFileStorage();
            var pathProvider = new FakePathProvider("root");
            var service = CreateService(storage, pathProvider);
            var value = new SerializationTestData
            {
                Id = 21,
                Name = "Package"
            };

            var saveResult = service.SaveToPersistentDataPathAsync(value, "data.json", SerializationFormat.Json)
                .GetAwaiter()
                .GetResult();

            Assert.That(saveResult.IsSuccess, Is.True);
            Assert.That(storage.Exists("root/data.json"), Is.True);

            var loaded = service.LoadFromPersistentDataPathAsync<SerializationTestData>(
                    "data.json",
                    SerializationFormat.Json)
                .GetAwaiter()
                .GetResult();

            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.Id, Is.EqualTo(value.Id));
            Assert.That(loaded.Value.Name, Is.EqualTo(value.Name));
        }

        [Test]
        public void SerializeToText_WithSecureKey_EncryptsAndDecryptsJson()
        {
            var service = CreateService();
            var value = new SerializationTestData
            {
                Id = 34,
                Name = "Secure"
            };

            var text = service.SerializeToText(value, SerializationFormat.Json, "test-key");

            Assert.That(text.IsSuccess, Is.True);
            Assert.That(text.Value, Does.Not.Contain("Secure"));

            var loaded = service.DeserializeFromText<SerializationTestData>(
                text.Value,
                SerializationFormat.Json,
                "test-key");

            Assert.That(loaded.IsSuccess, Is.True);
            Assert.That(loaded.Value.Id, Is.EqualTo(value.Id));
            Assert.That(loaded.Value.Name, Is.EqualTo(value.Name));
        }

        [Test]
        public void SerializeToText_WithBinary_ReturnsFailure()
        {
            var service = CreateService();

            var result = service.SerializeToText(new SerializationTestData(), SerializationFormat.Binary);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(SerializationErrors.BinaryTextUnsupported));
        }

        private static SerializationService CreateService(
            IFileStorage fileStorage = null,
            IPathProvider pathProvider = null)
        {
            return new SerializationService(
                fileStorage ?? new MemoryFileStorage(),
                pathProvider ?? new FakePathProvider("root"),
                new JsonDataSerializer(),
                new XmlDataSerializer(),
                new BinaryDataSerializer(),
                new AesStringEncryptor());
        }

        private sealed class FakePathProvider : IPathProvider
        {
            public FakePathProvider(string persistentDataPath)
            {
                PersistentDataPath = persistentDataPath;
            }

            public string PersistentDataPath { get; }

            public string Combine(string directory, string fileName)
            {
                return $"{directory}/{fileName}";
            }
        }

        private sealed class MemoryFileStorage : IFileStorage
        {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

            public bool Exists(string path)
            {
                return files.ContainsKey(path);
            }

            public Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                if (!files.TryGetValue(path, out var bytes))
                    throw new FileNotFoundException(path);

                return Task.FromResult(bytes);
            }

            public Task WriteBytesAsync(string path, byte[] bytes, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                files[path] = bytes;
                return Task.CompletedTask;
            }

            public void Delete(string path)
            {
                files.Remove(path);
            }
        }
    }

    public sealed class SerializationTestData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
