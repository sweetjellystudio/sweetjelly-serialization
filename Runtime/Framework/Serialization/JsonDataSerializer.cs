using System;
using Newtonsoft.Json;
using SWeetJelly.Serialization.Contract;

namespace SWeetJelly.Serialization.Framework.Serialization
{
    public sealed class JsonDataSerializer : ITextSerializer
    {
        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public string Serialize(object value, Type type)
        {
            return JsonConvert.SerializeObject(value, type, null);
        }

        public T Deserialize<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        public object Deserialize(string text, Type type)
        {
            return JsonConvert.DeserializeObject(text, type);
        }
    }
}