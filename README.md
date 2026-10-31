# SWeetJelly Serialization

Serialization services and adapters for Unity 6.3 LTS. JSON/XML, optional authenticated text encryption, file IO and Bootstrapper registration are provided. Game save schemas, scheduling, atomic writes and key management belong to consumers.

## Installation

After the 2.0.0 tag is published, add the custom Git dependencies explicitly in your project's Packages/manifest.json. UPM package dependencies cannot themselves contain Git URLs.

```json
{
  "dependencies": {
    "com.sweetjelly.serialization": "https://github.com/sweetjellystudio/sweetjelly-serialization.git#v2.0.0",
    "com.sweetjelly.bootstrapper": "https://github.com/sweetjellystudio/sweetjelly-bootstrapper.git#v2.0.0",
    "com.sweetjelly.shared": "https://github.com/sweetjellystudio/sweetjelly-shared.git#v2.0.0"
  }
}
```

Newtonsoft.Json 3.2.2 and Bootstrapper's uGUI dependency resolve through UPM. Merge these entries into the existing manifest; do not replace unrelated project dependencies. For 1.x consumers, read [migration][migration]. Root [LICENSE.md][license] is MIT.

## Usage

Normal consumers reference SWeetJelly.Serialization.Contract and obtain ISerializationService from their composition root. The included SerializationPackageBootstrapperSo registers defaults through Bootstrapper and unregisters its own service on shutdown. Existing asset/script GUIDs are preserved.

Direct composition additionally references App, Framework.Serialization and Framework.Unity:

```csharp
using SWeetJelly.Serialization.App;
using SWeetJelly.Serialization.Contract;
using SWeetJelly.Serialization.Framework.Serialization;
using SWeetJelly.Serialization.Framework.Unity;

var service = new SerializationService(
    new UnityFileStorage(), new UnityPersistentDataPathProvider(),
    new JsonDataSerializer(), new XmlDataSerializer(),
    stringEncryptor: new AesStringEncryptor());

var text = service.SerializeToText(data, SerializationFormat.Json);
if (text.IsSuccess)
{
    var restored = service.DeserializeFromText<MyData>(text.Value, SerializationFormat.Json);
    // Check restored.IsSuccess before using restored.Value.
}

// In an async method, with a consumer-supplied key and cancellation token:
var saved = await service.SaveToPathAsync(data, fullPath, SerializationFormat.Xml, key, ct);
var loaded = await service.LoadFromPathAsync<MyData>(fullPath, SerializationFormat.Xml, key, ct);
```

The snippet uses application-defined data/MyData, fullPath, key and ct. Check every Result before accessing Value. Persistent-path helpers use a filename and no encryption key parameter; use the explicit path methods for encrypted files. ExistsAtPersistentDataPath/DeleteAtPersistentDataPath retain synchronous IO exceptions.

## Formats, errors and cancellation

JSON uses Newtonsoft defaults; XML uses XmlSerializer, writes an UTF-8 declaration and rejects DTDs. JSON null behavior and custom global JsonConvert settings follow Newtonsoft. XML requires a supported DTO with a public parameterless constructor. The generic and object/Type text overloads are supported.

Binary enum value 0 remains available for a custom IBinarySerializer in file APIs; no BinaryDataSerializer is shipped. Binary text requests fail, missing codecs return specific Missing errors, and a Binary file request with secureKey fails rather than ignoring the key.

JSON/XML secureKey enables the versioned authenticated envelope for both text and file APIs. New data has random salt/IV and tamper authentication. Existing 1.x encrypted data can be read, but that old format has no integrity guarantee. Reading never rewrites data. See [the encryption format][encryption] for the protocol, cost and limitations.

Async operations check cancellation before synchronous codec work and around IO; cancellation propagates as OperationCanceledException. Ordinary codec/path/IO errors return Result failures. A token cannot interrupt synchronous serialization/KDF work already running. Writes are not atomic and concurrent writers need consumer coordination. The service borrows injected adapters and does not dispose them or cancel in-flight calls on bootstrap shutdown.

## Samples and validation

Import the Basic sample through Package Manager and follow [its instructions][sample]. Reflection-based IL2CPP consumers must preserve their actual DTOs/converters; sample link.xml covers only the sample model. Read [Tests][tests] for coverage and [architecture][architecture] for boundaries. Runtime results are specific to the tested Editor, backend and model; no all-platform verification is implied.

[migration]: docs/migration-2.0.md
[license]: LICENSE.md
[encryption]: docs/encryption-format.md
[sample]: Samples~/Basic/README.md
[tests]: Tests/README.md
[architecture]: ARCHITECTURE.md
