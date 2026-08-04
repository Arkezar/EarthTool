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

COLLADA files do not contain the new EarthTool metadata graph. They cannot serve
as edit baselines for `msh import edit`. Export the original MSH again with the
current release before continuing an asset-editing workflow.

## CLI migration

Replace the former implicit conversion commands with an explicit intent:

| Previous workflow | Current workflow |
|---|---|
| Convert MSH for editing | `EarthTool.CLI msh export model.msh` |
| Export separate package | `EarthTool.CLI msh export model.msh --format Gltf` |
| Import an edited EarthTool baseline | `EarthTool.CLI msh import edit model.glb --expected-lineage <UUID> --expected-document <UUID>` |
| Author MSH from metadata-free input | `EarthTool.CLI msh import new model.glb` |

Export defaults to GLB and accepts static assets plus all 15 recognized dynamic
effect types. Export and new-model import accept deterministic, case-insensitive
input patterns. Edit import accepts exactly one concrete package because it binds one
expected asset lineage and document identity. Use `--tex-root` repeatedly to
supply ordered absolute TEX preview roots and `--msh-root` repeatedly for
`ScalableObject` resource previews. Use `--plan` for a versioned typed import
plan and `--report` for a transactional versioned machine report.

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

Register the supported services explicitly:

```csharp
services
  .AddMshServices()
  .AddGltfServices();
```

SharpGLTF types are not public EarthTool contracts. Callers exchange EarthTool
assets, options, plans, reports, operation results, streams, and paths with
`GltfInterchange`.

## Workflow migration

1. Preserve the original MSH asset as the serialized authority.
2. Export a fresh GLB or separate glTF baseline with the current EarthTool.
3. Edit the native glTF scene in Blender 4.5 LTS or a later supported release.
4. Import with `msh import edit` and the expected identities from the baseline.
5. Use `msh import new` only for intentionally metadata-free models.
6. Treat metadata conflicts as explicit decisions; do not strip metadata to
   force an edit import through the new-model path.

## Attachment helper name migration

Attachment artist-object names are a case-sensitive contract. Existing glTF
files using the earlier `ET_Attachment_...`, `ET_Cannon_..._Attachment_...`, or
light names ending in `_Attachment_...` must be renamed before name-driven
new-model import. EarthTool deliberately does not treat the former names as
aliases. Export a fresh baseline for edit-import workflows whenever possible;
its metadata identities remain authoritative independently of display names.

The current identifiers are listed in the
[attachment identifier cheat sheet](mesh-artist-quickstart.md#attachment-identifier-cheat-sheet).
An `ET_Emitter_n` object is exported under, and must remain under, the source
object containing a render object carrying the matching `MarkerAttachmentN`
flag. Use a fresh export to obtain the correct relative transform and hierarchy.

See the [quick start](quickstart.md), [MSH API](api/msh.md), and
[glTF API](api/gltf.md) for the current contracts.
