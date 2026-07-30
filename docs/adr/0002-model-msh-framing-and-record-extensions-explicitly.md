# Model MSH framing and record extensions explicitly

`MeshBaseHeader` owns and represents exactly the shared `0x368`-byte record, including `MESH`, supported version 1, and `MeshKind`. A static mesh exposes a read-only `TrailingHierarchyUnwindCount` derived from its final source depth, while dynamic effect type belongs to the dynamic extension. Top-level readers require kind-compatible canonical archive framing and reject payloads beginning directly at `MESH`; newly authored files receive a GUID once at creation and preserve it across writes.
