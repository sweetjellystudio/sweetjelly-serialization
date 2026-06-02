namespace SWeetJelly.Serialization.Contract
{
    public interface IBinarySerializer
    {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] bytes);
    }
}