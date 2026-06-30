namespace SWeetJelly.Serialization.Framework.Bootstrapper
{
    using UnityEngine;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using SWeetJelly.Serialization.Contract;
    using SWeetJelly.Serialization.App;
    using SWeetJelly.Serialization.Framework.Unity;
    [CreateAssetMenu(
        fileName = "Serialization Package Bootstrapper",
        menuName = "SWeetJelly/Bootstrapper/Packages/Serialization")]
    public sealed class SerializationPackageBootstrapperSo : PackageBootstrapperSo
    {
        public override string PackageName => "Serialization";

        public override void Bootstrap(PackageBootstrapContext context)
        {
            IPathProvider pathProvider = new UnityPersistentDataPathProvider();
            IFileStorage fileStorage = new UnityFileStorage();
            ITextSerializer jsonSerializer = new JsonDataSerializer();
            ITextSerializer xmlSerializer = new XmlDataSerializer();
            IBinarySerializer binarySerializer = new BinaryDataSerializer();
            IStringEncryptor encryptor = new AesStringEncryptor(/* config */);

            ISerializationService service = new SerializationService(
                fileStorage,
                pathProvider,
                jsonSerializer,
                xmlSerializer,
                binarySerializer,
                encryptor);

            context.Services.Register<ISerializationService>(service);
        }
    }
}