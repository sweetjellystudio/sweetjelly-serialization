using System.Threading;
using System.Threading.Tasks;

namespace SWeetJelly.Serialization.Contract
{
    public interface IFileStorage
    {
        bool Exists(string path);

        Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default);

        Task WriteBytesAsync(string path, byte[] bytes, CancellationToken ct = default);

        void Delete(string path);
    }
}