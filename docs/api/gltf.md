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

Both exports assign asset-lineage and document identities, write EarthTool
metadata as string-valued `extras["earthtool"]`, and compute named version-1
geometry fingerprints. Partition fingerprints ignore index width, vertex
numbering, cyclic triangle-index rotation, and triangle order. They retain
winding and triangle multiplicity.

`ImportEditGlbAsync` and `ImportEditGltfFileAsync` require the expected lineage
and document identities. They validate metadata carriers, object and mesh
ownership, native hierarchy, projection name and version, and partition
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

`ValidateGlbAsync` and `ValidateGltfFileAsync` validate packages without
materializing MSH output. All import and validation operations enforce finite
positions, normals, and texture coordinates; triangle or non-indexed triangle
topology; valid unsigned-byte, unsigned-short, or unsigned-int indices; required
texture coordinates; and `GltfOperationProfile.MaxActiveRenderVertices`.

Path-based exports stage sibling temporary files and replace the destination
only after conversion and validation succeed. SharpGLTF strict validation runs
against the exact captured package bytes. The CI contract gate also checks GLB
and separate glTF through the pinned Khronos validator and requires zero errors
and zero warnings on Linux and Windows.
