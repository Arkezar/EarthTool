# EarthTool Overview

EarthTool is a cross-platform suite for inspecting and modifying Earth 2150 game
data.

## Supported workflows

| Format | Capabilities |
|---|---|
| WD | Read, create, extract, add, remove, and inspect archives |
| TEX | Read textures and produce standard image previews |
| MSH | Read, validate, write, canonically author, and safely edit framed version-1 assets |
| GLB/glTF | Export static MSH and supported Group, sprite, ribbon, attached-particle, and procedural dynamic MSH; reconcile artist edits; author new static MSH models |
| PAR | Read, edit, and serialize parameter data |

The MSH API supports both static and dynamic mesh assets. Artist interchange
supports the complete static contract plus the available dynamic slices: ordered
`Group` hierarchies and deterministic `Explosion`, `Track`, `MappedExplosion`,
`FlatExplosion`, `Smoke`, `Laser`, `LaserWall`, `ElectricalCannon`, and
`Lightning` previews, attached-context `Shockwave`, `Line`, and `Keelwater`
previews, and the built-in `Sphere` preview. `ScalableObject` remains binary-only
until its dedicated slice lands.

## Blender workflow

EarthTool targets Blender 4.5 LTS and later supported releases through standard
glTF 2.0. Export defaults to one GLB. Separate glTF is available when sidecars
are preferable. Native hierarchy, geometry, animation, materials, attachment
artist objects, and punctual lights remain editable while versioned EarthTool
metadata retains applicable MSH-only serialized representations.

Use explicit commands for each intent:

```text
EarthTool.CLI msh export model.msh
EarthTool.CLI msh import edit model.glb --expected-lineage <UUID> --expected-document <UUID>
EarthTool.CLI msh import new model.glb
```

See the [quick start](quickstart.md), [MSH API](api/msh.md), and
[glTF API](api/gltf.md).

## Safety model

Operations use finite profiles, operation-scoped diagnostics, explicit
cancellation, and transactional destination writes. Structural hazards,
metadata conflicts, validation failures, cancellation, and I/O failures produce
no partial mesh asset and do not replace an existing destination.

## Breaking release

The glTF release removes the previous COLLADA project, commands, file type, and
mutable generic MSH conversion API. No aliases are provided. Existing users
should follow the [COLLADA to glTF migration guide](migration-collada-to-gltf.md).
