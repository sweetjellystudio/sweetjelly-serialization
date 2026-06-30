using System.IO;
using SWeetJelly.Serialization.Contract;
using UnityEngine;

namespace SWeetJelly.Serialization.Framework.Unity
{
    public sealed class UnityPersistentDataPathProvider : IPathProvider
    {
        public string PersistentDataPath => Application.persistentDataPath;

        public string Combine(string directoryPath, string fileName)
        {
            return Path.Combine(directoryPath, fileName);
        }
    }
}