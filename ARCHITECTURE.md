#   Serialization architecture

The package translates objects into JSON/XML or an injected binary representation, optionally encrypts text, and coordinates file operations. It does not own game save schemas, save scheduling, key management, atomic transactions or migration of changed game models.

##  Responsibilities and dependencies

*   Contract owns `ISerializationService`, serializer/encryptor/storage/path ports, stable format values and Result errors. It references Shared.Result.
*   App owns `SerializationService`: format dispatch, dependency checks, text encryption flow, UTF-8 file conversion, cancellation boundaries and error mapping. It references Contract and Shared.Result, with no Unity/Newtonsoft assembly reference.
*   Framework.Serialization implements Newtonsoft JSON, System.Xml XML, and AES-CBC/HMAC encryption plus a private read-only legacy decryptor. It references Contract and Newtonsoft.Json.dll and has no Unity reference.
*   Framework.Unity supplies file IO, Unity persistent paths and main-thread Resources access. It references Contract. File IO is grouped with these adapters for this package's small scope; no additional filesystem assembly is needed.
*   Framework.Bootstrapper composes the defaults and registers `ISerializationService`. It references the inward contract/App and outward adapters and Bootstrapper. This is the composition root, not App behavior.

There is no independent Domain: dispatch and error flow belong to App, and codecs/cryptography are technical adapters. DDD guides ownership and terminology; asmdefs are deployment/compilation boundaries, not automatically bounded contexts. This follows the development host's DDD/Clean Architecture principles without forcing a uniform layer count.

Consumers normally reference Contract. Consumers constructing services also reference App and the adapters they instantiate. The actual serializer variation is exposed by `ITextSerializer`/`IBinarySerializer`; the service creates no concrete codecs. UI and sample-only state stay in Samples~.

##  Ownership and execution

The service borrows its injected dependencies. It has no subscriptions, registry lookup, background worker or disposal obligation. Async file operations are awaited per call; cancellation propagates as OperationCanceledException. Synchronous serialization/encryption can be expensive and runs on the caller's thread. A token is checked before work and before IO, but cannot interrupt a synchronous codec/KDF already running.

The bootstrap asset tracks one registration/registry at a time, rejects duplicate active Bootstrap calls and removes only its own identical service on Shutdown. Registration conflicts preserve the existing owner. Successful Shutdown is idempotent and permits rebootstrap. Other scopes require different runtime bootstrap asset instances; a shared config asset is not a concurrent scope container. Unregistering does not cancel in-flight calls or dispose borrowed adapters.

File storage performs ordinary writes; interrupted writes and simultaneous writers require consumer policy. Exists/Delete retain synchronous exception behavior. ResourcesReader calls Unity APIs and belongs on the main thread.

##  Data and support boundaries

Read [migration][migration] for 1.x data, removed placeholder/API locations and XML key changes. The versioned [encryption format][encryption] owns protocol details and legacy limitations. IL2CPP consumers preserve the actual DTOs/converters used by reflection serializers; the sample includes a narrow link.xml. Testing one backend/model is not proof for arbitrary reflection models or other platforms.

Important alternatives and reasons are recorded in [boundary ADR][boundaries] and [encryption ADR][crypto]. Validation commands and coverage belong to [Tests][tests]; current host reports remain outside this package.

[migration]: docs/migration-2.0.md
[encryption]: docs/encryption-format.md
[boundaries]: docs/adr/0001-unity63-and-adapter-boundaries.md
[crypto]: docs/adr/0002-authenticated-encryption-and-legacy-reading.md
[tests]: Tests/README.md
