using System;
using SWeetJelly.Serialization.Contract;

namespace SWeetJelly.Serialization.App
{
    public sealed class BinaryDataSerializer : IBinarySerializer
    {
        public byte[] Serialize<T>(T data)
        {
            throw new NotSupportedException();
        }

        public T Deserialize<T>(byte[] bytes)
        {
            throw new NotSupportedException();
        }
    }
}