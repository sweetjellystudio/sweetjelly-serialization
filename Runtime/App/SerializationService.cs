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

        public async Task<Result> SaveToPersistentDataPathAsync<T>(
            T value,
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                string path = pathProvider.Combine(pathProvider.PersistentDataPath, fileName);
                return await SaveToPathAsync(value, path, format, ct: ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) { return Result.Failure(SerializationErrors.PathFailed(fileName, exception)); }
        }

        public async Task<Result> SaveToPathAsync<T>(
            T value,
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Result<byte[]> bytesResult = SerializeToBytes(value, format, secureKey);

            if (bytesResult.IsFailure)
                return Result.Failure(bytesResult.Error);

            try
            {
                ct.ThrowIfCancellationRequested();
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

        public async Task<Result<T>> LoadFromPersistentDataPathAsync<T>(
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                string path = pathProvider.Combine(pathProvider.PersistentDataPath, fileName);
                return await LoadFromPathAsync<T>(path, format, ct: ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) { return Result<T>.Failure(SerializationErrors.PathFailed(fileName, exception)); }
        }

        public async Task<Result<T>> LoadFromPathAsync<T>(
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                Error dependency = ByteDependencyError(format, secureKey);
                if (!dependency.IsEmpty) return Result<T>.Failure(dependency);
                byte[] bytes = await fileStorage.ReadBytesAsync(path, ct);
                ct.ThrowIfCancellationRequested();
                return DeserializeFromBytes<T>(bytes, format, secureKey);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DirectoryNotFoundException)
            {
                return Result<T>.Failure(SerializationErrors.FileNotFound(path));
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
            Error dependency = TextDependencyError(format, secureKey);
            if (!dependency.IsEmpty) return Result<string>.Failure(dependency);
            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        return Result<string>.Success(Encrypt(xmlSerializer.Serialize(value), secureKey));

                    case SerializationFormat.Json:
                        return Result<string>.Success(Encrypt(jsonSerializer.Serialize(value), secureKey));

                    case SerializationFormat.Binary:
                        return Result<string>.Failure(SerializationErrors.BinaryTextUnsupported);

                    default:
                        return Result<string>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                return Result<string>.Failure(SerializationErrors.SerializeFailed(format, exception));
            }
        }

        public Result<string> SerializeToText(object value, Type valueType, SerializationFormat format, string secureKey = "")
        {
            if (valueType == null)
                return Result<string>.Failure(SerializationErrors.InvalidValueType());

            Error dependency = TextDependencyError(format, secureKey);
            if (!dependency.IsEmpty) return Result<string>.Failure(dependency);
            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        return Result<string>.Success(Encrypt(xmlSerializer.Serialize(value, valueType), secureKey));

                    case SerializationFormat.Json:
                        return Result<string>.Success(Encrypt(jsonSerializer.Serialize(value, valueType), secureKey));

                    case SerializationFormat.Binary:
                        return Result<string>.Failure(SerializationErrors.BinaryTextUnsupported);

                    default:
                        return Result<string>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                return Result<string>.Failure(SerializationErrors.SerializeFailed(format, exception));
            }
        }

        public Result<T> DeserializeFromText<T>(string text, SerializationFormat format, string secureKey = "")
        {
            if (string.IsNullOrWhiteSpace(text))
                return Result<T>.Failure(SerializationErrors.EmptyText);

            Error dependency = TextDependencyError(format, secureKey);
            if (!dependency.IsEmpty) return Result<T>.Failure(dependency);
            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        return Result<T>.Success(xmlSerializer.Deserialize<T>(Decrypt(text, secureKey)));

                    case SerializationFormat.Json:
                        return Result<T>.Success(jsonSerializer.Deserialize<T>(Decrypt(text, secureKey)));

                    case SerializationFormat.Binary:
                        return Result<T>.Failure(SerializationErrors.BinaryTextDeserializationUnsupported);

                    default:
                        return Result<T>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (OperationCanceledException) { throw; }
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

            Error dependency = TextDependencyError(format, secureKey);
            if (!dependency.IsEmpty) return Result<object>.Failure(dependency);
            try
            {
                switch (format)
                {
                    case SerializationFormat.Xml:
                        return Result<object>.Success(xmlSerializer.Deserialize(
                            Decrypt(text, secureKey), valueType));

                    case SerializationFormat.Json:
                        return Result<object>.Success(jsonSerializer.Deserialize(
                            Decrypt(text, secureKey), valueType));

                    case SerializationFormat.Binary:
                        return Result<object>.Failure(SerializationErrors.BinaryTextDeserializationUnsupported);

                    default:
                        return Result<object>.Failure(SerializationErrors.UnsupportedFormat(format));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception)
            {
                return Result<object>.Failure(SerializationErrors.DeserializeFailed(format, exception));
            }
        }

        private Result<byte[]> SerializeToBytes<T>(T value, SerializationFormat format, string secureKey)
        {
            Error dependency = ByteDependencyError(format, secureKey);
            if (!dependency.IsEmpty) return Result<byte[]>.Failure(dependency);
            if (format != SerializationFormat.Binary)
            {
                Result<string> result = SerializeToText(value, format, secureKey);
                if (result.IsFailure) return Result<byte[]>.Failure(result.Error);
                try { return Result<byte[]>.Success(Encoding.UTF8.GetBytes(result.Value)); }
                catch (Exception exception) { return Result<byte[]>.Failure(SerializationErrors.SerializeFailed(format, exception)); }
            }
            try { return Result<byte[]>.Success(binarySerializer.Serialize(value)); }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) { return Result<byte[]>.Failure(SerializationErrors.SerializeFailed(format, exception)); }
        }

        private Result<T> DeserializeFromBytes<T>(byte[] bytes, SerializationFormat format, string secureKey)
        {
            if (bytes == null || bytes.Length == 0) return Result<T>.Failure(SerializationErrors.EmptyBytes);
            Error dependency = ByteDependencyError(format, secureKey);
            if (!dependency.IsEmpty) return Result<T>.Failure(dependency);
            if (format != SerializationFormat.Binary)
                return DeserializeFromText<T>(Encoding.UTF8.GetString(bytes), format, secureKey);
            try { return Result<T>.Success(binarySerializer.Deserialize<T>(bytes)); }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) { return Result<T>.Failure(SerializationErrors.DeserializeFailed(format, exception)); }
        }

        private Error TextDependencyError(SerializationFormat format, string secureKey)
        {
            if (format == SerializationFormat.Json && jsonSerializer == null) return SerializationErrors.MissingJsonSerializer;
            if (format == SerializationFormat.Xml && xmlSerializer == null) return SerializationErrors.MissingXmlSerializer;
            if ((format == SerializationFormat.Json || format == SerializationFormat.Xml) && !string.IsNullOrEmpty(secureKey) && stringEncryptor == null)
                return SerializationErrors.MissingStringEncryptor;
            return Error.None;
        }

        private Error ByteDependencyError(SerializationFormat format, string secureKey)
        {
            if (format == SerializationFormat.Binary)
            {
                if (!string.IsNullOrEmpty(secureKey)) return SerializationErrors.BinaryEncryptionUnsupported;
                return binarySerializer == null ? SerializationErrors.MissingBinarySerializer : Error.None;
            }
            if (format != SerializationFormat.Json && format != SerializationFormat.Xml) return SerializationErrors.UnsupportedFormat(format);
            return TextDependencyError(format, secureKey);
        }

        private string Encrypt(string text, string secureKey) => string.IsNullOrEmpty(secureKey) ? text : stringEncryptor.Encrypt(text, secureKey);
        private string Decrypt(string text, string secureKey) => string.IsNullOrEmpty(secureKey) ? text : stringEncryptor.Decrypt(text, secureKey);
    }
}
