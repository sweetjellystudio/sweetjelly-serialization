namespace SWeetJelly.Serialization.InitialiserIntegration
{
    using UnityEngine;
    using SWeetJelly.Initialiser.Contract;
    using SWeetJelly.Initialiser.Framework;
    using SWeetJelly.Serialization.Contract;
    using SWeetJelly.Serialization.App;
    using SWeetJelly.Serialization.Framework;

    [RegisterModule("Serialization")]
    [CreateAssetMenu(
        fileName = "Serialization Init Module",
        menuName = "SWeetJelly/Initialiser/Modules/Serialization")]
    public sealed class SerializationInitModule : InitModuleSo
    {
        public override string ModuleName => "Serialization";

        public override void Install(InitModuleInstallContext context)
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