# Serialization Dev Sample

This sample shows the consumer-side flow for `com.sweetjelly.serialization`.

- `Serialization Package Bootstrapper.asset` installs `SerializationPackageBootstrapperSo` through Bootstrapper.
- `Sample Package Bootstrapper Profile.asset` is assigned to `BootstrapperBehaviour`.
- `SerializationSampleConsumerBehaviour` resolves `ISerializationService` and demonstrates text serialization, secure text serialization, and persistent data path file operations.

## Setup

Open `Sample Scene` and enter Play Mode.

Use the on-screen buttons to serialize JSON/XML, save JSON to Unity `Application.persistentDataPath`, load it back, delete it, and verify secure text serialization.
