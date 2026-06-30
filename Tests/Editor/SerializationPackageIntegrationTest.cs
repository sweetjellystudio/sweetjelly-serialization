namespace SWeetJelly.Serialization.Tests.EditMode
{
    using NUnit.Framework;
    using SWeetJelly.Bootstrapper.App;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using SWeetJelly.Serialization.Contract;
    using SWeetJelly.Serialization.Framework.Bootstrapper;
    using UnityEngine;

    public sealed class SerializationPackageIntegrationTest
    {
        [Test]
        public void SerializationPackageBootstrapperSo_RegistersSerializationService()
        {
            var holder = new GameObject("Serialization Test Holder");
            var module = ScriptableObject.CreateInstance<SerializationPackageBootstrapperSo>();

            try
            {
                var registry = new ServiceRegistry();
                var context = new PackageBootstrapContext(holder, null, null, registry);

                module.Bootstrap(context);

                Assert.That(registry.TryResolve<ISerializationService>(out var service), Is.True);
                Assert.That(service, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(module);
                Object.DestroyImmediate(holder);
            }
        }
    }
}
