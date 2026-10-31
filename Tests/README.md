# Serialization tests

Add com.sweetjelly.serialization to the project's testables list with Unity Test Framework 1.6.0 available. Run EditMode assembly SWeetJelly.Serialization.Tests.EditMode and PlayMode assembly SWeetJelly.Serialization.Tests.PlayMode separately through Test Runner or the installed Pipeline test commands. Record actual Editor patch, platform/backend, nonzero counts and report path.

## EditMode

Existing generic JSON/XML, memory-file, secure JSON, Binary text failure Result and bootstrap registration tests are retained. Regressions cover object/Type overloads, fixed 1.x fixtures, authenticated random writes, independent OpenSSL compatibility, wrong keys, salt/IV/ciphertext/tag changes, unknown versions, malformed data, encrypted JSON/XML files, no plaintext fallback or read-time rewrite, custom/missing Binary adapters, specific dependency errors, DTD rejection, cancellation, IO/path errors and bootstrap registration ownership/retry/shutdown.

The [fixtures][fixtures] are immutable compatibility evidence. Do not regenerate legacy files using the current writer to make tests pass. Crypto tests intentionally perform a real expensive KDF; avoid zero-test/stale-report pass claims.

## PlayMode and consumers

Runtime tests exercise real temporary files for JSON/XML and encrypted XML plus bootstrap registration across frames. They delete only their own uniquely named files. Package tests do not change or test the user's actual save files.

The development host additionally verifies the actual imported sample, independent declared-dependency consumption, save's contract consumers, and Windows Mono/IL2CPP Player behavior with preserved DTOs. These are separate results, not proof of every game model, platform permission or generic AOT instantiation. Consumers must preserve reflection DTOs/converters as described in [migration][migration]. Actual transient reports belong to the host work record, not shipped package content.

[fixtures]: Fixtures/README.md
[migration]: ../docs/migration-2.0.md
