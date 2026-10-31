using System;
using SWeetJelly.Bootstrapper.Contract;
using SWeetJelly.Bootstrapper.Framework.Unity;
using SWeetJelly.Serialization.App;
using SWeetJelly.Serialization.Contract;
using SWeetJelly.Serialization.Framework.Serialization;
using SWeetJelly.Serialization.Framework.Unity;
using UnityEngine;

namespace SWeetJelly.Serialization.Framework.Bootstrapper
{
    [CreateAssetMenu(fileName = "Serialization Package Bootstrapper", menuName = "SWeetJelly/Bootstrapper/Packages/Serialization")]
    public sealed class SerializationPackageBootstrapperSo : PackageBootstrapperSo
    {
        private ISerializationService service;
        private IServiceRegistry registry;
        public override string PackageName => "Serialization";

        public override void Bootstrap(PackageBootstrapContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (service != null) throw new InvalidOperationException("This bootstrap asset already owns a registration.");
            var created = new SerializationService(new UnityFileStorage(), new UnityPersistentDataPathProvider(),
                new JsonDataSerializer(), new XmlDataSerializer(), stringEncryptor: new AesStringEncryptor());
            context.Services.Register<ISerializationService>(created);
            registry = context.Services;
            service = created;
        }

        public override void Shutdown(PackageBootstrapContext context)
        {
            if (service == null) return;
            if (registry.TryResolve<ISerializationService>(out var registered) && ReferenceEquals(registered, service))
                registry.Unregister<ISerializationService>();
            registry = null;
            service = null;
        }
    }
}
