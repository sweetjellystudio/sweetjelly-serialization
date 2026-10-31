#   Unity 6.3 and serialization adapter boundaries

Status: accepted, 2026-10-31.

Serialization 2.0 targets Unity 6.3 and Bootstrapper/Shared 2.0. Its orchestration service already accepts ports, but App directly contained Newtonsoft/XML/AES implementations. Move those public concrete types into Framework.Serialization, keep App's port-based flow and retain the service contract and enum numbers. A separate Domain would add no independent rules. Keeping the adapters in App would avoid namespace/assembly migration, but would retain the technology coupling we intend to remove; concrete callers must update references in this major release.

Remove the throwing BinaryDataSerializer placeholder instead of advertising a nonexistent default codec. Preserve IBinarySerializer for consumer implementations and report missing/default Binary support explicitly. Bootstrap owns only the registration it created, with no new disposal contract for a service holding borrowed dependencies. Preserve existing Unity script/asset GUIDs so composition assets remain usable.
