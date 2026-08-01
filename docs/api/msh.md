# MSH API

## Immutable assets and authoring

`MeshAsset` is a closed immutable hierarchy with `StaticMeshAsset` and
`DynamicMeshAsset` branches. `Match` handles both branches without concrete
casts. Every accepted asset receives a `MeshAssetLineageId`; static render and
source object identities are scoped to that lineage and are not serialized in
MSH bytes.

`StaticMeshBuilder` authors canonical source-object trees whose material
partitions become one authoritative `StaticRenderObjectSequence`. It derives
hierarchy flags, next-record markers, trailing hierarchy unwind, static archive
framing, a persistent creation GUID, the historical one-cell footprint,
horizontal extents, zero-filled vertex padding, triangle render-pass flags, and
canonically absent attachments. All finite and fixed-point conversions are
checked. `DynamicMeshBuilder` authors ordered trees
of recognized `DynamicEffectType` values with dynamic framing and the observed
zero/absent common-header profile. `CanonicalDynamicObject` copies constructor
collections, and build rejects cycles, reused instances, excessive depth,
excessive children, and excessive total object counts before serialization.

Expected authoring failures return `MshBuildResult<T>` diagnostics without a
partial asset. Complete exact construction is isolated in
`EarthTool.MSH.Expert.MshExpert`; it still passes through the bounded structural
decoder. Expert dynamic construction can preserve unrecognized numeric effect
and light values, exact extension fields, exact string bytes, and ordered nested
objects without weakening structural checks.

`StaticMeshAsset.Edit()` starts a one-shot edit session. `Commit` returns a new
immutable snapshot, operation diagnostics, and a `PreservationReport` whose
field paths are classified as retained, regenerated, invalidated, or
canonicalized. Retained objects keep lineage-scoped identities; removing and
adding a render object allocates a new identity. Source collections are copied
at builder and edit boundaries.

```csharp
var build = StaticMeshBuilder.Create(creationGuid, lineageId)
  .SetRenderObject(vertices, triangles)
  .Build();

if (build.TryGetValue(out var asset))
{
  var edit = asset.Edit()
    .ReplaceGeometry(asset.StaticRenderObjectSequence[0].Id, editedVertices, editedTriangles)
    .Commit();
}

var dynamicRoot = new CanonicalDynamicObject(
  DynamicEffectType.Group,
  [new CanonicalDynamicObject(DynamicEffectType.Smoke)]);
var dynamicBuild = DynamicMeshBuilder.Create(creationGuid, lineageId)
  .SetRoot(dynamicRoot)
  .Build();

var exact = MshExpert.CreateDynamic(serializedMsh, lineageId);
```

## Safe operations

`IMshReader`, `IMshValidator`, and `IMshWriter` provide the bounded,
result-based API for the safe MSH production slice. The reader copies
caller-owned input under a finite `MshOperationProfile`, returns an immutable
`MeshAsset`, and produces no partial value on failure or cancellation.

The supported branches are complete static render-object sequences with a
reconstructed source-object grouping view and complete ordered
`DynamicMeshAsset` trees. Every `DynamicObject` owns its inherited
`CommonMeshBaseHeader`, complete `DynamicEffectExtension`, and immutable ordered
children. `DynamicEffectExtension.EffectType` and `LightType` retain exact
numeric values; nullable known-value views name recognized values without
collapsing unrecognized ones. Its exact fixed extension, mesh-name bytes, and
texture-path bytes remain independently available. Dynamic child records begin
at bare `MESH` headers and never carry archive framing.

`MshOperationProfile` bounds total input and output, trailing bytes, static
record, geometry, animation, texture-path and hierarchy sizes, dynamic depth,
total dynamic objects, children per object, total dynamic string bytes, and
retained diagnostics. Limits are checked before child, record, or string
materialization. Domains assigned to later slices fail with `ETM1005` rather
than being silently dropped. `IMshWriter.WriteFileAsync` validates and stages a
sibling temporary file before atomically replacing an existing destination.
Stream overloads leave caller-owned streams open.

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
the corresponding radian view. `ExtraAngle` preserves the exact eighth byte.
For cannon slots, `ExtraAngleRadians` exposes its confirmed quantized-angle
encoding using 256 units per turn. Assigning `Slot.ExtraAngleRadians` truncates
to format units and wraps negative angles into the unsigned byte. The ordinary
AOD slot form defaults this angle to `0x80`, or pi radians. Light-generated
attachment bytes are preserved raw and are not interpreted as slot angles.

`ISlot.IsValid` is false only when all three stored coordinates are the
`0x8000` sentinel. Coordinate values use signed fixed point with 256 units and
invert Y between the file and object model. COLLADA conversion retains sparse
one-based numbers within each attachment range. For exported slot nodes,
EarthTool stores the raw extra angle in `EARTHTOOL` attachment metadata;
external COLLADA slot nodes without that metadata use the ordinary `0x80`
default. Static-light attachment conversion is handled separately from slot
node metadata.

## Static lights

`IMeshBaseHeader.SpotLights` and `OmnidirectionalLights` are indexed read-only
lists containing the four physical records in source-number order. A static
light uses composition through `IStaticLight`: `Position` is its full-precision
`Vector3`, and `LightParameters` is the raw three-float parameter vector. These
values are never quantized or restricted to the display-color range.

Spot records expose `HorizontalTargetDistance`, the exact one-byte
`TargetHeading`, its derived `TargetHeadingRadians` view, `Reserved1` through
`Reserved3`, `ConeHalfAngleTangent`, `DistanceScaledCone`,
`VerticalTargetSlope`, and `FinalParameter`. Omni records expose their final
unidentified float as `FinalParameter`.

Light activity comes only from the corresponding attachment at the same list
index: spot records use `Slots.Headlights`, and omni records use
`Slots.Omnilights`. Full-precision light positions and quantized attachment
positions are independent serialized fields. MSH writing neither synchronizes
nor rejects differences between them.

Static COLLADA export preserves source numbers in names `SpotLight-1` through
`SpotLight-4` and `OmniLight-1` through `OmniLight-4`, omitting records whose
corresponding attachment is inactive. Standard COLLADA fields contain white
color, position, and the confirmed spot cone angle. The complete active MSH
record is invariant text in the unqualified version-1 payload
`<extra><technique profile="EARTHTOOL"><msh_static_light version="1">`.

COLLADA import prefers valid EarthTool metadata. Without metadata, a light must
still use the numbered name, standard color becomes `LightParameters`, and only
confirmed standard fields are mapped; other values and reserved bytes become
zero. Unsupported versions, incomplete or malformed metadata, source-number
conflicts, duplicates, out-of-range numbers, unnumbered lights, and more than
four records are invalid input. If an imported active light position would
quantize to the three-coordinate absence sentinel, EarthTool moves only the
attachment X coordinate by one fixed-point unit so the light remains active;
the full-precision light position remains exact.

## Dynamic objects

`DynamicEffectExtension.TerrainLightColor` and `VisibleEffectColor` expose their
serialized three-float vectors as `Vector3`.
`VisibleTerrainLightGain` preserves the separate fourth visible-color value.
The start and end effect rectangles, frame declarations, reciprocals, signed
ribbon half-width, reserved word, raw additive flag, alpha values, scales, and
child translations are exposed independently. Child translation Y is converted
between serialized and MSH coordinates; the exact `SerializedRepresentation`
remains available for binary authority and round trips.

Dynamic glTF transport is not part of this API. The glTF facade continues to
accept only `StaticMeshAsset`.

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
| `Slot.Flag` or `Slot.FinalParameter` | `Slot.ExtraAngle` |
| `ILight` / `Light` | `IStaticLight` / `StaticLight` |
| static `Light.Value` | `StaticLight.Position` |
| static `Light.Color` | `StaticLight.LightParameters` |
| `Light.IsAvailable` | corresponding `ISlot.IsValid` |
| `SpotLight.Length` | `SpotLight.HorizontalTargetDistance` |
| `SpotLight.Direction` | `SpotLight.TargetHeading` and `Reserved1..3` |
| `SpotLight.Width` | `SpotLight.ConeHalfAngleTangent` |
| `SpotLight.U3` | `SpotLight.DistanceScaledCone` |
| `SpotLight.Tilt` | `SpotLight.VerticalTargetSlope` |
| `SpotLight.Ambience` | `SpotLight.FinalParameter` |
| `OmniLight.Radius` | `OmniLight.FinalParameter` |
| `DynamicPart.LightColor` | `DynamicPart.LightVector` |
| `DynamicPart.Color` | `DynamicPart.ColorRgb` |
| `DynamicPart.ColorIntensity` | `DynamicPart.ColorParameter` |

Box heights are now `ushort[]`, and horizontal extent properties are `ushort`.
Coverage values are intentionally raw because their derived bit layout is not a
rotation model.
