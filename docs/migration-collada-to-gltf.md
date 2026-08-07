# Migrate From COLLADA To glTF

The glTF release is an intentional breaking change. It removes the COLLADA
assembly, standalone command, file type, tests, DI registration, and the mutable
generic MSH conversion model. There are no command aliases, compatibility
facades, or obsolete forwarding types.

## Last COLLADA release

`v0.4.4` is the last EarthTool release that can read or write COLLADA. Keep that
version only when maintaining an existing COLLADA-based pipeline:

- [EarthTool v0.4.4 release and binaries](https://github.com/Arkezar/EarthTool/releases/tag/v0.4.4)
- [EarthTool v0.4.4 source](https://github.com/Arkezar/EarthTool/tree/v0.4.4)
- [Current releases](https://github.com/Arkezar/EarthTool/releases)
- [glTF implementation specification](https://github.com/Arkezar/EarthTool/issues/133)

COLLADA files do not contain canonical EarthTool authoring envelopes. Export the
original MSH again with the current release when its supported typed authoring
values are needed. Import still creates a new canonical MSH rather than restoring
the original source representation.

## CLI migration

Replace the former conversion commands with the glTF asset-creation commands:

| Previous workflow | Current workflow |
|---|---|
| Convert MSH for editing | `EarthTool.CLI msh export model.msh` |
| Export separate package | `EarthTool.CLI msh export model.msh --format Gltf` |
| Import an edited EarthTool package | `EarthTool.CLI msh import model.glb` |
| Author MSH from metadata-free input | `EarthTool.CLI msh import model.glb` |

Export defaults to GLB and accepts static assets plus all 15 recognized dynamic
effect types. Export and import accept deterministic, case-insensitive input
patterns. Every import uses the same source-free canonical creation path. Use
`--tex-root` repeatedly to supply ordered absolute TEX preview roots and
`--msh-root` repeatedly for `ScalableObject` resource previews. These roots do
not become resource metadata. Use `--plan` for versioned typed TEX/MSH bindings
and other creation options, and `--report` for a transactional versioned machine
report.

The stable exit statuses are `0` for success, `1` for content or operation
failure, `2` for usage failure, and `130` for cancellation.

## API migration

Replace mutable format models and generic conversion readers or writers with the
new domain boundaries:

| Removed surface | Replacement |
|---|---|
| Mutable generic mesh root | Closed immutable `MeshAsset` hierarchy |
| Mutable static part collections | `StaticMeshAsset.StaticRenderObjectSequence` and source object tree |
| Generic mesh reader/writer registration | `IMshReader`, `IMshWriter`, `IMshValidator`, and `AddMshServices` |
| Coherent construction by property mutation | `StaticMeshBuilder` or `DynamicMeshBuilder` |
| Exact low-level property mutation | `MshExpert` with a complete serialized representation |
| COLLADA reader/writer | Sealed `GltfInterchange` facade |
| Global or thrown conversion warnings | Operation results with stable diagnostics |
| `GltfNewModelImportOptions.HelperBindings`, `GltfNewModelHelperBinding`, and `GltfNewModelHelperKind` | Canonical `ET_...` authoring identifiers on helper nodes and matching punctual-light definitions |
| `GltfNewModelImportOptions.AnimationClasses`, `GltfNewModelAnimationClass`, and `GltfAnimationHandle` | `EarthTool A` through `EarthTool D` canonical authoring identifiers on animation clips |

Register the supported services explicitly:

```csharp
services
  .AddMshServices()
  .AddGltfServices();
```

SharpGLTF types are not public EarthTool contracts. Callers exchange EarthTool
assets, options, plans, reports, operation results, streams, and paths with
`GltfInterchange`.

Import-plan protocol version 3 accepts only TEX and MSH resource bindings,
footprint, horizontal extents, non-marker object roles and barrel angle, and
static-light-only values. It has no edit mode, source bindings, or conflict
actions. Version 1 and 2 plans must be regenerated. Version 3 rejects removed
edit, source binding, `helperBindings`, `animationClasses`, and marker-role
inputs; unsupported protocol versions report `ETG3001`.

## Workflow migration

1. Back up the original MSH and archive outside the conversion workflow.
2. Export a fresh GLB or separate glTF projection with the current EarthTool.
3. Review `ETG1030` warnings for source-only representations that canonical
   creation will not retain.
4. Edit the native scene in Blender 4.5 LTS or a later supported release.
5. Keep strict canonical owner names and any needed `earthtoolAuthoring` string
   envelopes; remove legacy `earthtool` properties if present.
6. Generate a new version-2 plan when TEX/MSH resource bindings or other typed
   options are required.
7. Import with `msh import` and review the canonical creation diagnostics.

## Attachment helper name migration

Attachment artist-object names are a case-sensitive contract. Existing glTF
files using the earlier `ET_Attachment_...`, `ET_Cannon_..._Attachment_...`, or
light names ending in `_Attachment_...` must be renamed before name-driven
canonical creation. EarthTool deliberately does not treat the former names as
aliases. Export a fresh projection whenever possible so supported typed values
use current canonical named owners.

The current identifiers are listed in the
[attachment identifier cheat sheet](mesh-artist-quickstart.md#attachment-identifier-cheat-sheet).
An `ET_Emitter_n` object belongs to its nearest source-object ancestor across
transform-only groups. Reparenting it transfers the matching `MarkerAttachmentN`
role to the new canonical owner. Use a fresh export to obtain the intended
relative transform and hierarchy.

See the [quick start](quickstart.md), [MSH API](api/msh.md), and
[glTF API](api/gltf.md) for the current contracts.
