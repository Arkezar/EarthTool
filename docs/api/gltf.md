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
an invocation status and every operation's input, destination, package kind,
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
Declared zero-length animation with present tracks projects effective frame
zero; longer serialized tails remain metadata and do not extend the clip.
Unrecognized serialized animation-class values remain exact and report
`ETG1015`, while their native projection uses the game's modulo-four class
selection.

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
