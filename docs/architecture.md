# EarthTool Architecture

EarthTool is organized around format modules and explicit public boundaries.

```text
EarthTool.CLI
  |-- EarthTool.WD
  |-- EarthTool.PAR
  |-- EarthTool.TEX
  `-- EarthTool.GLTF --> EarthTool.MSH
                         `-- EarthTool.Common
```

## Modules

| Module | Responsibility |
|---|---|
| `EarthTool.Common` | Shared operation results, diagnostics, file types, and format infrastructure |
| `EarthTool.MSH` | Immutable framed MSH assets, bounded binary operations, canonical authoring, and expert exact construction |
| `EarthTool.GLTF` | GLB and separate glTF projection, source-free canonical creation, validation, plans, and reports |
| `EarthTool.WD` | WD archive operations |
| `EarthTool.TEX` | TEX resource decoding and previews |
| `EarthTool.PAR` | PAR parameter operations |
| `EarthTool.CLI` | Transactional command workflows and stable exit statuses |

`EarthTool.GLTF` is the only artist-interchange adapter. SharpGLTF remains an
internal implementation dependency and does not appear in EarthTool's public
contracts.

## MSH boundary

The MSH domain separates a serialized representation from a semantic view and a
canonical authored representation. Reading retains accepted values and reports
compatibility anomalies without silently normalizing them. Structural hazards
fail closed without a partial mesh asset.

The public binary boundary is `IMshReader`, `IMshWriter`, and `IMshValidator`.
The asset hierarchy is immutable and closed. `StaticMeshBuilder` and
`DynamicMeshBuilder` author canonical assets; `MshExpert` is the explicit exact
serialized-construction boundary.

## Interchange boundary

`GltfInterchange` projects static mesh assets and supported dynamic `Group`,
sprite-effect, ribbon-effect, attached-particle, procedural, and resource-backed
`ScalableObject` assets to GLB or separate glTF. Referenced static MSH geometry is
borrowed preview data and never becomes dynamic asset authority. EarthTool
emits only `extras.earthtoolAuthoring` string envelopes with format
`earthtool.msh.authoring`, version 1. These envelopes contain typed authoring
values and are read only on strict case-sensitive canonical named node owners.

Every static or dynamic GLB and separate glTF creation path regenerates a new
canonical MSH from current native state, canonical names, typed authoring values,
and explicit creation options. Legacy metadata, embedded source assets,
serialized representations, interchange identities, fingerprints, guards,
conflict actions, and preservation data have no authority. TEX and MSH resource
bindings are explicit import-plan or creation options rather than metadata.
Both stream and file paths are bounded and transactional.

Release qualification applies the same GLB and separate-glTF export, strict
validation, canonical-creation, and explicit-binding oracles to every accepted
static and dynamic corpus asset. The single-invocation `Export All Meshes` batch
prevents a static-only rollout from passing release gates.

## CLI boundary

The supported MSH command topology is:

```text
earthtool msh export
earthtool msh import
```

Commands return `0` for success, `1` for content or operation failure, `2` for
usage failure, and `130` for cancellation. Batch expansion and destination
collision checks complete before writes, while independent per-file failures do
not suppress later inputs.

## Verification

Public MSH and glTF APIs, canonical authoring envelopes, import plans, reports,
and command behavior have approval or behavioral gates. Linux and Windows CI run
the public MSH/glTF contract tests and CLI process acceptance tests. Generated
packages are also qualified through SharpGLTF strict validation and the pinned
Khronos validator.
