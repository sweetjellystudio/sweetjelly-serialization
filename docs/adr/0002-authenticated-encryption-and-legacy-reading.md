#   Authenticated encryption with read-only legacy compatibility

Status: accepted, 2026-10-31.

The 1.x encryption format derives a fixed IV and has no integrity check. This major release improves new writes while preserving old reads. Use a versioned AES-256-CBC Encrypt-then-HMAC-SHA256 envelope with independently derived encryption/authentication keys, random salt/IV and PBKDF2-HMAC-SHA512 (220,000 iterations). Verify the header and full payload using constant-time tag comparison before CBC decryption. GCM was considered, but constructing AesGcm on the actual Unity 6000.3.27f1 Mono Editor throws PlatformNotSupportedException. BCL CBC/HMAC avoids adding an external crypto dependency and must also be exercised in the required Windows Players.

Unprefixed 1.x Base64 data uses a private read-only legacy decryptor. New/unknown envelope failures never retry the legacy path or plaintext. Reading does not rewrite a file; a subsequent explicit save writes the new format. This retains compatibility at the cost of accepting old unauthenticated input: legacy reads are for existing trusted local data and cannot promise tamper detection. New data cannot be read by 1.x, so rollback needs a compatible reader or pre-upgrade backup. See [the protocol][protocol] and [migration][migration].

[protocol]: ../encryption-format.md
[migration]: ../migration-2.0.md
