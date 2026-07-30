# MSH API

`IMesh.BaseHeader` exposes the complete `0x368`-byte common MSH base header through `IMeshBaseHeader`. `MeshBaseHeader.SupportedVersion` is the only supported format version, and `MeshKind` distinguishes static geometry from dynamic effects.

Static meshes expose a read-only `TrailingHierarchyUnwindCount`. It is derived from the final render record's source depth and is validated when a file is read. Each static `IModelPart` exposes its raw `NextRecordMarker`; readers preserve its value while writers canonicalize the record sequence to marker `1` for every nonfinal record and `0` for the final record.

The former `Descriptor`, `MeshType`, `RegularMeshSubType`, and `MeshSubType` APIs were removed because the post-header static dword is a hierarchy unwind, not a Unit/Building subtype. Dynamic effect classification is exposed as `IDynamicPart.EffectType`.

## Footprints and horizontal extents

`IMeshBaseHeader.BoxPresenceMask` exposes the complete 32-bit box presence mask.
`IMeshBaseHeader.Footprint` exposes box heights and flags by logical index 0
through 15, regardless of their reverse physical order in the file. It also
preserves the four raw 32-bit coverage descriptors and four raw 64-bit coverage
bitmaps.

`IMeshBaseHeader.HorizontalExtents` exposes four unsigned 16-bit magnitudes in
positive Y, negative Y, positive X, and negative X order.

## Migration from the legacy model

The legacy model assigned incorrect meanings and signed types to these fields.
Update callers as follows:

| Legacy API | Replacement |
|---|---|
| `Template.Matrix` and `Template.Flag` | `BoxPresenceMask` |
| `TemplateDetails.SectionHeights` | `Footprint.BoxHeights` |
| `TemplateDetails.SectionFlags` | `Footprint.BoxFlags` |
| `TemplateDetails.SectionRotations` | `Footprint.CoverageDescriptors` |
| `TemplateDetails.SectionFlagRotations` | `Footprint.CoverageBitmaps` |
| `Boundaries.MaxY` | `HorizontalExtents.PositiveY` |
| `Boundaries.MinY` | `HorizontalExtents.NegativeY` |
| `Boundaries.MaxX` | `HorizontalExtents.PositiveX` |
| `Boundaries.MinX` | `HorizontalExtents.NegativeX` |

Box heights are now `ushort[]`, and horizontal extent properties are `ushort`.
Coverage values are intentionally raw because their derived bit layout is not a
rotation model.
