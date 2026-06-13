namespace SWeetJelly.Serialization.Contract
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using SWeetJelly.Shared.Result;

    public interface ISerializationService
    {
        Task<Result> SaveToPersistentDataPathAsync<T>(
            T value,
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default);

        Task<Result<T>> LoadFromPersistentDataPathAsync<T>(
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default);

        Task<Result> SaveToPathAsync<T>(
            T value,
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default);

        Task<Result<T>> LoadFromPathAsync<T>(
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default);

        bool ExistsAtPersistentDataPath(string fileName);

        void DeleteAtPersistentDataPath(string fileName);

        Result<string> SerializeToText<T>(T value, SerializationFormat format, string secureKey = "");

        Result<string> SerializeToText(object value, Type valueType, SerializationFormat format, string secureKey = "");

        Result<T> DeserializeFromText<T>(string text, SerializationFormat format, string secureKey = "");

        Result<object> DeserializeFromText(string text, Type valueType, SerializationFormat format, string secureKey = "");
    }
}
