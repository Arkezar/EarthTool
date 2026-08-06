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
| `EarthTool.GLTF` | GLB and separate glTF projection, metadata reconciliation, validation, plans, and reports |
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
metadata envelopes
retain MSH-only state only while native projection fingerprints prove that state
remains applicable. Native glTF owns only the deliberately documented artist
projection; runtime-only dynamic behavior remains metadata authority.

Mesh asset creation validates self-contained metadata when present and otherwise
attempts canonical authoring with a warning. Callers provide neither MSH identities
nor an import mode; both stream and file paths are bounded and transactional.

Release qualification applies the same GLB and separate-glTF export, strict
validation, unchanged-import, and canonical-baseline oracles to every accepted
static and dynamic corpus asset. A packaged CLI byte-parity oracle and the
single-invocation `Export All Meshes` batch prevent a static-only rollout from
passing release gates.

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

Public MSH and glTF APIs, metadata, import plans, reports, and command behavior
have approval or behavioral gates. Linux and Windows CI run the public MSH/glTF
contract tests and the CLI process acceptance tests. Generated packages are also
qualified through SharpGLTF strict validation and the pinned Khronos validator.
