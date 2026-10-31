#   Migrating from 1.x to 2.0

Use Unity 6.3 or later with Bootstrapper 2.0.0, Shared 2.0.0 and Newtonsoft.Json 3.2.2. The unused Utils dependency is removed. The root LICENSE remains MIT.

##  Code and assemblies

`ISerializationService` signatures and `SerializationFormat` integer values remain unchanged. JsonDataSerializer, XmlDataSerializer and AesStringEncryptor move from `SWeetJelly.Serialization.App` to `SWeetJelly.Serialization.Framework.Serialization` in the matching new asmdef. Direct constructors need the new namespace and assembly reference. SerializationService remains in App. The old concrete namespace is removed in this major release.

BinaryDataSerializer is removed because it never implemented a format. Inject an IBinarySerializer to use Binary file APIs. Text Binary remains unsupported. With no binary adapter, file requests return MissingBinarySerializer; a nonempty secureKey with Binary returns BinaryEncryptionUnsupported even when an adapter exists. No format numbers are reordered.

##  Stored data and encryption

Existing JSON/XML data remains readable with the same DTO/schema and serializer settings. This does not migrate a game's renamed/deleted fields. JSON still uses Newtonsoft's default settings; application-level JsonConvert.DefaultSettings/custom converters remain the consumer's policy.

New encrypted text and JSON/XML files use the authenticated versioned envelope. Existing 1.x encrypted JSON/XML text and JSON files remain readable with the original key. Reads do not rewrite files; explicit save upgrades them. Unknown/damaged new envelopes and wrong keys fail without plaintext/legacy fallback. 1.x cannot read new ciphertext: back up original data or deploy a compatible reader before rolling back. Legacy ciphertext itself remains unauthenticated; see [format details][format].

1.x XML file APIs silently ignored secureKey and wrote plaintext. 2.0 honors it. Read those historical plaintext XML files without a key, then explicitly save with a key to encrypt them. Do not add automatic plaintext fallback after a key was supplied. New XML output declares UTF-8, matching stored bytes; the former UTF-16 declaration in string output is still accepted on reads. DTDs are prohibited.

##  Cancellation and lifecycle

Pre-cancelled async requests stop before path/codec/IO work. Cancellation propagates as OperationCanceledException; ordinary serialization/path/IO failures return Result errors. Missing adapters/encryptors now return specific Missing errors. Exists/Delete remain synchronous and may throw. Cancellation is cooperative, does not interrupt synchronous codecs/cryptography, and does not promise atomic file replacement.

Bootstrap uses the same script GUID and public integration entry point. Shutdown unregisters only its own identical service; a bootstrap asset instance owns one active registration. Use separate instances for concurrent scopes. Existing services do not gain IDisposable or cancellation-on-shutdown.

##  IL2CPP consumers

Reflection-based JSON/XML requires preserving DTO constructors/members and any custom converters for the actual consumer model. The sample's [link.xml][linker] preserves only its own DTO. Add appropriate preservation in the game instead of preserving every package assembly. Windows Player smoke coverage does not prove arbitrary generic/reflection paths or other platforms.

[format]: encryption-format.md
[linker]: ../Samples~/Basic/link.xml
