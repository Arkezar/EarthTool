# MSH API

`IMesh.BaseHeader` exposes the complete `0x368`-byte common MSH base header through `IMeshBaseHeader`. `MeshBaseHeader.SupportedVersion` is the only supported format version, and `MeshKind` distinguishes static geometry from dynamic effects.

Static meshes expose a read-only `TrailingHierarchyUnwindCount`. It is derived from the final render record's source depth and is validated when a file is read. Each static `IModelPart` exposes its raw `NextRecordMarker`; the final marker must be zero.

The former `Descriptor`, `MeshType`, `RegularMeshSubType`, and `MeshSubType` APIs were removed because the post-header static dword is a hierarchy unwind, not a Unit/Building subtype. Dynamic effect classification is exposed as `IDynamicPart.EffectType`.
