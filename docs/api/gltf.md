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
values through `GltfMeshResourceLimits`.

Public export methods return `OperationResult`: status and diagnostics only.
Their generated packages use canonical `ET_Static_{n}` or
`ET_Dynamic_{n}_{effect}` owner names and canonical authoring envelopes for
typed values. Embedded legacy EarthTool metadata supports the exported artist
projection, but its lineage, document, scope, fingerprint, and source-MSH fields
are not public contracts and never authorize public creation to restore source
bytes.

## Dynamic effect-preview contract

Dynamic exports use overloads that accept `DynamicMeshAsset`; static overloads
remain strongly typed. Canonical owner names identify the dynamic object tree.
Every object node projects its effect declaration, ordered children, resource
bindings, and authoring values needed by source-free regeneration. `Shockwave`,
`Line`, `Sphere`, and
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
The generated action is an artist preview. Public creation regenerates a new
canonical MSH from the current glTF hierarchy, owner names, authoring values,
and supported native edits; it does not restore an embedded source asset.

Core glTF material factors are limited to zero through one. Finite MSH colors,
alpha, or gains that evaluate outside that range are clamped only in the preview
and made read-only for that native material. Supported active authoring values
remain in the canonical authoring envelope. Non-finite active values are rejected
instead of being normalized.

The native preview behavior is effect-specific:

| Effect | Shape and TEX region | Color, alpha, light, and hierarchy |
| --- | --- | --- |
| `Track` | Horizontal XZ quad approximating the runtime terrain-cell decal bounds; full decoded TEX image because atlas declarations are inactive | Neutral white deterministic terrain color; semantic alpha; no terrain-light preview; ordered children and child translation remain native |
| `MappedExplosion` | Horizontal XZ quad approximating the runtime terrain-cell decal bounds; full decoded TEX image | Serialized visible RGB and semantic alpha; evaluated terrain light remains metadata-only; ordered children participate normally |
| `FlatExplosion` | Horizontal XZ quad at the serialized local-plane depth; UVs use the selected source frame and exact reciprocal atlas values | Serialized visible RGB and semantic alpha; evaluated terrain light remains metadata-only; ordered children participate normally |
| `Smoke` | Vertical XY billboard snapshot at the camera-depth offset; UVs use the selected source frame and exact reciprocal atlas values | Visible RGB is modulated by the explicit white terrain sample and serialized gain; semantic alpha; no terrain-light producer; ordered children participate normally |
| `Shockwave` | Vertical XY billboard snapshot at the camera-depth offset; UVs use the attached-particle selected source frame and exact reciprocal atlas values | Explicitly uses attached-particle RGB modulation and frame-phase alpha; primary dispatch has no own geometry or terrain light; ordered children still use normal hierarchy dispatch |
| `Line` | Same attached-particle billboard contract as `Shockwave` | Explicitly uses attached-particle RGB modulation and frame-phase alpha; primary dispatch has no own geometry or terrain light; ordered children still use normal hierarchy dispatch |
| `Keelwater` | Vertical XY billboard snapshot at the camera-depth offset; UVs use its dedicated attached-particle frame selection | Uses fixed preview water RGB and frame-phase alpha; primary dispatch has no own geometry or terrain light |
| `Sphere` | Generated unit sphere with full decoded TEX preview; metadata identifies the selected `builtIn16` lifetime frame | Visible RGB and additive selection are represented; ordered children participate normally |
| `ScalableObject` | Borrowed referenced static MSH geometry with the selected atlas texture and uniform interpolated model scale | Its typed mesh binding remains authoring authority; referenced geometry is preview-only |

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
preview. They are deterministic preview inputs, never claims about the serialized
representation.

| Effect | Ribbon, texture, material, and light preview | Hierarchy and authority |
| --- | --- | --- |
| `Laser` | One segment between the deterministic endpoints; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND` | Ordered children, child translation, and supported authoring values regenerate canonically |
| `LaserWall` | The same one-segment geometry; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND` | Ordered children and supported authoring values regenerate canonically |
| `ElectricalCannon` | Twenty deterministic segments communicate the adaptive jagged path without pretending to reproduce runtime randomness; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND` | Runtime subdivision and random inputs are preview-only |
| `Lightning` | Thirty deterministic segments communicate the fixed vertical bolt; selected atlas frame, semantic alpha, serialized RGB, ribbon half-width, and `BLEND` | Runtime endpoint, global-angle, and random inputs are preview-only |

Each ribbon pair stores logical left then logical right, with atlas U assigned by
that logical side and V advancing along the path. Indices retain
`(L[i],L[i+1],R[i])`, `(R[i],L[i+1],R[i+1])`. The serialized
ribbon half-width is used with its sign: negating it exchanges physical sides,
mirrors texture orientation, and reverses winding. Zero or non-finite serialized
ribbon half-width is preserved by the MSH binary API but cannot form a preview
and fails export with scoped `ETG1006`.

TEX lookup uses the same bounded root order and diagnostic/default fallback as
static materials. Missing or unsafe resources retain the typed game key and
receive deterministic diagnostics and, where available, the diagnostic preview.
Core glTF `BLEND` is only an artist preview: it cannot express the game's
additive `ONE,ONE` behavior. Core glTF also cannot express runtime billboard
orientation, terrain tessellation, frame advance, terrain lighting, or random
light behavior. Canonical authoring envelopes carry supported active values;
resource keys that glTF cannot evidence must come from typed options or plans.

Native hierarchy, child start translation, visible geometry, and supported
material values provide current glTF evidence. Canonical authoring envelopes
provide non-visible active values such as frame domains, end values, light
settings, and additive selection. Creation emits a new canonical representation
with a new creation GUID. Invalid, contradictory, ambiguous, or unsupported
values fail without partial output rather than falling back to embedded source
bytes. Runtime-only preview inputs and inactive serialized representations are
not restoration authority.

Use `CreateMeshAsync` for GLB streams and `CreateMeshFileAsync` for separate glTF
files. Both return `OperationResult<MeshAsset>` and route static and dynamic
packages exclusively through source-free canonical regeneration. Typed import plans use
`CreateMeshWithPlanAsync` and `CreateMeshFileWithPlanAsync`; these methods validate
the mode, package kind, profile limits, and exact captured source before creation.
No public creation overload accepts a source MSH, expected baseline, lineage,
document identity, scope identity, or preservation policy.

Import plans and machine reports are separate protocols from embedded metadata.
Version 2 plans use format `earthtool.msh.import-plan`; version 2 reports use
format `earthtool.msh.cli-report`. `GltfImportPlanFormat` and
`GltfCliReportFormat` expose their supported versions independently, so changing
one protocol does not reinterpret either of the others.

`GltfImportPlanSerializer` accepts only the closed typed values represented by
`GltfNewModelImportOptions`. New-model plans may carry
TEX bindings, footprint and extent values, non-marker object roles, barrel angle,
and MSH-only static-light values. Canonical `ET_...` authoring identifiers on
nodes and punctual-light definitions author helpers, and `EarthTool A` through
`EarthTool D` authoring identifiers on clips author animation classes.
Unknown members and raw metadata, MSH bytes, adapter objects, guards, and expert
state are rejected.
Version 1 plans must be regenerated; version 2 rejects removed `helperBindings`,
`animationClasses`, and marker-role inputs with `ETG3005` migration diagnostics.
A plan also binds its
intent, package kind, and a lowercase SHA-256 source digest. GLB uses the exact
file bytes. Separate glTF uses a domain-separated digest over the exact manifest,
buffer, and referenced images, with sidecars ordered by ordinal URI. Use
`ComputeGlbSourceSha256Async` or `ComputeGltfSourceSha256Async` to produce the
binding. The `WithPlanAsync` import methods capture and verify those same bytes
before opening an import transaction. Malformed, unsupported, excessive, stale,
mismatched, and removed-input plans report `ETG3000` through `ETG3005` without
returning a partial asset.

`GltfCliReport` collects export, canonical import, and validation outcomes.
`GltfCliReportSerializer` writes fixed-order UTF-8 JSON with invocation status
and each operation's input, destination, package kind, asset kind, status,
diagnostics, and generated mesh creation GUID. Version 2 deliberately removes
lineage, document, scope, fingerprint, conflict-action, restoration-path,
preservation, and export-receipt fields. Operation and diagnostic order are
retained; diagnostic data keys are sorted ordinally. Reports contain no
timestamps or host-generated paths, and repeated serialization of the same
outcomes is byte-identical.

## Internal legacy reconciliation reference

The following matrix documents the baseline-backed edit machinery retained
temporarily for internal tests and deletion work. It is not a public creation
contract. Public `CreateMesh*` methods do not accept an edit baseline, execute
these preservation rules, or return the reports described by the matrix.

### Static authoring authority and inference matrix

This internal path applied the same hierarchy-first reconciliation to GLB and
separate glTF packages. It is not called by the CLI or public creation APIs. Its
four authority categories are:

- A **reconstructable authoring semantic** is safely recoverable from current
  artist-visible glTF state. It overrides stale metadata.
- A **projection-bound authoring semantic** is trusted only when the package is a
  matching EarthTool edit projection. Punctual-light intensity is the current
  static example.
- A **typed authoring input** supplies a value that glTF cannot evidence safely or
  deliberately replaces a documented fallback. Version 2 import plans contain
  only these values.
- A **canonical authoring default** is deterministic output used only after no
  applicable evidence or typed value exists. It is not inferred source intent.

Applicable edit metadata remains authoritative for **interchange scope identity**,
independent non-reconstructable values, and exact unchanged serialized
representations. It never overrides a reconstructable artist edit. The complete
static matrix is:

| Semantic | Artist-visible evidence | Edit-import authority | New-model-import authority | Metadata authority | Typed input | Canonical authoring default | Rename or reparent | Deletion | Ambiguity or contradiction | Diagnostics and preservation report |
|---|---|---|---|---|---|---|---|---|---|---|
| Hierarchy and source objects | Mesh-bearing nodes, native parent/child relationships, effective transforms, and mesh primitives. Ordinary source-object names are presentation-only. | The current **source object tree** is a reconstructable authoring semantic after applicability checks. Reparenting and position-affecting transforms author the effective tree and pose. | Each mesh-bearing node becomes a source object; each primitive becomes an ordered **static render object** material partition. Transform-only groups collapse, and the result must be one source object tree. | Retains **interchange scope identity**, identity high-water marks, and exact unaffected records after lineage, document, carrier, guard, and scope validation. | `ViewerFaced`, `Barrel`, `Rotor`, and `BarrelMaximumAngle` remain typed because ordinary names do not evidence game roles. | None for hierarchy or partitions; artist evidence defines them. Unassigned source objects use animation class A, and no role is guessed from a display name. | Reparenting authors hierarchy. Presentation renames retain interchange scope identity. | Removes the source-object subtree while retaining identity high-water marks. | Multiple effective roots, ambiguous copied identities, invalid transforms, and non-unique partition correspondence fail transactionally. Unsupported metadata-free empty leaves warn and remain scene-only. | `ETG1006`, `ETG2001`, and `ETG2012` identify native nodes or scopes. Reports mark affected hierarchy, sequence, pose, and flags `regenerated` or `canonicalized`; unaffected records remain `retained`. |
| Attachment and cannon helpers | Case-sensitive `ET_...` **canonical authoring identifiers**, leaf-node pose, and helper family/number. | Identifier, **canonical helper family**, physical target, pose, and presence are reconstructable. | Canonical identifiers and pose create helper records. Unknown or malformed reserved-looking empty leaves warn and are ignored rather than corrected. | Retains interchange scope identity and compatible object-owned values, including cannon yaw range, across a same-family retarget. | No helper binding exists. MSH-only non-marker object-role and barrel-angle values remain typed at their source object. | Absence of a canonical identifier authors no helper. | Same-family renumbering to a free slot retargets and clears the old slot. Cross-family rename requires delete-and-create. Non-emitter helper ancestry affects only effective transform. | An attachment deletion clears its record. Cannon deletion clears both its attachment and full-precision position. | Children, duplicate or occupied targets, cross-family edit renames, unsupported pose, and a noncanonical rename of an expected edit helper fail with all relevant paths. | Scoped `ETG1006` or edit conflict diagnostics identify failures. Reports identify cleared, retained, regenerated, and canonicalized attachment/cannon paths; scene-only nodes warn. |
| Emitter marker ownership | Each `ET_Emitter_n` and its nearest source-object ancestor across transform-only groups. | Changed hierarchy is authoritative for **emitter marker ownership** and transfers the marker bit to the visible owner. | The nearest source-object ancestor is the sole owner. EarthTool clears the matching marker bit globally, then sets `MarkerAttachmentN` on the owner's first canonical material partition. | Unchanged metadata-backed zero- or multi-marker compatibility anomalies retain their exact representation rather than being normalized. | None. Marker object roles are removed plan members rejected with `ETG3005`. | No emitter means no marker role. | Reparenting transfers ownership. One source object may own several distinct emitter numbers. | Clears both the emitter attachment and its marker ownership. | Duplicate helpers for one number, no source-object ancestor, or ambiguous changed ownership fail transactionally with every conflicting native path. | Preserved anomalies warn with `ETG1027` and stay `retained`. Attachment addition/deletion is `canonicalized` with its attachment reason; `ObjectFlags` is `regenerated` with reason `EmitterMarkerOwnership`. Ambiguity uses `ETG2012`. |
| Static lights | Matching case-sensitive node and punctual-light definition identifiers, point/spot type, leaf pose, linear RGB, spot cones, positive range, and edit-projected intensity. | Pose, color, cones, and positive range are reconstructable. Intensity is a **projection-bound authoring semantic** for terrain-light amplitude only in a matching edit projection. Missing edit range preserves loaded target distance. | Matching declarations author pose, color, cones, and type. Positive spot range authors target distance. Photometric intensity is not amplitude evidence. | Retains interchange scope identity, missing-range target distance, and compatible owned state across same-family retargets. | `TargetDistance` supplies the value only when range is absent. `TerrainLightAmplitude` replaces its default. | Terrain-light amplitude is `1.0`. A spot light still requires positive range or typed target distance. | Same spot-to-spot or omni-to-omni renumbering retargets to a free slot. Spot/omni conversion requires delete-and-create. | Clears the activity attachment and complete static-light record. | Shared definitions, mismatched names or types, duplicate or occupied targets, children, and any simultaneous range/typed-distance authority fail transactionally. | Ignored non-default new-model intensity warns with `ETG1028`. Reports expose retained distance, regenerated light fields, complete deletion, and scoped conflict paths. |
| Animations | Unique case-sensitive `EarthTool A` through `EarthTool D` clips, participating source objects, and finite supported TRS channels sampled on integer 24 FPS frames `0..254`. | Canonical clip identity, class participation, and supported samples are reconstructable. | Canonical clips assign classes and generate dense canonical tracks. Noncanonical or malformed reserved-looking clips warn and are ignored. | Restores unchanged representation details and retains a loaded class assignment that had no baseline projected clip. | None. Animation-class binding plan members are rejected with `ETG3005`. | Source objects not assigned by a surviving clip use class A. | Canonical-to-canonical rename retargets to a free class. | Clears the class's tracks, declaration, and current frame. Former participants reset to A unless another surviving clip assigns them. | Duplicate classes or participation, occupied retargets, unsupported targets/samples, non-integer or excessive frames, and a noncanonical expected-clip rename fail transactionally. | Edit projection conflicts use scoped `ETG2016`; metadata-only affine compatibility warns with `ETG1014`. Reports distinguish restored exact tracks from regenerated or invalidated class paths. |
| Horizontal extents | Complete effective vertex positions in root-local space. UVs, normals, material assignment, and hierarchy order without effective position changes are not evidence. | Effective vertex-position or position-affecting transform edits recompute all four extents over the complete source object tree. | Effective positions author geometry-derived extents when no typed replacement exists. | Loaded extents remain exact while effective positions are unchanged, even when they differ from geometry bounds. | `GltfNewModelHorizontalExtents` deliberately replaces geometry-derived values. | Root-local minimum and maximum horizontal coordinates supply all four extents. | Position-affecting reparenting regenerates extents; presentation renames and order-only changes retain them. | Source deletion regenerates extents only when it changes effective positions; it does not imply footprint deletion. | Non-finite geometry and values outside the serialized range fail with no partial asset. | `CommonBaseHeader.HorizontalExtents` is `retained` or `regenerated`; invalid derivation uses actionable `ETG1006` geometry paths. |
| Footprint | glTF has no reliable evidence for game occupancy, corner passage, rotated representation, or per-cell heights. Mesh height is used only by the fallback. | Geometry and hierarchy edits do not author footprint state. | Uses typed input or the canonical fallback; native geometry never infers game occupancy. | Loaded footprint presence, elevations, corner flags, and rotated representations remain exact through geometry edits. | `GltfNewModelFootprint` supplies the complete representation. | One occupied `0x8000` cell whose top is the maximum effective root-local mesh height. | Rename and reparent do not change a loaded footprint. | Geometry/source deletion does not delete a loaded footprint. A new-model plan replaces the whole fallback rather than partially merging it. | Invalid dimensions, cell values, or out-of-range derived top elevation fail transactionally. | Edit footprint paths stay `retained`; typed or fallback new-model paths are `canonicalized`. Invalid values use scoped geometry/plan diagnostics. |
| Materials and partitions | Primitive-to-material assignment and sharing. Material display names are presentation-only. | Native assignment is reconstructable for partition ownership. | Primitive assignment and sharing deterministically create ordered material partitions. | Retains material interchange scope identity, exact unaffected partition representation, and TEX binding through display rename and valid reassignment. | None for partition ownership; TEX keys are separate typed inputs. | None; primitive assignment is artist evidence, and no game identity is guessed from a material name. | Display rename preserves identity. Reassignment moves visible primitive ownership. | Removes only uniquely corresponding primitive/material authored state. | Duplicate material scopes, ambiguous correspondence, and indistinguishable conflicting partition edits fail transactionally. | `ETG2012` reports ambiguous native partitions/scopes. Reports expose identity, material reaffirmation, regeneration, deletion, and retained unrelated bytes. |
| TEX resource bindings | Applicable edit metadata can identify a game TEX key. A base-color image proves only that a referenced material is textured; names, URIs, and bytes never evidence a key. | Existing binding follows material interchange scope identity through presentation rename and valid reassignment. Preview availability never replaces it. | A textured material referenced by a mesh primitive requires a typed canonical key. EarthTool never creates, converts, renames, or packages TEX from the preview. | The exact existing **TEX resource binding** remains authoritative for its applicable material scope. | `TextureResourceBindings` maps a referenced document-local material handle to an exact canonical game key. | Untextured referenced materials need no binding. There is deliberately no guessed default for a textured referenced material. | Material rename retains the key; reassignment carries the bound material scope. | Removes that material use without creating or deleting the external TEX resource. | Missing typed keys for textured referenced materials and bindings to unused, invalid, duplicate, or ambiguous handles fail without partial output. | Required binding failures use `ETG1029`. An unresolved explicit key remains a writable reference-only binding and reports `ETG1007`/`ETG1009`. Reports retain existing paths or canonicalize explicit new ones. |

### Reports and compatibility

Use `--report <path>` on CLI export and import commands when the result will be
reviewed or automated. `operations[n].diagnostics` contains stable codes,
severity, primary native path, sorted diagnostic data, and the full actionable
message, including secondary conflicting paths. Successful import operations
also report the new mesh creation GUID. Reports do not expose preservation or
source-restoration details. GLB and separate glTF packages, and CLI and library
entry points, share the same canonical creation and rejection rules.

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
only when range is absent. Supplying both authorities is rejected.
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
clears its generated tracks, class declaration, and current frame index. Former
participants reset to class A unless another surviving canonical clip assigns
them. Ambiguous class ownership, duplicate clips, targets that
do not match an editable metadata scope, and samples beyond a guarded class
declaration fail with `ETG2016`. A finite affine frame that cannot be decomposed
and recomposed within the binary32 tolerance remains metadata-only for that one
object/class and reports `ETG1014`; other objects and classes still export.

EarthTool's internal baseline-backed reconciliation path requires the expected
lineage and document identities. It validates metadata carriers, object and mesh
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
This baseline-backed reconciliation code is internal legacy machinery. Public
mesh creation does not call it and exposes none of its identities, fingerprints,
restoration paths, or preservation results.

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
use their assigned `ETG2000` through `ETG2020` conflicts. Internal CLI
reconciliation binds conflict actions to the complete conflict and expected
baseline; stale, duplicate, mismatched, disallowed, incomplete, or replayed
actions fail before MSH creation. Scope mapping requires an explicit native
carrier path. Forking drops identity-bound metadata and lets native-addition
reconciliation allocate fresh IDs. Branch acceptance retains lineage, while
adopt-as-new and discard-lineage remove metadata authority and canonically admit
native content under fresh identities.
Unknown additive version-1 members retain their exact raw JSON tokens internally
but do not gain identity, guard, reference, action, or trust semantics. Dictionary
keys combine scope identity with an escaped JSON Pointer to the additive member.
Unsupported versions remain opaque and are never partially salvaged. Missing
expected scopes, dangling references, unsupported or stale guards, and ambiguous
native correspondence are inventoried before MSH creation, so any conflict
returns no partial asset. Native projection normalization remains domain-specific;
no global floating-point epsilon is applied.

Path-based exports stage sibling temporary files and replace the destination
only after conversion and validation succeed. SharpGLTF strict validation runs
against the exact captured package bytes. The CI contract gate also checks GLB
and separate glTF through the pinned Khronos validator and requires zero errors
and zero warnings on Linux and Windows.
