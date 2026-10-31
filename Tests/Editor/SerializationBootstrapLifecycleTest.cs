using System;
using NUnit.Framework;
using SWeetJelly.Bootstrapper.App;
using SWeetJelly.Bootstrapper.Framework.Unity;
using SWeetJelly.Serialization.Contract;
using SWeetJelly.Serialization.Framework.Bootstrapper;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SWeetJelly.Serialization.Tests.EditMode
{
    public sealed class SerializationBootstrapLifecycleTest
    {
        private GameObject holder;
        private SerializationPackageBootstrapperSo module;
        private ServiceRegistry registry;
        private PackageBootstrapContext context;
        [SetUp]
        public void SetUp()
        {
            holder = new GameObject("Serialization lifecycle fixture");
            module = ScriptableObject.CreateInstance<SerializationPackageBootstrapperSo>();
            registry = new ServiceRegistry();
            context = new PackageBootstrapContext(holder, null, null, registry);
        }
        [TearDown]
        public void TearDown()
        {
            module.Shutdown(context);
            Object.DestroyImmediate(module);
            Object.DestroyImmediate(holder);
        }
        [Test]
        public void ShutdownIsIdempotent_AndAllowsRebootstrap()
        {
            module.Bootstrap(context);
            Assert.Throws<InvalidOperationException>(() => module.Bootstrap(context));
            module.Shutdown(context);
            module.Shutdown(context);
            Assert.That(registry.TryResolve<ISerializationService>(out _), Is.False);
            module.Bootstrap(context);
            Assert.That(registry.TryResolve<ISerializationService>(out _), Is.True);
        }
        [Test]
        public void FailedRegistration_PreservesOtherOwnerAndCanRetry()
        {
            var other = ScriptableObject.CreateInstance<SerializationPackageBootstrapperSo>();
            try
            {
                other.Bootstrap(context);
                var original = registry.Resolve<ISerializationService>();
                Assert.Throws<ArgumentException>(() => module.Bootstrap(context));
                module.Shutdown(context);
                Assert.That(registry.Resolve<ISerializationService>(), Is.SameAs(original));
                other.Shutdown(context);
                module.Bootstrap(context);
            }
            finally { other.Shutdown(context); Object.DestroyImmediate(other); }
        }
        [Test]
        public void ShutdownDoesNotUnregisterAReplacement()
        {
            var other = ScriptableObject.CreateInstance<SerializationPackageBootstrapperSo>();
            var otherRegistry = new ServiceRegistry();
            var otherContext = new PackageBootstrapContext(holder, null, null, otherRegistry);
            try
            {
                module.Bootstrap(context);
                other.Bootstrap(otherContext);
                var replacement = otherRegistry.Resolve<ISerializationService>();
                registry.Unregister<ISerializationService>();
                registry.Register(replacement);
                module.Shutdown(context);
                Assert.That(registry.Resolve<ISerializationService>(), Is.SameAs(replacement));
            }
            finally { other.Shutdown(otherContext); Object.DestroyImmediate(other); }
        }
    }
}
