#   Encrypted text format

`AesStringEncryptor` writes `SJENC:1:` followed by standard Base64 of `salt || IV || ciphertext || tag`. The envelope version is independent of the package's semantic version. All sizes below are bytes.

*   Salt: 16 bytes from the cryptographic RNG, fresh per write.
*   IV: 16 independent random bytes per write.
*   Key derivation: PBKDF2-HMAC-SHA512, 220,000 iterations, UTF-8 password, 64 derived bytes. The first 32 bytes are the AES key and the last 32 are the HMAC key. The fixed version defines the work factor; input cannot request unbounded KDF iterations.
*   Plaintext: strict UTF-8. AES-256-CBC, PKCS#7 padding, nonempty block-aligned ciphertext (including encrypted empty text).
*   Tag: HMAC-SHA256 of ASCII `SJENC:1:` concatenated with salt, IV and ciphertext. All 32 bytes are compared in constant time before attempting CBC decryption.

Unknown versions, malformed/truncated payloads, wrong keys and invalid tags fail. No plaintext or legacy retry follows an identified new-envelope failure. Keys are supplied per call, never stored in files or bootstrap assets. The package does not provide a keystore or protect a key embedded in a shipped client. The KDF is synchronous and intentionally costly; avoid performing repeated encryption on the rendering thread without considering application latency. Cancellation cannot interrupt a KDF already executing. Empty keys are rejected.

Unprefixed 1.x Base64 ciphertext retains its original fixed salt, PBKDF2-HMAC-SHA1/1,000 derivation and UTF-16LE decoding for reads only, including its historical space-to-plus normalization. This path has no authentication and cannot reliably detect tampering or every wrong-key case. It is a compatibility concession for trusted historical local files, not the new write format. A consumer requiring strictly authenticated input can supply its own IStringEncryptor enforcing its accepted formats.

The format's integrity construction follows [Microsoft's CBC guidance][cbc]. The work factor uses the published [OWASP PBKDF2-HMAC-SHA512 guidance][pbkdf2] as a password-strengthening baseline; this is not a claim of FIPS validation or independent security certification. Changes to the wire format/KDF require a new version and compatibility tests, not edits to version 1 in place.

[cbc]: https://learn.microsoft.com/en-us/dotnet/standard/security/vulnerabilities-cbc-mode
[pbkdf2]: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html#pbkdf2
