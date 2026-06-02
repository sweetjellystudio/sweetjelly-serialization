using System;
using System.IO;
using System.Xml.Serialization;
using SWeetJelly.Serialization.Contract;

namespace SWeetJelly.Serialization.App
{
    public sealed class XmlDataSerializer : ITextSerializer
    {
        public string Serialize<T>(T value)
        {
            return Serialize(value, typeof(T));
        }

        public string Serialize(object value, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (value != null && !type.IsInstanceOfType(value))
            {
                throw new ArgumentException(
                    $"Value type '{value.GetType().FullName}' is not assignable to '{type.FullName}'.",
                    nameof(value));
            }

            var serializer = new XmlSerializer(type);

            using var writer = new StringWriter();
            serializer.Serialize(writer, value);

            return writer.ToString();
        }

        public T Deserialize<T>(string text)
        {
            return (T)Deserialize(text, typeof(T));
        }

        public object Deserialize(string text, Type type)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Xml text cannot be empty.", nameof(text));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var serializer = new XmlSerializer(type);

            using var reader = new StringReader(text);
            return serializer.Deserialize(reader);
        }
    }
}