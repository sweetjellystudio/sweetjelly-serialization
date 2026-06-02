# CHANGELOG

## v1.0.0

* [Added] Added `SWeetJelly.Serialization` as an independent Unity package extracted from the previous Core module.
* [Added] Added text serialization contracts for serializing and deserializing data.
* [Added] Added JSON serialization support through `JsonDataSerializer`.
* [Added] Added XML serialization support through `XmlDataSerializer`.
* [Added] Added file storage and path provider abstractions for persistence-related modules.
* [Added] Added string encryption support through AES-based encryption.
* [Changed] Moved serialization, storage, path, and encryption utilities from the unclassified Core module into a dedicated Serialization package.
* [Changed] Separated Serialization into dedicated contract and implementation assemblies.