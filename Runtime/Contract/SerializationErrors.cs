namespace SWeetJelly.Serialization.Contract
{
    using System;
    using SWeetJelly.Shared.Result;

    public static class SerializationErrors
    {
        public static readonly Error EmptyText =
            new Error("Serialization.EmptyText", "Serialized text cannot be empty.");

        public static readonly Error EmptyBytes =
            new Error("Serialization.EmptyBytes", "Serialized bytes cannot be empty.");

        public static readonly Error MissingJsonSerializer =
            new Error("Serialization.MissingJsonSerializer", "Json serializer is not assigned.");

        public static readonly Error MissingXmlSerializer =
            new Error("Serialization.MissingXmlSerializer", "Xml serializer is not assigned.");

        public static readonly Error MissingBinarySerializer =
            new Error("Serialization.MissingBinarySerializer", "Binary serializer is not assigned.");

        public static readonly Error MissingStringEncryptor =
            new Error("Serialization.MissingStringEncryptor", "String encryptor is not assigned.");

        public static readonly Error BinaryTextUnsupported =
            new Error("Serialization.BinaryTextUnsupported", "Binary format does not support text serialization.");

        public static readonly Error BinaryTextDeserializationUnsupported =
            new Error("Serialization.BinaryTextDeserializationUnsupported", "Binary format does not support text deserialization.");

        public static Error InvalidValueType()
        {
            return new Error("Serialization.InvalidValueType", "Value type cannot be null.");
        }

        public static Error UnsupportedFormat(SerializationFormat format)
        {
            return new Error("Serialization.UnsupportedFormat", $"Unsupported serialization format: {format}.");
        }

        public static Error FileNotFound(string path)
        {
            return new Error("Serialization.FileNotFound", $"Serialized file was not found: {path}.");
        }

        public static Error ReadFailed(string path, Exception exception)
        {
            return new Error("Serialization.ReadFailed", $"Failed to read serialized file '{path}'. {exception.Message}");
        }

        public static Error WriteFailed(string path, Exception exception)
        {
            return new Error("Serialization.WriteFailed", $"Failed to write serialized file '{path}'. {exception.Message}");
        }

        public static Error SerializeFailed(SerializationFormat format, Exception exception)
        {
            return new Error("Serialization.SerializeFailed", $"Failed to serialize {format} data. {exception.Message}");
        }

        public static Error DeserializeFailed(SerializationFormat format, Exception exception)
        {
            return new Error("Serialization.DeserializeFailed", $"Failed to deserialize {format} data. {exception.Message}");
        }
    }
}
