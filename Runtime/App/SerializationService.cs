using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SWeetJelly.Serialization.Contract;

namespace SWeetJelly.Serialization.App
{
    public sealed class SerializationService : ISerializationService
    {
        private readonly IFileStorage fileStorage;
        private readonly IPathProvider pathProvider;

        private readonly ITextSerializer jsonSerializer;
        private readonly ITextSerializer xmlSerializer;
        private readonly IBinarySerializer binarySerializer;

        private readonly IStringEncryptor stringEncryptor;

        public SerializationService(
            IFileStorage fileStorage,
            IPathProvider pathProvider,
            ITextSerializer jsonSerializer,
            ITextSerializer xmlSerializer,
            IBinarySerializer binarySerializer = null,
            IStringEncryptor stringEncryptor = null)
        {
            this.fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            this.pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            this.jsonSerializer = jsonSerializer;
            this.xmlSerializer = xmlSerializer;
            this.binarySerializer = binarySerializer;
            this.stringEncryptor = stringEncryptor;
        }

        public Task SaveToPersistentDataPathAsync<T>(
            T value,
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default)
        {
            string path = pathProvider.Combine(
                pathProvider.PersistentDataPath,
                fileName);

            return SaveToPathAsync(value, path, format, ct: ct);
        }

        public async Task SaveToPathAsync<T>(
            T value,
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default)
        {
            byte[] bytes = SerializeToBytes(value, format, secureKey);

            await fileStorage.WriteBytesAsync(path, bytes, ct);
        }

        public Task<T> LoadFromPersistentDataPathAsync<T>(
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default)
        {
            string path = pathProvider.Combine(
                pathProvider.PersistentDataPath,
                fileName);

            return LoadFromPathAsync<T>(path, format, ct: ct);
        }

        public async Task<T> LoadFromPathAsync<T>(
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default)
        {
            byte[] bytes = await fileStorage.ReadBytesAsync(path, ct);

            return DeserializeFromBytes<T>(bytes, format, secureKey);
        }

        public bool ExistsAtPersistentDataPath(string fileName)
        {
            string path = pathProvider.Combine(
                pathProvider.PersistentDataPath,
                fileName);

            return fileStorage.Exists(path);
        }

        public void DeleteAtPersistentDataPath(string fileName)
        {
            string path = pathProvider.Combine(
                pathProvider.PersistentDataPath,
                fileName);

            fileStorage.Delete(path);
        }

        public string SerializeToText<T>(T value, SerializationFormat format, string secureKey = "")
        {
            switch (format)
            {
                case SerializationFormat.Xml:
                    RequireXmlSerializer();
                    return Encrypt(xmlSerializer.Serialize(value), secureKey);

                case SerializationFormat.Json:
                    RequireJsonSerializer();
                    return Encrypt(jsonSerializer.Serialize(value), secureKey);

                case SerializationFormat.Binary:
                    throw new NotSupportedException("Binary format does not support text serialization.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        public string SerializeToText(object value, Type valueType, SerializationFormat format, string secureKey = "")
        {
            if (valueType == null)
                throw new ArgumentNullException(nameof(valueType));

            switch (format)
            {
                case SerializationFormat.Xml:
                    RequireXmlSerializer();
                    return Encrypt(xmlSerializer.Serialize(value, valueType), secureKey);

                case SerializationFormat.Json:
                    RequireJsonSerializer();
                    return Encrypt(jsonSerializer.Serialize(value, valueType), secureKey);

                case SerializationFormat.Binary:
                    throw new NotSupportedException("Binary format does not support text serialization.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        public T DeserializeFromText<T>(string text, SerializationFormat format, string secureKey = "")
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Serialized text cannot be empty.", nameof(text));

            switch (format)
            {
                case SerializationFormat.Xml:
                    RequireXmlSerializer();
                    return xmlSerializer.Deserialize<T>(Decrypt(text, secureKey));

                case SerializationFormat.Json:
                    RequireJsonSerializer();
                    return jsonSerializer.Deserialize<T>(Decrypt(text, secureKey));

                case SerializationFormat.Binary:
                    throw new NotSupportedException("Binary format does not support text deserialization.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        public object DeserializeFromText(string text, Type valueType, SerializationFormat format, string secureKey = "")
        {
            if (valueType == null)
                throw new ArgumentNullException(nameof(valueType));

            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Serialized text cannot be empty.", nameof(text));

            switch (format)
            {
                case SerializationFormat.Xml:
                    RequireXmlSerializer();
                    return xmlSerializer.Deserialize(
                        Decrypt(text, secureKey), valueType);

                case SerializationFormat.Json:
                    RequireJsonSerializer();
                    return jsonSerializer.Deserialize(
                        Decrypt(text, secureKey), valueType);

                case SerializationFormat.Binary:
                    throw new NotSupportedException("Binary format does not support text deserialization.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private byte[] SerializeToBytes<T>(
            T value,
            SerializationFormat format,
            string secureKey)
        {
            switch (format)
            {
                case SerializationFormat.Binary:
                    RequireBinarySerializer();
                    return binarySerializer.Serialize(value);

                case SerializationFormat.Xml:
                    RequireXmlSerializer();
                    return Encoding.UTF8.GetBytes(xmlSerializer.Serialize(value));

                case SerializationFormat.Json:
                    RequireJsonSerializer();

                    string json = jsonSerializer.Serialize(value);

                    if (!string.IsNullOrEmpty(secureKey))
                    {
                        if (stringEncryptor == null)
                            throw new InvalidOperationException("String encryptor is not assigned.");

                        json = stringEncryptor.Encrypt(json, secureKey);
                    }

                    return Encoding.UTF8.GetBytes(json);

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private T DeserializeFromBytes<T>(
            byte[] bytes,
            SerializationFormat format,
            string secureKey)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Serialized bytes cannot be empty.", nameof(bytes));

            switch (format)
            {
                case SerializationFormat.Binary:
                    RequireBinarySerializer();
                    return binarySerializer.Deserialize<T>(bytes);

                case SerializationFormat.Xml:
                    RequireXmlSerializer();
                    return xmlSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes));

                case SerializationFormat.Json:
                    RequireJsonSerializer();

                    string json = Encoding.UTF8.GetString(bytes);

                    if (!string.IsNullOrEmpty(secureKey))
                    {
                        if (stringEncryptor == null)
                            throw new InvalidOperationException("String encryptor is not assigned.");

                        json = stringEncryptor.Decrypt(json, secureKey);
                    }

                    return jsonSerializer.Deserialize<T>(json);

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private string Encrypt(string text, string secureKey)
        {
            if (string.IsNullOrEmpty(secureKey))
                return text;

            if (stringEncryptor == null)
                throw new InvalidOperationException("String encryptor is not assigned.");

            return stringEncryptor.Encrypt(text, secureKey);
        }

        private string Decrypt(string text, string secureKey)
        {
            if (string.IsNullOrEmpty(secureKey))
                return text;

            if (stringEncryptor == null)
                throw new InvalidOperationException("String encryptor is not assigned.");

            return stringEncryptor.Decrypt(text, secureKey);
        }

        private void RequireJsonSerializer()
        {
            if (jsonSerializer == null)
                throw new InvalidOperationException("Json serializer is not assigned.");
        }

        private void RequireXmlSerializer()
        {
            if (xmlSerializer == null)
                throw new InvalidOperationException("Xml serializer is not assigned.");
        }

        private void RequireBinarySerializer()
        {
            if (binarySerializer == null)
                throw new InvalidOperationException("Binary serializer is not assigned.");
        }
    }
}