# SWeetJelly Serialization

SWeetJelly Serialization provides a small serialization service for Unity projects.

It supports text serialization, persistent data path file storage, optional string encryption, and Initialiser-based service registration.

## Structure

```text
Runtime
├── Contract
│   ├── ISerializationService
│   ├── ITextSerializer
│   ├── IBinarySerializer
│   ├── IStringEncryptor
│   ├── IFileStorage
│   ├── IPathProvider
│   └── SerializationFormat
├── App
│   ├── SerializationService
│   ├── JsonDataSerializer
│   ├── XmlDataSerializer
│   ├── BinaryDataSerializer
│   └── AesStringEncryptor
├── Framework
│   ├── UnityFileStorage
│   ├── UnityPersistentDataPathProvider
│   └── UnityResourcesReader
└── InitialiserIntegration
    └── SerializationInitModule
```

## Responsibilities

### Contract

Defines the public serialization API.

- `ISerializationService`
- `ITextSerializer`
- `IBinarySerializer`
- `IStringEncryptor`
- `IFileStorage`
- `IPathProvider`
- `SerializationFormat`
- `SerializationErrors`

### App

Contains Unity-independent serialization flow and default serializer implementations.

`SerializationService` is responsible for:

- serializing values to text
- deserializing values from text
- saving serialized bytes to a path
- loading serialized bytes from a path
- resolving persistent data path file names
- applying optional encryption when a secure key is provided

Default implementations include:

- `JsonDataSerializer` using Newtonsoft Json
- `XmlDataSerializer` using `System.Xml.Serialization.XmlSerializer`
- `AesStringEncryptor` for secure text serialization
- `BinaryDataSerializer` placeholder implementation

### Framework

Contains Unity-specific adapters.

- `UnityFileStorage` reads, writes, checks, and deletes files.
- `UnityPersistentDataPathProvider` exposes `Application.persistentDataPath`.
- `UnityResourcesReader` reads `TextAsset` data from Unity `Resources`.

### Initialiser Integration

`SerializationInitModule` installs the serialization module into the Initialiser system.

It creates:

- `UnityPersistentDataPathProvider`
- `UnityFileStorage`
- `JsonDataSerializer`
- `XmlDataSerializer`
- `BinaryDataSerializer`
- `AesStringEncryptor`
- `SerializationService`

Then it registers the service as `ISerializationService`.

## Serialization Formats

`SerializationFormat` currently defines:

```csharp
SerializationFormat.Binary
SerializationFormat.Xml
SerializationFormat.Json
```

JSON and XML are supported for text serialization.

Binary is part of the contract, but the current `BinaryDataSerializer` throws `NotSupportedException`. Do not use `SerializationFormat.Binary` until a real binary serializer is provided.

## Basic Usage

Resolve `ISerializationService` from the application context and serialize data.

```csharp
using SWeetJelly.Initialiser.Framework;
using SWeetJelly.Serialization.Contract;

var service = InitialiserBehaviour.Services.Resolve<ISerializationService>();

var data = new PlayerData
{
    Level = 3,
    Name = "Sweet"
};

var json = service.SerializeToText(data, SerializationFormat.Json);

if (json.IsFailure)
    return;

var loaded = service.DeserializeFromText<PlayerData>(json.Value, SerializationFormat.Json);

if (loaded.IsSuccess)
{
    var player = loaded.Value;
}
```

## Persistent Data Path

Use the persistent data path helpers when the file should be stored under Unity `Application.persistentDataPath`.

```csharp
var saveResult = await service.SaveToPersistentDataPathAsync(
    data,
    "player-data.json",
    SerializationFormat.Json,
    cancellationToken);

if (saveResult.IsFailure)
    return;

var loaded = await service.LoadFromPersistentDataPathAsync<PlayerData>(
    "player-data.json",
    SerializationFormat.Json,
    cancellationToken);

if (loaded.IsSuccess)
{
    var player = loaded.Value;
}
```

Check or delete the same file through the service.

```csharp
if (service.ExistsAtPersistentDataPath("player-data.json"))
{
    service.DeleteAtPersistentDataPath("player-data.json");
}
```

## Custom Paths

Use explicit paths when another system owns the location.

```csharp
var saveResult = await service.SaveToPathAsync(
    data,
    fullPath,
    SerializationFormat.Json,
    secureKey: "",
    ct: cancellationToken);

if (saveResult.IsFailure)
    return;

var loaded = await service.LoadFromPathAsync<PlayerData>(
    fullPath,
    SerializationFormat.Json,
    secureKey: "",
    ct: cancellationToken);
```

## Secure Text

Pass a non-empty secure key to encrypt serialized text.

```csharp
var secureText = service.SerializeToText(
    data,
    SerializationFormat.Json,
    secureKey: "local-save-key");

if (secureText.IsFailure)
    return;

var loaded = service.DeserializeFromText<PlayerData>(
    secureText.Value,
    SerializationFormat.Json,
    secureKey: "local-save-key");
```

The same secure key must be used for serialization and deserialization.

If a secure key is provided and no `IStringEncryptor` is assigned, `SerializationService` returns a failed result.

## Initialiser Setup

1. Create `SerializationInitModule` from:

```text
Create > SWeetJelly > Initialiser > Modules > Serialization
```

2. Add the module asset to a `ProjectInitSettingSo`.
3. Assign that setting to `InitialiserBehaviour`.
4. Resolve `ISerializationService` after Initialiser startup.

```csharp
using SWeetJelly.Initialiser.Framework;
using SWeetJelly.Serialization.Contract;

var serialization = InitialiserBehaviour.Services.Resolve<ISerializationService>();
```

## Custom Implementations

`SerializationService` depends on contracts, so tests or projects can replace infrastructure.

Useful replacement points:

- `IFileStorage`
- `IPathProvider`
- `ITextSerializer`
- `IBinarySerializer`
- `IStringEncryptor`

Example:

```csharp
var service = new SerializationService(
    fileStorage,
    pathProvider,
    jsonSerializer,
    xmlSerializer,
    binarySerializer,
    encryptor);
```

## Design Notes

- The public API is exposed through `SWeetJelly.Serialization.Contract`.
- Unity file system details stay in `SWeetJelly.Serialization.Framework`.
- `SerializationService` does not depend on Unity APIs.
- Consumers should depend on `ISerializationService`, not concrete serializer implementations.
- Serialization operations return `Result` or `Result<T>` and use `SerializationErrors` for package-owned failure descriptions.
- Save, loading, or gameplay systems should treat serialization as an infrastructure service.

## Current Status

This package currently provides:

- JSON text serialization
- XML text serialization
- optional AES string encryption
- persistent data path save/load helpers
- explicit path save/load helpers
- Unity file storage adapter
- Unity persistent data path adapter
- Resources text/byte reader
- Initialiser integration

Binary serialization is not implemented yet.
