#   Serialization package instructions

This package owns serialization orchestration, codec/encryption adapters and file access adapters. Target Unity 6.3 LTS or later; record the exact tested Editor patch and Player backend.

*   Read [architecture][architecture] before changing APIs, assembly dependencies, registration ownership or adapter responsibilities. Read relevant [decisions][decisions] before changing stored formats or compatibility.
*   Keep `SerializationService` dependent on contracts and Shared.Result. JSON/XML/AES implementations belong to Framework.Serialization; do not restore its Newtonsoft dependency to App or add a Domain assembly without actual rules to protect.
*   Preserve enum numbers and existing asset/script GUIDs. Concrete adapter namespaces changed in 2.0; [migration][migration] owns that guidance.
*   Read [the encrypted format][encryption] before touching cryptography. Every new-format authentication failure is final. Legacy ciphertext reading is a migration path, not authenticated data validation.
*   Package correctness tests live in Tests; Samples~ owns the distributable example. Read [test instructions][tests] for suites, fixtures and Player coverage. Host integration and transient validation reports belong to the development host.
*   Use the installed unity-package harness for implementation and verification. Publishing requires a separate request; the user handles merge and optional tags. Keep actual validation results distinct from supported APIs and untested targets.

[architecture]: ARCHITECTURE.md
[decisions]: docs/adr/0001-unity63-and-adapter-boundaries.md
[migration]: docs/migration-2.0.md
[encryption]: docs/encryption-format.md
[tests]: Tests/README.md
