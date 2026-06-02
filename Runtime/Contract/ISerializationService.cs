namespace SWeetJelly.Serialization.Contract
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ISerializationService
    {
        Task SaveToPersistentDataPathAsync<T>(
            T value,
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default);

        Task<T> LoadFromPersistentDataPathAsync<T>(
            string fileName,
            SerializationFormat format,
            CancellationToken ct = default);

        Task SaveToPathAsync<T>(
            T value,
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default);

        Task<T> LoadFromPathAsync<T>(
            string path,
            SerializationFormat format,
            string secureKey = "",
            CancellationToken ct = default);

        bool ExistsAtPersistentDataPath(string fileName);

        void DeleteAtPersistentDataPath(string fileName);

        string SerializeToText<T>(T value, SerializationFormat format, string secureKey = "");

        string SerializeToText(object value, Type valueType, SerializationFormat format, string secureKey = "");

        T DeserializeFromText<T>(string text, SerializationFormat format, string secureKey = "");

        object DeserializeFromText(string text, Type valueType, SerializationFormat format, string secureKey = "");
    }
}