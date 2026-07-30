# MSH API

`IMesh.BaseHeader` exposes the complete `0x368`-byte common MSH base header through `IMeshBaseHeader`. `MeshBaseHeader.SupportedVersion` is the only supported format version, and `MeshKind` distinguishes static geometry from dynamic effects.

Static meshes expose a read-only `TrailingHierarchyUnwindCount`. It is derived from the final render record's source depth and is validated when a file is read. Each static `IModelPart` exposes its raw `NextRecordMarker`; readers preserve its value while writers canonicalize the record sequence to marker `1` for every nonfinal record and `0` for the final record.

The former `Descriptor`, `MeshType`, `RegularMeshSubType`, and `MeshSubType` APIs were removed because the post-header static dword is a hierarchy unwind, not a Unit/Building subtype. Dynamic effect classification is exposed as `IDynamicPart.EffectType`.

## Static geometry

Static render vertices expose all three stored texture components through
`ITextureCoordinate.U`, `V`, and `W`. `V` uses the object-model orientation and
is inverted when read from or written to MSH; `W` preserves the raw third
component whose game-facing meaning remains unknown.

`IVertex.NormalVectorIdx` and `PositionVectorIdx` are unsigned sharing links.
The value `0xFFFF` means that no earlier render vertex is shared. `IFace.V1`,
`V2`, `V3`, and `Flags` preserve the four unsigned 16-bit triangle words.
The COLLADA converter carries triangle flags in its `EARTHTOOL` static-part
metadata and uses the base value `0x0001` when importing external COLLADA files
without that metadata.

Readers require the stored vertex-block count to equal
`ceil(vertexCount / 4)` and reject triangle indices outside the declared vertex
range. Unused lanes in a partial final block are not exposed as vertices and
are normalized to zero when written.

## Attachments

`IMeshBaseHeader.Slots` always contains the 49 physical attachment records.
Each `ISlot.Id` is its stable one-based global record position. `Heading`
preserves the exact stored byte using 256 units per turn, while `Direction` is
the corresponding radian view. `FinalParameter` preserves the independent
eighth byte, including zero.

`ISlot.IsValid` is false only when all three stored coordinates are the
`0x8000` sentinel. Coordinate values use signed fixed point with 256 units and
invert Y between the file and object model. COLLADA conversion retains sparse
one-based numbers within each attachment range.

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
| `Face.UNKNOWN` | `Face.Flags` |
| `Slot.Flag` | `Slot.FinalParameter` |

Box heights are now `ushort[]`, and horizontal extent properties are `ushort`.
Coverage values are intentionally raw because their derived bit layout is not a
rotation model.
