using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SWeetJelly.Serialization.Contract;
using SWeetJelly.Shared.Result;

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

        public Task<Result> SaveToPersistentDataPathAsync<T>(
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

        public async Task<Result> SaveToPathAsync<T>(
            T value,
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default)
        {
            Result<byte[]> bytesResult = SerializeToBytes(value, format, secureKey);

            if (bytesResult.IsFailure)
                return Result.Failure(bytesResult.Error);

            try
            {
                await fileStorage.WriteBytesAsync(path, bytesResult.Value, ct);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return Result.Failure(SerializationErrors.WriteFailed(path, exception));
            }
        }

        public Task<Result<T>> LoadFromPersistentDataPathAsync<T>(
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default)
        {
            string path = pathProvider.Combine(
                pathProvider.PersistentDataPath,
                fileName);

            return LoadFromPathAsync<T>(path, format, ct: ct);
        }

        public async Task<Result<T>> LoadFromPathAsync<T>(
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default)
        {
            try
            {
                byte[] bytes = await fileStorage.ReadBytesAsync(path, ct);

                return DeserializeFromBytes<T>(bytes, format, secureKey);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FileNotFoundException)
            {
                return Result<T>.Failure(SerializationErrors.FileNotFound(path));
            }
            catch (Exception exception)
            {
                return Result<T>.Failure(SerializationErrors.ReadFailed(path, exception));
            }
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

        public Result<string> SerializeToText<T>(T value, SerializationFormat format, string secureKey = "")
        {
            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        RequireXmlSerializer();
                        return Result<string>.Success(Encrypt(xmlSerializer.Serialize(value), secureKey));

                    case SerializationFormat.Json:
                        RequireJsonSerializer();
                        return Result<string>.Success(Encrypt(jsonSerializer.Serialize(value), secureKey));

                    case SerializationFormat.Binary:
                        return Result<string>.Failure(SerializationErrors.BinaryTextUnsupported);

                    default:
                        return Result<string>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (Exception exception)
            {
                return Result<string>.Failure(SerializationErrors.SerializeFailed(format, exception));
            }
        }

        public Result<string> SerializeToText(object value, Type valueType, SerializationFormat format, string secureKey = "")
        {
            if (valueType == null)
                return Result<string>.Failure(SerializationErrors.InvalidValueType());

            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        RequireXmlSerializer();
                        return Result<string>.Success(Encrypt(xmlSerializer.Serialize(value, valueType), secureKey));

                    case SerializationFormat.Json:
                        RequireJsonSerializer();
                        return Result<string>.Success(Encrypt(jsonSerializer.Serialize(value, valueType), secureKey));

                    case SerializationFormat.Binary:
                        return Result<string>.Failure(SerializationErrors.BinaryTextUnsupported);

                    default:
                        return Result<string>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (Exception exception)
            {
                return Result<string>.Failure(SerializationErrors.SerializeFailed(format, exception));
            }
        }

        public Result<T> DeserializeFromText<T>(string text, SerializationFormat format, string secureKey = "")
        {
            if (string.IsNullOrWhiteSpace(text))
                return Result<T>.Failure(SerializationErrors.EmptyText);

            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        RequireXmlSerializer();
                        return Result<T>.Success(xmlSerializer.Deserialize<T>(Decrypt(text, secureKey)));

                    case SerializationFormat.Json:
                        RequireJsonSerializer();
                        return Result<T>.Success(jsonSerializer.Deserialize<T>(Decrypt(text, secureKey)));

                    case SerializationFormat.Binary:
                        return Result<T>.Failure(SerializationErrors.BinaryTextDeserializationUnsupported);

                    default:
                        return Result<T>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (Exception exception)
            {
                return Result<T>.Failure(SerializationErrors.DeserializeFailed(format, exception));
            }
        }

        public Result<object> DeserializeFromText(string text, Type valueType, SerializationFormat format, string secureKey = "")
        {
            if (valueType == null)
                return Result<object>.Failure(SerializationErrors.InvalidValueType());

            if (string.IsNullOrWhiteSpace(text))
                return Result<object>.Failure(SerializationErrors.EmptyText);

            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        RequireXmlSerializer();
                        return Result<object>.Success(xmlSerializer.Deserialize(
                            Decrypt(text, secureKey), valueType));

                    case SerializationFormat.Json:
                        RequireJsonSerializer();
                        return Result<object>.Success(jsonSerializer.Deserialize(
                            Decrypt(text, secureKey), valueType));

                    case SerializationFormat.Binary:
                        return Result<object>.Failure(SerializationErrors.BinaryTextDeserializationUnsupported);

                    default:
                        return Result<object>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (Exception exception)
            {
                return Result<object>.Failure(SerializationErrors.DeserializeFailed(format, exception));
            }
        }

        private Result<byte[]> SerializeToBytes<T>(
            T value,
            SerializationFormat format,
            string secureKey)
        {
            try
            {
                switch (format)
                {
                    case SerializationFormat.Binary:
                        RequireBinarySerializer();
                        return Result<byte[]>.Success(binarySerializer.Serialize(value));

                    case SerializationFormat.Xml:
                        RequireXmlSerializer();
                        return Result<byte[]>.Success(Encoding.UTF8.GetBytes(xmlSerializer.Serialize(value)));

                    case SerializationFormat.Json:
                        RequireJsonSerializer();

                        string json = jsonSerializer.Serialize(value);

                        if (!string.IsNullOrEmpty(secureKey))
                        {
                            if (stringEncryptor == null)
                                return Result<byte[]>.Failure(SerializationErrors.MissingStringEncryptor);

                            json = stringEncryptor.Encrypt(json, secureKey);
                        }

                        return Result<byte[]>.Success(Encoding.UTF8.GetBytes(json));

                    default:
                        return Result<byte[]>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (Exception exception)
            {
                return Result<byte[]>.Failure(SerializationErrors.SerializeFailed(format, exception));
            }
        }

        private Result<T> DeserializeFromBytes<T>(
            byte[] bytes,
            SerializationFormat format,
            string secureKey)
        {
            if (bytes == null || bytes.Length == 0)
                return Result<T>.Failure(SerializationErrors.EmptyBytes);

            try
            {
                switch (format)
                {
                    case SerializationFormat.Binary:
                        RequireBinarySerializer();
                        return Result<T>.Success(binarySerializer.Deserialize<T>(bytes));

                    case SerializationFormat.Xml:
                        RequireXmlSerializer();
                        return Result<T>.Success(xmlSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes)));

                    case SerializationFormat.Json:
                        RequireJsonSerializer();

                        string json = Encoding.UTF8.GetString(bytes);

                        if (!string.IsNullOrEmpty(secureKey))
                        {
                            if (stringEncryptor == null)
                                return Result<T>.Failure(SerializationErrors.MissingStringEncryptor);

                            json = stringEncryptor.Decrypt(json, secureKey);
                        }

                        return Result<T>.Success(jsonSerializer.Deserialize<T>(json));

                    default:
                        return Result<T>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (Exception exception)
            {
                return Result<T>.Failure(SerializationErrors.DeserializeFailed(format, exception));
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
                throw new InvalidOperationException(SerializationErrors.MissingJsonSerializer.Message);
        }

        private void RequireXmlSerializer()
        {
            if (xmlSerializer == null)
                throw new InvalidOperationException(SerializationErrors.MissingXmlSerializer.Message);
        }

        private void RequireBinarySerializer()
        {
            if (binarySerializer == null)
                throw new InvalidOperationException(SerializationErrors.MissingBinarySerializer.Message);
        }
    }
}
