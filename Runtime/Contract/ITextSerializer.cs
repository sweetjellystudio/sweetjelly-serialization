using System;

namespace SWeetJelly.Serialization.Contract
{
    public interface ITextSerializer
    {
        string Serialize<T>(T value);

        string Serialize(object value, Type type);

        T Deserialize<T>(string text);

        object Deserialize(string text, Type type);
    }
}