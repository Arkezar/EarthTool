# glTF API

`GltfInterchange` is EarthTool's sealed facade over `SharpGLTF.Core`. No
SharpGLTF type appears in the public API. It exports static and supported
dynamic MSH assets to glTF 2.0 and creates new canonical MSH assets from GLB or
separate glTF packages.

## Export packages

`ExportGlbAsync` emits a standard glTF 2.0 +Y-up GLB. `ExportGltfFileAsync`
emits the same projection as a JSON manifest plus content-addressed `.bin` and
decoded `.png` sidecars. The exporter validates referenced sidecars and commits
the manifest last. A failed operation can leave an unreferenced
content-addressed sidecar, but never a committed package that references a
missing or partial file.

Static source objects become native nodes and meshes. Child relationships remain
native node relationships, each material partition becomes one triangle
primitive, and the effective first-partition pivot becomes node translation.
EarthTool does not add an axis-conversion root. A partition that references
vertex index `65535` uses unsigned-int indices so the unsigned-short restart
value is never emitted.

## Canonical creation

Every public creation route performs source-free canonical regeneration:

- `CreateMeshAsync` creates from a GLB stream.
- `CreateMeshFileAsync` creates from a separate `.gltf` package.
- `CreateMeshWithPlanAsync` and `CreateMeshFileWithPlanAsync` add explicit typed
  creation options.

The same rule applies to static and dynamic assets. Creation reads the current
native hierarchy, geometry, materials, animations, canonical owner names, and
typed authoring values, then builds a new canonical MSH with a new creation GUID.
There is no edit mode, baseline comparison, source MSH fallback, or byte-exact
round trip.

Legacy `extras.earthtool` data has no compatibility meaning. Source MSH payloads,
serialized representations, lineage and local identities, projection
fingerprints, guards, inventories, conflict actions, and preservation data are
ignored. They cannot authorize or alter creation.

## Canonical authoring envelopes

The exporter emits local typed values only as a JSON string at
`extras.earthtoolAuthoring`:

```json
{
  "extras": {
    "earthtoolAuthoring": "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{}}"
  }
}
```

The string envelope has exactly these root members:

```json
{
  "format": "earthtool.msh.authoring",
  "version": 1,
  "values": {}
}
```

EarthTool reads an envelope only from its strict, case-sensitive canonical named
owner. Positive ordinals have no leading zero. A misspelled, case-changed, or
duplicated owner is not an alias.

| Owner | Typed values |
|---|---|
| `ET_Static_{n}` node | Object role and barrel angle; the root owner also carries footprint and horizontal extents |
| `ET_Turret_{n}` node | Cannon yaw half-range |
| `ET_SpotLight_{n}` or `ET_OmniLight_{n}` node | Terrain-light amplitude and, for spot lights, optional target distance |
| `ET_Dynamic_{n}_{Effect}` node | Effect-applicable frame, sprite-sheet, rectangle, terrain-light, gain, alpha, and additive values |

Unknown or inapplicable members are ignored with bounded warnings. Missing or
invalid optional values use canonical defaults. Missing required dynamic frame
or sprite-sheet values fail creation. Resource keys are never authoring-envelope
members.

## Static authoring contract

Canonical static creation combines native glTF state, local typed envelopes,
explicit creation options, and documented defaults. It does not compare the
package with an earlier export.

| Semantic | Canonical creation authority |
|---|---|
| Source object tree | Mesh-bearing `ET_Static_{n}` nodes and native parent/child transforms; transform-only groups collapse into descendant poses |
| Material partitions | Ordered mesh primitives and native material assignment |
| Object roles | The `ET_Static_{n}` envelope or `GltfNewModelImportOptions.ObjectRoles`; display names do not imply roles |
| Attachments and cannons | Exact case-sensitive `ET_...` helper names and native pose; turret yaw comes from its envelope or the canonical default |
| Emitter ownership | The nearest source-object ancestor of `ET_Emitter_1` through `ET_Emitter_4` |
| Static lights | Matching canonical node and punctual-light definition names, type, pose, color, cone, and range; MSH-only values come from the envelope or typed options |
| Animations | Unique `EarthTool A` through `EarthTool D` clips with supported finite TRS channels on integer 24 FPS frames `0..254` |
| Horizontal extents | Root envelope, explicit option, or effective root-local geometry bounds |
| Footprint | Root envelope, explicit option, or one occupied cell whose top is the maximum effective mesh height |
| TEX resource binding | Explicit `TextureResourceBindings` option or import-plan entry only |

Canonical helper identifiers are case-sensitive. Unknown or malformed
reserved-looking helpers are ignored with diagnostics; duplicate or contradictory
canonical declarations fail without a partial asset.

Static lights must be leaf nodes. `ET_SpotLight_1` through `ET_SpotLight_4` own
unshared spot definitions, and `ET_OmniLight_1` through `ET_OmniLight_4` own
unshared point definitions. Node and definition names must agree. A positive spot
`range` supplies target distance when no typed value is supplied. Terrain-light
amplitude defaults to `1.0`; glTF photometric intensity is not amplitude evidence
and a non-default ignored value reports `ETG1028`.

Each animation class exports as a dense `LINEAR` action named `EarthTool A`
through `EarthTool D`. Translation, canonical-quaternion rotation, and scale are
sampled at `frame / 24`. Blender normally activates only the first imported
action. [F3D](https://f3d.app/) can preview all actions together:

```bash
f3d --animation-indices=-1 --animation-autoplay=true model.glb
```

## Explicit TEX and MSH bindings

Game resource identities are not inferred from glTF names, URIs, image bytes,
preview geometry, or authoring envelopes.

- `GltfNewModelImportOptions.TextureResourceBindings` maps a one-based
  document-local material handle to a canonical TEX key. Every textured material
  used by a mesh primitive requires one; otherwise creation fails with `ETG1029`.
- `GltfNewModelImportOptions.MeshResourceBindings` maps the owning dynamic node
  handle to the MSH key required by `ScalableObject`.

The version-3 import plan serializes these bindings with the other closed typed
creation options. Resource bindings are references only: EarthTool does not
create, rename, convert, or package the referenced TEX or MSH resource.

`GltfExportOptions.TextureSearchRoots` and `MeshResourceSearchRoots` are separate
export-preview settings. They locate decoded TEX previews and referenced static
MSH preview geometry; they do not embed game resource keys. Lookup is bounded,
component-wise, rejects unsafe paths and linked escapes, and reports missing,
ambiguous, shadowed, or unsupported resources.

## Dynamic effect-preview contract

Dynamic exports support all 15 recognized effect types. Canonical nodes use
`ET_Dynamic_{n}_{Effect}` names and retain ordered child relationships. Their
native geometry is an artist preview; canonical creation rebuilds effect state
from the current hierarchy, owner names, applicable typed envelope values, and
explicit resource bindings.

| Effect | Native preview | Canonical authoring notes |
|---|---|---|
| `Group` | Transform node with ordered children and no synthetic geometry | Hierarchy and child translation are native |
| `Explosion`, `Track`, `MappedExplosion`, `FlatExplosion`, `Smoke` | Unlit, double-sided preview quads | Applicable frame, rectangle, light, alpha, and additive values are typed in the owner envelope |
| `Laser`, `LaserWall` | One deterministic ribbon segment | Width, native hierarchy, and applicable typed values regenerate canonically |
| `ElectricalCannon`, `Lightning` | Deterministic jagged ribbon snapshots | Runtime endpoints and randomness are preview-only |
| `Shockwave`, `Line`, `Keelwater` | Attached-particle billboard snapshots | The billboard demonstrates the attached route; it does not infer a game attachment |
| `Sphere` | Deterministic generated unit sphere | Generated shape edits are preview-only; applicable typed values remain authoritative |
| `ScalableObject` | Flattened referenced static geometry with dynamic scale | Borrowed geometry is preview-only; the MSH key must be supplied explicitly at creation |

The deterministic preview uses a 100-tick, five-second interval at the normal
20-update scheduler rate. Lifetime-driven child translation and scalable-object
scale can export as `EarthTool Dynamic Preview`. Runtime billboard orientation,
terrain tessellation, random state, atlas timing, and additive `ONE,ONE` blending
have no portable core glTF equivalent and remain preview approximations.

Ribbon pairs store logical left then right. Indices use
`(L[i],L[i+1],R[i])` and `(R[i],L[i+1],R[i+1])`. A negative half-width swaps
physical sides, mirrors texture orientation, and reverses winding. Zero or
non-finite width cannot form a preview and fails export with `ETG1006`.

`ScalableObject` imports only flattened static preview geometry. Referenced
hierarchy, animation, attachments, lights, metadata, and resource authority do
not enter the dynamic asset. Missing, ambiguous, unsafe, malformed, dynamic, or
cyclic references use deterministic diagnostics and a placeholder where
possible.

## Import plans and reports

Import plans and machine reports are independent protocols:

- `earthtool.msh.import-plan`, version 3
- `earthtool.msh.cli-report`, version 2

A plan contains `GltfNewModelImportOptions`. Supported options are TEX and MSH
resource bindings, footprint, horizontal extents, non-marker object roles and
barrel angle, and static-light values. Version 1 and 2 plans must be
regenerated. Version 3 rejects removed edit mode, package kind, source bindings,
helper bindings, animation-class bindings, marker roles, raw metadata, and
expert state.

`GltfCliReport` records export, canonical import, and validation outcomes. Each
operation contains its input, destination, package kind, asset kind, status,
ordered diagnostics, and mesh creation GUID when available. Reports contain no
edit mode, source-restoration result, conflict actions, preservation changes, or
export receipts.

## Limits and diagnostics

`GltfOperationProfile` bounds input and output bytes, graph size and depth,
active vertices, preview resources, and canonical metadata. Metadata limits cover:

- envelope count;
- bytes per envelope and in aggregate;
- JSON depth and aggregate elements;
- unknown members; and
- emitted authoring warning diagnostics.

Metadata exhaustion reports `ETG2005`. Unknown or defaulted values use bounded
`ETG4000` warnings; duplicate owners use `ETG4001`, missing required values use
`ETG4002`, and warning truncation uses `ETG4003`.

Export reports `ETG1030` for accepted source-only MSH representations that the
GLB or separate glTF package cannot carry and a later canonical creation will not
retain. This warning does not block export and does not add a hidden source
payload.

`ValidateGlbAsync` and `ValidateGltfFileAsync` validate packages without
materializing MSH output. Import and validation enforce finite geometry,
supported triangle topology and index types, required texture coordinates, and
configured limits. Path-based exports stage sibling temporary files and replace
the destination only after conversion and validation succeed. Release gates run
SharpGLTF strict validation and the pinned Khronos validator for GLB and separate
glTF packages.
