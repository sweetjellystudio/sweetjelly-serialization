# CHANGELOG

## v2.0.0

* Require Unity 6.3, Bootstrapper 2.0.0 and Shared 2.0.0; remove unused Utils and retain Newtonsoft.Json 3.2.2. Root MIT license is unchanged.
* Move concrete JSON/XML/AES adapters from App to Framework.Serialization. Preserve ISerializationService and format numbers; remove unsupported BinaryDataSerializer while retaining the custom binary port.
* Write versioned AES-CBC/HMAC authenticated envelopes with random salt/IV and strengthened key derivation. Preserve read-only 1.x ciphertext compatibility; explicit save upgrades data and new ciphertext is not readable by 1.x.
* Honor XML file secureKey, reject Binary+secureKey, unify missing dependency errors and async cancellation, and make XML UTF-8 declarations match stored bytes while prohibiting DTDs.
* Track bootstrap registration ownership and idempotent shutdown/retry without adding disposal of borrowed adapters.
* Add fixed legacy/cross-implementation fixtures, failure/tamper/lifecycle/runtime coverage, sample linker preservation, architecture/agent guidance and migration decisions.

## v1.0.0

* [Added] Added `SWeetJelly.Serialization` as an independent Unity package extracted from the previous Core module.
* [Added] Added text serialization contracts for serializing and deserializing data.
* [Added] Added JSON serialization support through `JsonDataSerializer`.
* [Added] Added XML serialization support through `XmlDataSerializer`.
* [Added] Added file storage and path provider abstractions for persistence-related modules.
* [Added] Added string encryption support through AES-based encryption.
* [Added] Added `SerializationErrors` for package-owned serialization failure descriptions.
* [Changed] Serialization operations now return `Result` or `Result<T>` instead of exposing recoverable failures through exceptions.
* [Changed] Moved serialization, storage, path, and encryption utilities from the unclassified Core module into a dedicated Serialization package.
* [Changed] Separated Serialization into dedicated contract and implementation assemblies.
