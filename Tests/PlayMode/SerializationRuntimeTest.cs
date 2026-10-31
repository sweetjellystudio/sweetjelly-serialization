using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SWeetJelly.Bootstrapper.App;
using SWeetJelly.Bootstrapper.Framework.Unity;
using SWeetJelly.Serialization.App;
using SWeetJelly.Serialization.Contract;
using SWeetJelly.Serialization.Framework.Bootstrapper;
using SWeetJelly.Serialization.Framework.Serialization;
using SWeetJelly.Serialization.Framework.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SWeetJelly.Serialization.Tests.PlayMode
{
    public sealed class SerializationRuntimeTest
    {
        [UnityTest] public IEnumerator JsonFileRoundTrip() => FileRoundTrip(SerializationFormat.Json, "");
        [UnityTest] public IEnumerator XmlFileRoundTrip() => FileRoundTrip(SerializationFormat.Xml, "");
        [UnityTest] public IEnumerator EncryptedXmlFileRoundTrip() => FileRoundTrip(SerializationFormat.Xml, "runtime-test-key");

        private static IEnumerator FileRoundTrip(SerializationFormat format, string key)
        {
            string path = Path.Combine(Application.temporaryCachePath, "sweetjelly-serialization-test-" + Guid.NewGuid().ToString("N"));
            var storage = new UnityFileStorage();
            var service = new SerializationService(storage, new UnityPersistentDataPathProvider(), new JsonDataSerializer(), new XmlDataSerializer(), stringEncryptor: new AesStringEncryptor());
            try
            {
                var save = service.SaveToPathAsync(new RuntimeData { Id = 42, Name = "Runtime 한글" }, path, format, key);
                while (!save.IsCompleted) yield return null;
                Assert.That(save.GetAwaiter().GetResult().IsSuccess, Is.True);
                var load = service.LoadFromPathAsync<RuntimeData>(path, format, key);
                while (!load.IsCompleted) yield return null;
                var result = load.GetAwaiter().GetResult();
                Assert.That(result.IsSuccess, Is.True, result.Error.ToString());
                Assert.That(result.Value.Name, Is.EqualTo("Runtime 한글"));
                if (key.Length != 0) Assert.That(File.ReadAllText(path), Does.StartWith("SJENC:1:"));
            }
            finally { storage.Delete(path); }
            Assert.That(storage.Exists(path), Is.False);
        }

        [UnityTest]
        public IEnumerator BootstrapRegistrationLivesUntilShutdown()
        {
            var holder = new GameObject("Serialization runtime fixture");
            var module = ScriptableObject.CreateInstance<SerializationPackageBootstrapperSo>();
            var registry = new ServiceRegistry();
            var context = new PackageBootstrapContext(holder, null, null, registry);
            try
            {
                module.Bootstrap(context);
                yield return null;
                Assert.That(registry.TryResolve<ISerializationService>(out var service), Is.True);
                Assert.That(service.SerializeToText(7, SerializationFormat.Binary).IsFailure, Is.True);
                module.Shutdown(context);
                yield return null;
                Assert.That(registry.TryResolve<ISerializationService>(out _), Is.False);
            }
            finally { module.Shutdown(context); Object.Destroy(module); Object.Destroy(holder); }
        }
    }

    [Serializable]
    public sealed class RuntimeData
    {
        public int Id;
        public string Name;
    }
}
