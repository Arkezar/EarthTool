# EarthTool Overview

EarthTool is a cross-platform suite for inspecting and modifying Earth 2150 game
data.

## Supported workflows

| Format | Capabilities |
|---|---|
| WD | Read, create, extract, add, remove, and inspect archives |
| TEX | Read textures and produce standard image previews |
| MSH | Read, validate, write, and canonically author framed version-1 assets |
| GLB/glTF | Export static and supported dynamic MSH assets; canonically regenerate static or dynamic MSH from GLB or separate glTF |
| PAR | Read, edit, and serialize parameter data |

The MSH API supports both static and dynamic mesh assets. Artist interchange
supports the complete static contract plus all 15 recognized dynamic effect
types: ordered
`Group` hierarchies and deterministic `Explosion`, `Track`, `MappedExplosion`,
`FlatExplosion`, `Smoke`, `Laser`, `LaserWall`, `ElectricalCannon`, and
`Lightning` previews, attached-context `Shockwave`, `Line`, and `Keelwater`
previews, the built-in `Sphere` preview, and resource-backed `ScalableObject`
previews with owned dynamic scale and binding edits.

## Blender workflow

EarthTool targets Blender 4.5 LTS and later supported releases through standard
glTF 2.0. Export defaults to one GLB. Separate glTF is available when sidecars
are preferable. Native hierarchy, geometry, animation, materials, attachment
artist objects, and punctual lights are the editable projection. Strict canonical
node owners can carry version-1 `earthtool.msh.authoring` envelopes containing
typed MSH-only authoring values.

Use one import command for all source-free canonical creation:

```text
EarthTool.CLI msh export model.msh
EarthTool.CLI msh import model.glb
```

Import never restores an embedded source MSH and has no edit mode. TEX and MSH
resource keys must be supplied through explicit creation options or a version-2
import plan; they are not inferred from glTF metadata or preview content.

See the [quick start](quickstart.md), [MSH API](api/msh.md), and
[glTF API](api/gltf.md).

## Safety model

Operations use finite profiles, operation-scoped diagnostics, explicit
cancellation, and transactional destination writes. Structural hazards,
invalid canonical authoring values, validation failures, cancellation, and I/O
failures produce no partial mesh asset and do not replace an existing
destination. Metadata budgets bound envelope count, per-envelope and aggregate
bytes, JSON depth and elements, unknown members, and warning diagnostics.

## Breaking release

The glTF release removes the previous COLLADA project, commands, file type, and
mutable generic MSH conversion API. No aliases are provided. Existing users
should follow the [COLLADA to glTF migration guide](migration-collada-to-gltf.md).
