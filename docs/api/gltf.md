# glTF API

`GltfInterchange` is EarthTool's sealed facade over `SharpGLTF.Core`. No
SharpGLTF type appears in the public API. Static source objects are emitted as
native nodes and meshes. Their child relationships remain native node
relationships, and each material partition becomes one triangle primitive.
EarthTool does not add an axis-conversion root.

`ExportGlbAsync` emits a standard glTF 2.0 +Y-up GLB with positions, normals,
texture coordinates, and unsigned-short indices. A partition that references
vertex index `65535` uses unsigned-int indices so the maximum unsigned-short
restart value is never emitted. `ExportGltfFileAsync` emits the same projection
as a JSON manifest plus a content-addressed `.bin` sidecar. It commits the
manifest last, so a committed package never references missing or partial
output. A failed operation may leave an unreferenced content-addressed sidecar.

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
