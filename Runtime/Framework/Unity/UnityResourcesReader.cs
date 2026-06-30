using System;
using UnityEngine;

namespace SWeetJelly.Serialization.Framework.Unity
{
    public sealed class UnityResourcesReader
    {
        public bool Exists(string resourcePath)
        {
            return Resources.Load<TextAsset>(resourcePath) != null;
        }

        public byte[] ReadBytes(string resourcePath)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

            if (textAsset == null)
                throw new InvalidOperationException($"Resource not found: {resourcePath}");

            return textAsset.bytes;
        }

        public string ReadText(string resourcePath)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

            if (textAsset == null)
                throw new InvalidOperationException($"Resource not found: {resourcePath}");

            return textAsset.text;
        }
    }
}