# Model MSH framing and record extensions explicitly

`CommonMeshBaseHeader` owns and represents exactly the shared `0x368`-byte
record, including `MESH`, supported version 1, and mesh kind. A static mesh asset
retains the stored trailing hierarchy unwind count and validates it against the
expected value derived from its final source depth. Dynamic effect type belongs
to the dynamic effect extension. Top-level readers require archive framing and
reject payloads beginning directly at `MESH`; canonical authored assets receive
a creation GUID once and preserve it across writes.
