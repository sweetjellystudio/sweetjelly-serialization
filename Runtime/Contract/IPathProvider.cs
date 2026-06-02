namespace SWeetJelly.Serialization.Contract
{
    public interface IPathProvider
    {
        string PersistentDataPath { get; }

        string Combine(string directory, string fileName);
    }
}