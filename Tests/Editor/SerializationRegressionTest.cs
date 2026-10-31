using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SWeetJelly.Serialization.App;
using SWeetJelly.Serialization.Contract;
using SWeetJelly.Serialization.Framework.Serialization;
using UnityEditor.PackageManager;

namespace SWeetJelly.Serialization.Tests.EditMode
{
    public sealed class SerializationRegressionTest
    {
        public static string FixturePath(string file) => Path.Combine(
            PackageInfo.FindForAssembly(typeof(SerializationService).Assembly).resolvedPath, "Tests", "Fixtures", file);

        private static SerializationService Service(Storage storage = null, IPathProvider paths = null,
            IBinarySerializer binary = null, IStringEncryptor encryptor = null) => new SerializationService(
                storage ?? new Storage(), paths ?? new Paths(), new JsonDataSerializer(), new XmlDataSerializer(), binary, encryptor ?? new AesStringEncryptor());

        [TestCase(SerializationFormat.Json, "legacy.json")]
        [TestCase(SerializationFormat.Xml, "legacy.xml")]
        public void LegacyData_IsReadableWithSameModel(SerializationFormat format, string fixture)
        {
            var result = Service().DeserializeFromText<SerializationTestData>(File.ReadAllText(FixturePath(fixture)), format);
            Assert.That(result.IsSuccess, Is.True, result.Error.ToString());
            Assert.That(result.Value.Id, Is.EqualTo(73));
            Assert.That(result.Value.Name, Is.EqualTo("Legacy 한글"));
        }

        [TestCase(SerializationFormat.Json)]
        [TestCase(SerializationFormat.Xml)]
        public void RuntimeTypeOverloads_RoundTrip(SerializationFormat format)
        {
            var service = Service();
            var text = service.SerializeToText(new SerializationTestData { Id = 17, Name = "Typed" }, typeof(SerializationTestData), format);
            Assert.That(text.IsSuccess, Is.True);
            var result = service.DeserializeFromText(text.Value, typeof(SerializationTestData), format);
            Assert.That(result.IsSuccess, Is.True, result.Error.ToString());
            Assert.That(((SerializationTestData)result.Value).Id, Is.EqualTo(17));
        }

        [TestCase(SerializationFormat.Json)]
        [TestCase(SerializationFormat.Xml)]
        public async Task EncryptedFiles_RoundTripAndRejectPlaintextFallback(SerializationFormat format)
        {
            var storage = new Storage();
            var service = Service(storage);
            var saved = await service.SaveToPathAsync(new SerializationTestData { Id = 9 }, "file", format, "key");
            Assert.That(saved.IsSuccess, Is.True, saved.Error.ToString());
            Assert.That(Encoding.UTF8.GetString(storage.Bytes), Does.StartWith("SJENC:1:"));
            var loaded = await service.LoadFromPathAsync<SerializationTestData>("file", format, "key");
            Assert.That(loaded.IsSuccess, Is.True, loaded.Error.ToString());
            Assert.That(loaded.Value.Id, Is.EqualTo(9));
            storage.Bytes = Encoding.UTF8.GetBytes(service.SerializeToText(new SerializationTestData(), format).Value);
            var plaintext = await service.LoadFromPathAsync<SerializationTestData>("file", format, "key");
            Assert.That(plaintext.IsFailure, Is.True);
        }

        [Test]
        public async Task LegacyXmlFile_WithPreviouslyIgnoredKeyReadsWithoutKey()
        {
            byte[] original = File.ReadAllBytes(FixturePath("legacy.xml"));
            var storage = new Storage { Bytes = original };
            var loaded = await Service(storage).LoadFromPathAsync<SerializationTestData>("legacy", SerializationFormat.Xml);
            Assert.That(loaded.IsSuccess, Is.True, loaded.Error.ToString());
            Assert.That(storage.Bytes, Is.SameAs(original));
            Assert.That(storage.Writes, Is.Zero);
        }

        [Test]
        public async Task LegacyEncryptedFile_IsReadWithoutRewriting()
        {
            var storage = new Storage { Bytes = Encoding.UTF8.GetBytes(File.ReadAllText(FixturePath("legacy-aes.txt"))) };
            var result = await Service(storage).LoadFromPathAsync<SerializationTestData>("legacy", SerializationFormat.Json, "legacy-fixture-key");
            Assert.That(result.IsSuccess, Is.True, result.Error.ToString());
            Assert.That(result.Value.Id, Is.EqualTo(73));
            Assert.That(storage.Writes, Is.Zero);
        }

        [Test]
        public async Task BinaryNeedsCustomImplementation_AndRejectsStringKey()
        {
            var storage = new Storage();
            var missing = await Service(storage).SaveToPathAsync(73, "file", SerializationFormat.Binary);
            Assert.That(missing.Error.Code, Is.EqualTo(SerializationErrors.MissingBinarySerializer.Code));
            var keyed = await Service(storage, binary: new Binary()).SaveToPathAsync(73, "file", SerializationFormat.Binary, "key");
            Assert.That(keyed.Error.Code, Is.EqualTo(SerializationErrors.BinaryEncryptionUnsupported.Code));
            Assert.That(storage.Writes, Is.Zero);
            var service = Service(storage, binary: new Binary());
            Assert.That((await service.SaveToPathAsync(73, "file", SerializationFormat.Binary)).IsSuccess, Is.True);
            Assert.That((await service.LoadFromPathAsync<int>("file", SerializationFormat.Binary)).Value, Is.EqualTo(73));
        }

        [Test]
        public void EnumNumbers_AreStable()
        {
            Assert.That((int)SerializationFormat.Binary, Is.Zero);
            Assert.That((int)SerializationFormat.Xml, Is.EqualTo(1));
            Assert.That((int)SerializationFormat.Json, Is.EqualTo(2));
        }

        [Test]
        public void MissingDependencies_HaveSpecificErrors()
        {
            var service = new SerializationService(new Storage(), new Paths(), null, null);
            Assert.That(service.SerializeToText(1, SerializationFormat.Json).Error.Code, Is.EqualTo(SerializationErrors.MissingJsonSerializer.Code));
            Assert.That(service.DeserializeFromText<int>("1", SerializationFormat.Xml).Error.Code, Is.EqualTo(SerializationErrors.MissingXmlSerializer.Code));
            var noEncryptor = new SerializationService(new Storage(), new Paths(), new JsonDataSerializer(), new XmlDataSerializer());
            Assert.That(noEncryptor.SerializeToText(1, SerializationFormat.Json, "key").Error.Code, Is.EqualTo(SerializationErrors.MissingStringEncryptor.Code));
        }

        [TestCase(SerializationFormat.Json)]
        [TestCase(SerializationFormat.Xml)]
        public void MalformedData_ReturnsFailure(SerializationFormat format) =>
            Assert.That(Service().DeserializeFromText<SerializationTestData>("not serialized data", format).IsFailure, Is.True);

        [Test]
        public void XmlProhibitsDtd_AndUsesUtf8Declaration()
        {
            var service = Service();
            Assert.That(service.SerializeToText(new SerializationTestData(), SerializationFormat.Xml).Value, Does.Contain("encoding=\"utf-8\""));
            Assert.That(service.DeserializeFromText<SerializationTestData>("<!DOCTYPE a [<!ENTITY x 'value'>]><a>&x;</a>", SerializationFormat.Xml).IsFailure, Is.True);
        }

        [Test]
        public async Task PreCancelledRequests_DoNotTouchPathsOrStorage()
        {
            var storage = new Storage();
            var paths = new Paths();
            var service = Service(storage, paths);
            var ct = new CancellationToken(true);
            Assert.CatchAsync<OperationCanceledException>(async () => await service.SaveToPersistentDataPathAsync(1, "file", SerializationFormat.Json, ct));
            Assert.CatchAsync<OperationCanceledException>(async () => await service.LoadFromPersistentDataPathAsync<int>("file", SerializationFormat.Json, ct));
            Assert.CatchAsync<OperationCanceledException>(async () => await service.SaveToPathAsync(1, "file", SerializationFormat.Json, ct: ct));
            Assert.That(paths.Calls, Is.Zero);
            Assert.That(storage.Reads + storage.Writes, Is.Zero);
            await Task.CompletedTask;
        }

        [Test]
        public async Task PathAndIoErrors_AreResultsButCancellationPropagates()
        {
            var storage = new Storage { Failure = new IOException("fixture") };
            var service = Service(storage);
            Assert.That((await service.SaveToPathAsync(1, "file", SerializationFormat.Json)).Error.Code, Is.EqualTo("Serialization.WriteFailed"));
            Assert.That((await service.LoadFromPathAsync<int>("file", SerializationFormat.Json)).Error.Code, Is.EqualTo("Serialization.ReadFailed"));
            storage.Failure = new DirectoryNotFoundException();
            Assert.That((await service.LoadFromPathAsync<int>("file", SerializationFormat.Json)).Error.Code, Is.EqualTo("Serialization.FileNotFound"));
            storage.Failure = new OperationCanceledException();
            Assert.CatchAsync<OperationCanceledException>(async () => await service.SaveToPathAsync(1, "file", SerializationFormat.Json));
            Assert.CatchAsync<OperationCanceledException>(async () => await service.LoadFromPathAsync<int>("file", SerializationFormat.Json));
            var brokenPath = Service(paths: new Paths { Failure = new ArgumentException("fixture") });
            Assert.That((await brokenPath.SaveToPersistentDataPathAsync(1, "file", SerializationFormat.Json)).Error.Code, Is.EqualTo("Serialization.PathFailed"));
            Assert.That((await brokenPath.LoadFromPersistentDataPathAsync<int>("file", SerializationFormat.Json)).Error.Code, Is.EqualTo("Serialization.PathFailed"));
        }

        [Test]
        public async Task InvalidFormatAndInput_DoNotWrite()
        {
            var storage = new Storage();
            var service = Service(storage);
            Assert.That((await service.SaveToPathAsync(1, "file", (SerializationFormat)99)).Error.Code, Is.EqualTo("Serialization.UnsupportedFormat"));
            Assert.That(service.DeserializeFromText<int>(" ", SerializationFormat.Json).Error.Code, Is.EqualTo("Serialization.EmptyText"));
            Assert.That(service.SerializeToText(1, null, SerializationFormat.Json).Error.Code, Is.EqualTo("Serialization.InvalidValueType"));
            Assert.That(storage.Writes, Is.Zero);
        }

        private sealed class Binary : IBinarySerializer
        {
            public byte[] Serialize<T>(T value) => BitConverter.GetBytes((int)(object)value);
            public T Deserialize<T>(byte[] bytes) => (T)(object)BitConverter.ToInt32(bytes, 0);
        }
        private sealed class Paths : IPathProvider
        {
            public int Calls;
            public Exception Failure;
            public string PersistentDataPath => "root";
            public string Combine(string directory, string fileName) { Calls++; if (Failure != null) throw Failure; return directory + "/" + fileName; }
        }
        private sealed class Storage : IFileStorage
        {
            public byte[] Bytes;
            public int Reads, Writes;
            public Exception Failure;
            public bool Exists(string path) => Bytes != null;
            public void Delete(string path) => Bytes = null;
            public Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
            {
                Reads++; if (Failure != null) throw Failure; if (Bytes == null) throw new FileNotFoundException(); return Task.FromResult(Bytes);
            }
            public Task WriteBytesAsync(string path, byte[] bytes, CancellationToken ct = default)
            {
                Writes++; if (Failure != null) throw Failure; Bytes = bytes; return Task.CompletedTask;
            }
        }
    }
}
