# glTF API

`GltfInterchange` is EarthTool's sealed facade over `SharpGLTF.Core`. No
SharpGLTF type appears in the public API. The initial walking-skeleton slice
supports one immutable static MSH render object containing three vertices and
one triangle.

`ExportGlbAsync` emits a standard glTF 2.0 +Y-up GLB with positions, normals,
texture coordinates, and unsigned-short indices. It assigns an asset-lineage
identity and document identity, writes EarthTool metadata as string-valued
`extras["earthtool"]`, computes the named version-1 `static-geometry`
fingerprint, and performs SharpGLTF strict validation before returning.

`ImportEditGlbAsync` requires the expected lineage and document identities. It
validates the metadata carriers, projection name and version, scene membership,
and the native geometry fingerprint before restoring serialized MSH state. A
successful import retains the asset lineage and rotates the document identity.
Edited or unsupported content fails closed and returns no partial asset.

Path-based GLB export stages a sibling temporary file and replaces the
destination only after conversion and validation succeed. The CI contract gate
also checks generated GLB output with the pinned Khronos validator and requires
zero errors and zero warnings on Linux and Windows.
