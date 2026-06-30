using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SWeetJelly.Serialization.Contract;

namespace SWeetJelly.Serialization.Framework.Unity
{
    public sealed class UnityFileStorage : IFileStorage
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public async Task<byte[]> ReadBytesAsync(
            string path,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return await File.ReadAllBytesAsync(path, ct);
        }

        public async Task WriteBytesAsync(
            string path,
            byte[] bytes,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllBytesAsync(path, bytes, ct);
        }

        public void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}