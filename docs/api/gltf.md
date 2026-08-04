# glTF API

`GltfInterchange` is EarthTool's sealed facade over `SharpGLTF.Core`. No
SharpGLTF type appears in the public API. Static source objects are emitted as
native nodes and meshes. Their child relationships remain native node
relationships, and each material partition becomes one triangle primitive.
The effective first-partition pivot is emitted as node translation. EarthTool
does not add an axis-conversion root.

`ExportGlbAsync` emits a standard glTF 2.0 +Y-up GLB with positions, normals,
texture coordinates, and unsigned-short indices. A partition that references
vertex index `65535` uses unsigned-int indices so the maximum unsigned-short
restart value is never emitted. `ExportGltfFileAsync` emits the same projection
as a JSON manifest plus content-addressed `.bin` and decoded `.png` sidecars.
Identical canonical preview pixels share one image sidecar. The exporter
validates every referenced sidecar and commits the manifest last, so a
committed package never references missing or partial output. A failed
operation may leave an unreferenced content-addressed sidecar.

`GltfExportOptions.TextureSearchRoots` supplies ordered absolute roots for TEX
preview lookup. Lookup is component-wise and ASCII case-insensitive, rejects
symlink escapes, uses the first matching root, warns when later roots are
shadowed, and rejects case-ambiguous matches in the winning root. An unresolved
binding remains authoritative: EarthTool previews `Textures\Default.tex` when
available, otherwise it emits a deterministic diagnostic image. Special TEX
resources use their first highest-resolution image and report that variants are
not represented. `GltfOperationProfile` bounds aggregate TEX bytes and decoded
pixels as well as the number of search roots and directory entries examined.

`GltfExportOptions.MeshResourceSearchRoots` separately supplies ordered absolute
roots for `ScalableObject` preview lookup. A binding `name` is resolved with the
game-compatible `Meshes\name.msh` spelling by ASCII case-insensitive component
matching. Empty, rooted, non-ASCII, slash-containing, and traversal bindings are
never used as host paths. Linked roots and linked path components are skipped,
the first unambiguous root wins, and later matches are reported as shadowed.
`GltfOperationProfile` independently bounds MSH roots, examined directory
entries, resolved resources, aggregate bytes, and aggregate preview vertices.
Dynamic-to-dynamic references are not preview geometry; EarthTool traverses only
their referenced-binding graph to diagnose cycles, bounded by the configured MSH
resource depth, then emits the deterministic placeholder. Supply non-default
values through `GltfMeshResourceLimits`; existing operation-profile constructor
signatures remain unchanged.

Both exports assign asset-lineage and document identities and write compact
version-1 EarthTool envelopes as string-valued `extras["earthtool"]`. Each
envelope declares `format`, `version`, `kind`, lowercase version-4 `lineage` and
`document` UUIDs, a bounded local `id`, structured SHA-256 `guards`, and a
kind-specific `payload`. The scene manifest owns strictly increasing scope
inventories and retained identity high-water marks. Opaque metadata bytes use
unpadded base64url. Partition fingerprints ignore index width, vertex
numbering, cyclic triangle-index rotation, and triangle order. They retain
winding and triangle multiplicity. The manifest records the preserved MSH byte
length and SHA-256 as informational provenance; a contradiction reports
`ETG2017` but never authorizes preservation state.

## Dynamic effect-preview contract

Dynamic exports use additive overloads that accept `DynamicMeshAsset`; existing
static overloads remain strongly typed. Metadata version 2 identifies the
dynamic package explicitly. The manifest contains the complete exact source MSH,
an ordered object-scope inventory, a non-reusable next-ID boundary, and the
historical `dynamic-group-explosion-preview` projection name/version/fingerprint,
which remains stable for version 1 packages while covering the expanded sprite
and ribbon families plus attached-particle and procedural previews. Every
object node has one stable local scope ID, its exact effect declaration, its
ordered child IDs, exact inherited header and effect bytes, resource bindings,
and a named/versioned ordered-child guard. `Shockwave`, `Line`, `Sphere`, and
`Keelwater`, and `ScalableObject` preview scopes additionally record and guard their evaluation
context, frame domain and selected frame, total and remaining lifetime, global
tick, texture scale, lifetime progress, and parent phase. `ScalableObject` also
records the selected model-scale phase.

`Group` is a native transform node with ordered children and no mesh, material,
or synthetic effect geometry. `Explosion`, `Track`, `MappedExplosion`,
`FlatExplosion`, and `Smoke` receive unlit, double-sided stock-Blender preview
quads. `Laser`, `LaserWall`, `ElectricalCannon`, and `Lightning` receive
unlit, double-sided triangle-strip ribbon previews. `Shockwave`, `Line`, and
`Keelwater` receive explicitly attached-particle billboard snapshots. `Sphere`
receives a deterministic generated unit-sphere snapshot. `Explosion` retains its
version 1 declaration-start billboard. The framed effects use explicit
deterministic inputs: total lifetime 100 ticks,
remaining lifetime 100 ticks, global tick 0, texture scale 1, lifetime alpha
progress 0, sampled terrain light `(1,1,1)`, output color scale 1, and terrain
light random sample 0. The `Keelwater` preview uses fixed water RGB
`(0.2,0.45,0.7)`. A parent preview phase of 0 selects child start translation.
`ScalableObject` receives the flattened static geometry of its referenced MSH as
one borrowed, unlit preview primitive and applies its selected uniform model
scale on the dynamic node. Referenced static animation, hierarchy, attachments,
lights, metadata, and geometry authority are not imported into the dynamic
asset. Missing, ambiguous, unsafe, malformed, or dynamic references retain the
exact binding and use a deterministic triangle placeholder with scoped
diagnostics.

Finite, changing lifetime-driven transforms export as one `EarthTool Dynamic
Preview` action. Linear child translation channels use each child's `Position`
and `Position2`; linear `ScalableObject` scale channels run from the guarded
initial preview scale to its end `Scale`. The deterministic 100-tick preview
runs for five seconds at the normal 20-update scheduler rate. Periodic
global-tick transforms, atlas changes, and
material alpha remain snapshot metadata because their runtime timing or target
properties do not have an equivalent portable core glTF animation contract.
The generated action is an artist preview; dynamic import continues to own MSH
edits through the node rest transform and exact metadata rather than animation
channel edits.

Core glTF material factors are limited to zero through one. Finite MSH colors,
alpha, or gains that evaluate outside that range are clamped only in the preview
and made read-only for that native material; their exact authoritative values
remain in scoped metadata and round-trip unchanged. Non-finite active values are
rejected instead of being normalized.

The native preview behavior is effect-specific:

| Effect | Shape and TEX region | Color, alpha, light, and hierarchy |
| --- | --- | --- |
| `Track` | Horizontal XZ quad approximating the runtime terrain-cell decal bounds; full decoded TEX image because atlas declarations are inactive | Neutral white deterministic terrain color; semantic alpha; no terrain-light preview; ordered children and child translation remain native |
| `MappedExplosion` | Horizontal XZ quad approximating the runtime terrain-cell decal bounds; full decoded TEX image | Serialized visible RGB and semantic alpha; evaluated terrain light remains metadata-only; ordered children participate normally |
| `FlatExplosion` | Horizontal XZ quad at the serialized local-plane depth; UVs use the selected source frame and exact reciprocal atlas values | Serialized visible RGB and semantic alpha; evaluated terrain light remains metadata-only; ordered children participate normally |
| `Smoke` | Vertical XY billboard snapshot at the camera-depth offset; UVs use the selected source frame and exact reciprocal atlas values | Visible RGB is modulated by the explicit white terrain sample and serialized gain; semantic alpha; no terrain-light producer; ordered children participate normally |
| `Shockwave` | Vertical XY billboard snapshot at the camera-depth offset; UVs use the attached-particle selected source frame and exact reciprocal atlas values | Explicitly uses attached-particle RGB modulation and frame-phase alpha; primary dispatch has no own geometry or terrain light; ordered children still use normal hierarchy dispatch |
| `Line` | Same attached-particle billboard contract as `Shockwave` | Explicitly uses attached-particle RGB modulation and frame-phase alpha; primary dispatch has no own geometry or terrain light; ordered children still use normal hierarchy dispatch |
| `Keelwater` | Vertical XY billboard snapshot at the camera-depth offset; UVs use its dedicated attached-particle frame selection | Uses fixed preview water RGB and frame-phase alpha; primary dispatch has no own geometry or terrain light; serialized visible RGB/gain remain inactive and exact |
| `Sphere` | Generated unit sphere with full decoded TEX preview; metadata identifies the selected `builtIn16` lifetime frame | Serialized visible RGB and additive selection are represented; ordinary frame, rectangle, alpha, and scale declarations remain inactive and exact; ordered children participate normally |
| `ScalableObject` | Borrowed referenced static MSH geometry with the selected atlas texture and uniform interpolated model scale | Exact mesh-name binding remains dynamic authority; referenced geometry is preview-only; scale, material, ordered children, and child translation participate in the dynamic edit contract |

The attached-particle billboards are metadata-backed demonstrations of the
confirmed attached route. A dynamic hierarchy still invokes these records by
primary dispatch, where `Shockwave`, `Line`, and `Keelwater` have no own visible
geometry. Exporting their billboards does not change that contract or infer a
game attachment. `Sphere` frame selection calls its confirmed hardcoded
`((D-R)<<4)/D` domain and clamps values above 15; it never authors or interprets
ordinary serialized frame declarations. The selected frame is preview metadata,
not a new MSH field. Generated sphere shape edits and runtime-dependent preview
inputs remain preview-only and are reported without rewriting MSH.

Ribbon previews use explicit runtime-only inputs that are not serialized MSH
state. `Laser` and `LaserWall` use the centerline `(0,0,0)` to `(8,0,0)` and a
`+Y` heading side. `ElectricalCannon` uses the same endpoints, 21 center pairs,
and a fixed bounded integer-derived deviation sequence. `Lightning` uses 31
center pairs from `(0,12,0)` to `(0,0,0)`, a `+X` side, and a separate fixed
bounded deviation sequence. These inputs replace runtime entity endpoints,
heading, global angle, `RDTSC`, and shared CRT random state only for the stock
preview. They are fingerprinted projection inputs, never claims about the
serialized representation.

| Effect | Ribbon, texture, material, and light preview | Hierarchy and authority |
| --- | --- | --- |
| `Laser` | One segment between the deterministic endpoints; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND`; its evaluated modulated terrain-light line remains metadata-only | Ordered children and child translation are native; frame/atlas, additive integer, light, resources, and inactive fields remain exact |
| `LaserWall` | The same one-segment geometry; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND`; its unmodulated terrain-light line remains metadata-only | Ordered children participate normally; light type remains inactive and exact while terrain-light RGB remains authoritative |
| `ElectricalCannon` | Twenty deterministic segments communicate the adaptive jagged path without pretending to reproduce runtime randomness; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND` | Ordered children participate normally; runtime subdivision/RNG and inactive terrain-light representations remain metadata-only |
| `Lightning` | Thirty deterministic segments communicate the fixed vertical bolt; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND`; its modulated terrain-light line remains metadata-only | Ordered children participate normally; runtime endpoint/global-angle/RNG inputs and unrelated state remain metadata-only |

Each ribbon pair stores logical left then logical right, with atlas U assigned by
that logical side and V advancing along the path. Indices retain
`(L[i],L[i+1],R[i])`, `(R[i],L[i+1],R[i+1])`. The serialized
ribbon half-width is used with its sign: negating it exchanges physical sides,
mirrors texture orientation, and reverses winding. Zero or non-finite serialized
ribbon half-width is preserved by the MSH binary API but cannot form a preview
and fails export with scoped `ETG1006`.

TEX lookup uses the same bounded root order and diagnostic/default fallback as
static materials. Missing or unsafe resources retain their exact binding and
receive deterministic diagnostics and, where available, the diagnostic preview.
Core glTF `BLEND` is only an artist preview: it cannot express the game's
additive `ONE,ONE` behavior. Core glTF also cannot express runtime billboard
orientation, terrain tessellation, frame advance, terrain lighting, or random
light behavior. Exact additive flags, resource bytes, light values, frame and
atlas declarations, reciprocals, and inactive representations therefore remain
metadata authority rather than claims of runtime equivalence.

Native edits own ordered parent/child relationships, child start translation,
and the displayed rectangle and alpha start representation. Depth edits are
owned by `Explosion`, `FlatExplosion`, `Smoke`, and attached billboards; visible
RGB edits are owned by every preview except `Track` and `Keelwater`. Smoke,
Shockwave, and Line RGB edits are inverted through the documented deterministic
light sample only when their gain is invertible. Sphere material RGB owns its
serialized visible color while generated sphere geometry remains preview-only. Import
retains end translation, end rectangle, full frame domain, alpha timing and end
alpha, exact additive integer, light type/color/gain, reserved and inactive
fields, resource bytes, inherited headers, and trailing bytes. Unchanged import
returns the exact source representation. Invalid active frame or atlas capacity,
and non-finite active value domains report scoped `ETG1006` diagnostics without
partial output. Duplicate, missing, foreign, stale, dangling, or ambiguous
scopes fail before a dynamic snapshot is returned. `ScalableObject` uniform
scale edits regenerate only the selected start-scale representation, and an
explicit scoped `meshName` metadata edit replaces only the exact variable-length
binding. Borrowed preview geometry is never written to the referenced MSH.
Unknown serialized values remain exact and are never converted to catch-all enum
values.

Ribbon material edits own representable visible RGB and alpha start values.
Ribbon pair spacing owns the signed `RibbonHalfWidth`; import requires one
finite, nonzero, consistent ribbon half-width, finite normals, nondegenerate path
segments, fixed side UVs, and guarded index topology. Centerline and in-plane
orientation edits are accepted as runtime-only preview input and reported
separately without rewriting MSH fields; a later MSH export regenerates the
documented deterministic path.
Shared or overlapping POSITION, NORMAL, TEXCOORD, or index ownership is
ambiguous and fails transactionally. Frame declarations, reciprocal atlas
values, flags, terrain colors/gains, resource bytes, reserved values, inactive
rectangles/depth/model scales, and child end translations remain exact.

Use `ImportEditDynamicGlbAsync` or `ImportEditDynamicGltfFileAsync` when the
caller knows the package kind. `ImportEditMeshGlbAsync` and
`ImportEditMeshGltfFileAsync` preserve the existing kind-specific APIs while
allowing CLI and batch callers to accept either static or dynamic packages.

Import plans and machine reports are separate protocols from embedded metadata.
Version 1 plans use format `earthtool.msh.import-plan`; version 1 reports use
format `earthtool.msh.cli-report`. `GltfImportPlanFormat` and
`GltfCliReportFormat` expose their supported versions independently, so changing
one protocol does not reinterpret either of the others.

`GltfImportPlanSerializer` accepts only the closed typed values represented by
`GltfNewModelImportOptions` or `GltfEditImportOptions`. New-model plans may carry
TEX bindings, footprint and extent values, object roles, helper bindings,
MSH-only light values, and animation classes. Edit plans may carry exact
baseline-bound conflict actions. Unknown members and raw metadata, MSH bytes,
adapter objects, guards, and expert state are rejected. A plan also binds its
intent, package kind, and a lowercase SHA-256 source digest. GLB uses the exact
file bytes. Separate glTF uses a domain-separated digest over the exact manifest,
buffer, and referenced images, with sidecars ordered by ordinal URI. Use
`ComputeGlbSourceSha256Async` or `ComputeGltfSourceSha256Async` to produce the
binding. The `WithPlanAsync` import methods capture and verify those same bytes
before opening an import transaction. Malformed, unsupported, excessive, stale,
and mismatched plans report `ETG3000` through `ETG3004` without returning a
partial asset.

`GltfCliReport` collects complete export, edit-import, new-model-import, and
validation outcomes. `GltfCliReportSerializer` writes fixed-order UTF-8 JSON with
an invocation status and every operation's input, destination, package kind, asset kind,
status, diagnostics, identities, fingerprint, applied conflict actions, restored
paths, and preservation effects. Operation order and diagnostic/preservation
order are retained; diagnostic data keys are sorted ordinally. Reports contain
no timestamps or host-generated paths, and repeated serialization of the same
outcomes is byte-identical.

Effective animation classes A through D export as `EarthTool A` through
`EarthTool D` clips. Each participating source object has explicit dense
translation, canonical-quaternion rotation, and scale channels sampled at
`frame / 24` with `LINEAR` interpolation. Absent MSH tracks contribute their
runtime identity or pivot fallback without losing their exact absent state.
Blender activates only the first imported glTF animation and stores the other
class clips as muted, zero-influence NLA strips. Activate the desired Action
and its object slot to preview that class.

[F3D](https://f3d.app/) is recommended for previewing all independent class
animations together. Select **All animations** in F3D, or launch it with:

```bash
f3d --animation-indices=-1 --animation-autoplay=true model.glb
```

Declared zero-length animation with present tracks projects effective frame
zero; longer serialized tails remain metadata and do not extend the clip.
Unrecognized serialized animation-class values remain exact and report
`ETG1015`, while their native projection uses the game's modulo-four class
selection.

## Static-light authoring contract

`ET_SpotLight_1` through `ET_SpotLight_4` and `ET_OmniLight_1` through
`ET_OmniLight_4` identify compound static-light artist objects. Each must be a
leaf node and own one unshared `KHR_lights_punctual` definition. Canonical node
and definition names must agree with each other and with the definition's
`spot` or `point` type. Node pose, linear RGB, and valid spot cone values author
the corresponding static-light representations.

For new-model spot lights, a positive `range` authors approximate target
distance. `GltfNewModelStaticLightOptions.TargetDistance` supplies the value
only when range is absent; a differing range and typed value is rejected.
`TerrainLightAmplitude` is also a typed MSH-only value. It defaults to `1.0` and
does not use glTF photometric intensity. A non-default ignored intensity reports
`ETG1028`. In a matching edit projection, intensity remains the projected
terrain-light amplitude, and a missing range preserves the loaded target
distance because its absence is not deletion evidence.

Renaming within the same canonical light family retargets the artist object to a
free physical number while preserving its scope identity and compatible owned
state. Spot/omni conversion is rejected. Deleting the artist object clears both
its activity attachment and complete static-light record. Duplicate targets,
shared definitions, type contradictions, and occupied retargets fail without a
partial asset.

Animation guards bind the class, declaration, participating object, and dense
binary32 TRS values rather than clip names, array order, accessor layout, or
quaternion sign. Unchanged edit import restores exact independent scale,
translation, and matrix tracks, including constant tracks and retained tails.
Edited `LINEAR`, `STEP`, and `CUBICSPLINE` channels are evaluated on the guarded
integer-frame grid and regenerate dense canonical scale, translation, and pure
rotation-matrix tracks only for the affected object/class. Sparse and subframe
keys are sampled rather than inferred or preserved as MSH key intent; cubic
tangents and source interpolation are not retained. Deleting a native clip
clears only its participating tracks while keeping the independent class and
header declarations. Ambiguous class ownership, duplicate clips, targets that
do not match an editable metadata scope, and samples beyond a guarded class
declaration fail with `ETG2008`. A finite affine frame that cannot be decomposed
and recomposed within the binary32 tolerance remains metadata-only for that one
object/class and reports `ETG1014`; other objects and classes still export.

`ImportEditGlbAsync` and `ImportEditGltfFileAsync` require the expected lineage
and document identities. They validate metadata carriers, object and mesh
ownership, the complete scope inventory, native hierarchy, projection name and version, and partition
fingerprints before reconciling serialized MSH state. Applicable metadata
restores exact topology and guarded state, including duplicate or unreferenced
vertices, degenerate triangles, lane padding, sharing links, and triangle
flags. Same-surface primitive splits, merges, and reordering without distinct
material ownership are representation-only and restore the original partition
boundaries. A uniquely corresponding topology, winding,
normal, UV, or multiplicity edit canonically regenerates only that partition;
unaffected records remain exact. Unique partition deletion and copy/fork edits
retain existing identities and allocate fresh identities for canonical copies.
Ambiguous duplicate correspondence fails with `ETG2012` and no partial asset.
A deliberate hierarchy edit switches from the preserved source sequence to the
canonical first-partition, child-subtree, remaining-partition order. Import then
regenerates hierarchy unwind and nesting flags, canonical `1`/`0` next-record
markers, and trailing unwind while retaining unrelated record and common-header
state. Node translation regenerates only the effective pivot. Static rotation
and scale regenerate the affected object's geometry and normals, reversing
winding once for a reflection. Transform-only grouping nodes collapse into the
descendant local transform.

Source-object deletion retains identity high-water marks, so gaps are never
reused after re-export. An untagged copy gets fresh source-object and
render-object identities whether it keeps a linked glTF mesh or receives a
single-user mesh copy with an explicitly forked mesh identity. Its MSH records
are canonical and do not inherit raw padding, sharing links, or source-only
flags. Duplicate object or mesh metadata blocks until an explicit fork
resolution removes the old identity. An untagged object is new only when it
cannot be confused with a missing expected scope. Ambiguous identity blocks
with `ETG2012`.
A successful import retains the asset lineage and rotates the document
identity. `GltfEditImportResult.Preservation` reports retained, regenerated,
invalidated, and canonicalized MSH paths.

New-model import admits animation only through unique `EarthTool A` through
`EarthTool D` names. One mesh object may participate in at most one class. Each
clip must end on an integer 24 FPS frame from index `0` through `254`; EarthTool
samples that complete range into canonical dense tracks and rejects frame index
`255` or later. This conversion does not infer sparse MSH keys, TCB state,
source interpolation, cubic tangents, or unsupported matrix components. Native
rest TRS supplies unanimated paths without being baked into geometry a second
time. Animation on a collapsed transform-only parent is accumulated onto its
mesh descendants; an accumulated transform that cannot be represented as pure
TRS is rejected. Generated animation bytes participate in output-limit checks
before dense sample arrays or MSH output are materialized.

`ValidateGlbAsync` and `ValidateGltfFileAsync` validate packages without
materializing MSH output. All import and validation operations enforce finite
positions, normals, and texture coordinates; triangle or non-indexed triangle
topology; valid unsigned-byte, unsigned-short, or unsigned-int indices; required
texture coordinates; and `GltfOperationProfile.MaxActiveRenderVertices`.
The profile also bounds decoded bytes per envelope and cumulatively, envelope
count, JSON depth and elements, guards, unknown additive members, and retained
metadata. `MaxMetadataConflicts` bounds the stable pre-reconciliation conflict
inventory. If additional conflicts would be hidden, the final retained entry is
`ETG2019`. Metadata exhaustion reports `ETG2005` before reconciliation.
Malformed carriers, envelopes, identities, kinds, duplicates, and inventories
use their assigned `ETG2000` through `ETG2020` conflicts. Diagnostics expose a
deterministic conflict key and the complete allowed action identifiers from
`GltfMetadataConflictCatalog`. Pass exact keyed
`GltfMetadataConflictResolution` values through `GltfEditImportOptions` to
`ImportEditGlbWithResolutionsAsync` or `ImportEditGltfFileWithResolutionsAsync`
and retry the same input. Keys bind the complete conflict and caller-expected
baseline; stale, duplicate, mismatched, disallowed, incomplete, or replayed
resolutions fail before an edit session is opened. Scope mapping requires an
explicit native carrier path. Forking drops identity-bound metadata and lets
native-addition reconciliation allocate fresh IDs. Branch acceptance retains
lineage, while adopt-as-new and discard-lineage remove all metadata authority
and canonically admit native content under fresh identities. `abort`,
`retryWithMetadata`, and `repairNativeExternally` remain non-committing
instructions until the caller supplies changed input. Successful results report
applied actions and lineage disposition; `AppliedFingerprint` is null when the
old lineage was discarded.
Unknown additive version-1 members retain their
exact raw JSON tokens in `GltfEditImportResult.PreservedUnknownMetadata` but do
not gain identity, guard, reference, action, or trust semantics. Passing
`NextExportOptions` to the next export rewrites those tokens into the rotated
baseline. Dictionary keys combine scope identity with an escaped JSON Pointer
to the additive member. Unsupported versions remain opaque and are never
partially salvaged. Missing expected scopes, dangling references, unsupported
or stale guards, and ambiguous native correspondence are inventoried before an
edit session is opened, so any conflict returns no partial MSH asset. Native
projection normalization remains domain-specific; no global floating-point
epsilon is applied.

Path-based exports stage sibling temporary files and replace the destination
only after conversion and validation succeed. SharpGLTF strict validation runs
against the exact captured package bytes. The CI contract gate also checks GLB
and separate glTF through the pinned Khronos validator and requires zero errors
and zero warnings on Linux and Windows.
