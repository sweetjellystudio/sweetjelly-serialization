# Serialization sample

Import this sample through Package Manager, open Sample Scene and enter Play Mode. Serialization Package Bootstrapper registers ISerializationService through the assigned profile; shutdown removes its own registration. The consumer demonstrates JSON/XML text, authenticated secure JSON and persistent JSON save/load/delete. The sample key is public demonstration data, not a keystore or production secret.

Use the on-screen buttons and inspect the status and preview. File operations use Application.persistentDataPath/serialization-sample.json. Back up a pre-existing file with that name before using the sample. Tests/validation should clean up only files they created.

The bundled link.xml preserves SerializationSampleData for reflection-based JSON/XML in IL2CPP. Extend preservation for your actual DTOs and converters; do not assume all arbitrary models are covered. Framework.Serialization contains the concrete adapters in 2.0. See [migration][migration] for existing encrypted data and XML file key behavior.

[migration]: ../../docs/migration-2.0.md
