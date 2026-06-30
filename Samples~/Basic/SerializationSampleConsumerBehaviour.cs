namespace SWeetJelly.Samples.Serialization
{
    using System;
    using SWeetJelly.Bootstrapper.Framework.Unity;
    using SWeetJelly.Serialization.Contract;
    using UnityEngine;

    public sealed class SerializationSampleConsumerBehaviour : MonoBehaviour
    {
        private const string FileName = "serialization-sample.json";
        private const string SecureKey = "sample-secure-key";

        private ISerializationService serializationService;
        private string status = "Waiting for Serialization service...";
        private string preview = string.Empty;
        private int saveIndex = 1;

        private void OnEnable()
        {
            BootstrapperBehaviour.Bootstrapped += OnBootstrapped;
            ResolveIfReady();
        }

        private void OnDisable()
        {
            BootstrapperBehaviour.Bootstrapped -= OnBootstrapped;
        }

        private void OnBootstrapped(object sender, EventArgs args)
        {
            ResolveIfReady();
        }

        private void ResolveIfReady()
        {
            if (serializationService != null || !BootstrapperBehaviour.IsBootstrapped || BootstrapperBehaviour.Services == null)
                return;

            if (!BootstrapperBehaviour.Services.TryResolve<ISerializationService>(out serializationService))
            {
                status = "ISerializationService is not registered.";
                return;
            }

            status = "Serialization service is ready.";
        }

        private void OnGUI()
        {
            const float x = 16f;
            var y = 16f;

            GUI.Label(new Rect(x, y, 620f, 24f), status);
            y += 32f;

            GUI.enabled = serializationService != null;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Serialize JSON"))
                SerializeJson();

            y += 34f;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Serialize XML"))
                SerializeXml();

            y += 34f;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Secure JSON"))
                SerializeSecureJson();

            y += 42f;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Save File"))
                SaveFile();

            y += 34f;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Load File"))
                LoadFile();

            y += 34f;

            if (GUI.Button(new Rect(x, y, 190f, 28f), "Delete File"))
                DeleteFile();

            GUI.enabled = true;

            y += 34f;

            GUI.TextArea(new Rect(x, y, Screen.width - 16f, 240f), preview);
        }

        private void SerializeJson()
        {
            var data = CreateData();
            var json = serializationService.SerializeToText(data, SerializationFormat.Json);

            if (json.IsFailure)
            {
                status = $"JSON serialization failed: {json.Error}";
                return;
            }

            var loaded = serializationService.DeserializeFromText<SerializationSampleData>(json.Value, SerializationFormat.Json);

            if (loaded.IsFailure)
            {
                status = $"JSON deserialization failed: {loaded.Error}";
                return;
            }

            status = $"JSON round-trip complete: {loaded.Value.PlayerName}";
            preview = json.Value;
        }

        private void SerializeXml()
        {
            var data = CreateData();
            var xml = serializationService.SerializeToText(data, SerializationFormat.Xml);

            if (xml.IsFailure)
            {
                status = $"XML serialization failed: {xml.Error}";
                return;
            }

            var loaded = serializationService.DeserializeFromText<SerializationSampleData>(xml.Value, SerializationFormat.Xml);

            if (loaded.IsFailure)
            {
                status = $"XML deserialization failed: {loaded.Error}";
                return;
            }

            status = $"XML round-trip complete: {loaded.Value.PlayerName}";
            preview = xml.Value;
        }

        private void SerializeSecureJson()
        {
            var data = CreateData();
            var secureText = serializationService.SerializeToText(data, SerializationFormat.Json, SecureKey);

            if (secureText.IsFailure)
            {
                status = $"Secure JSON serialization failed: {secureText.Error}";
                return;
            }

            var loaded = serializationService.DeserializeFromText<SerializationSampleData>(
                secureText.Value,
                SerializationFormat.Json,
                SecureKey);

            if (loaded.IsFailure)
            {
                status = $"Secure JSON deserialization failed: {loaded.Error}";
                return;
            }

            status = $"Secure JSON round-trip complete: {loaded.Value.PlayerName}";
            preview = secureText.Value;
        }

        private async void SaveFile()
        {
            try
            {
                var data = CreateData();

                var result = await serializationService.SaveToPersistentDataPathAsync(
                    data,
                    FileName,
                    SerializationFormat.Json);

                if (result.IsFailure)
                {
                    status = $"Save failed: {result.Error}";
                    return;
                }

                status = $"Saved to persistent data path: {FileName}";
                preview = $"Exists: {serializationService.ExistsAtPersistentDataPath(FileName)}";
            }
            catch (Exception ex)
            {
                status = $"Save failed: {ex.Message}";
                Debug.LogException(ex, this);
            }
        }

        private async void LoadFile()
        {
            try
            {
                if (!serializationService.ExistsAtPersistentDataPath(FileName))
                {
                    status = $"File does not exist: {FileName}";
                    return;
                }

                var loaded = await serializationService.LoadFromPersistentDataPathAsync<SerializationSampleData>(
                    FileName,
                    SerializationFormat.Json);

                if (loaded.IsFailure)
                {
                    status = $"Load failed: {loaded.Error}";
                    return;
                }

                status = $"Loaded file: {loaded.Value.PlayerName}";
                preview = $"Level: {loaded.Value.Level}\nName: {loaded.Value.PlayerName}\nPlayTime: {loaded.Value.PlayTime:0.0}";
            }
            catch (Exception ex)
            {
                status = $"Load failed: {ex.Message}";
                Debug.LogException(ex, this);
            }
        }

        private void DeleteFile()
        {
            serializationService.DeleteAtPersistentDataPath(FileName);
            status = $"Deleted persistent data path file: {FileName}";
            preview = $"Exists: {serializationService.ExistsAtPersistentDataPath(FileName)}";
        }

        private SerializationSampleData CreateData()
        {
            var data = new SerializationSampleData
            {
                Level = saveIndex,
                PlayerName = $"Sweet Jelly {saveIndex}",
                PlayTime = Time.time
            };

            saveIndex++;
            return data;
        }
    }
}
