# Mesh Artist Quick Start And Cheat Sheet

This guide is for artists modifying or creating Earth 2150 meshes in Blender
4.5 LTS or later. EarthTool uses glTF 2.0 as an artist interchange format. Every
GLB and separate glTF import creates a new canonical MSH; it never restores the
exported source MSH.

Dynamic MSH files are effect previews with effect-specific rules. See the
[dynamic effect-preview contract](api/gltf.md#dynamic-effect-preview-contract).

## Choose The Correct Workflow

| Goal | Workflow | Important rule |
|---|---|---|
| Modify a static MSH projection | `msh export`, edit, then `msh import` | The result is canonical regeneration, not a byte-preserving edit |
| Add a static source object | Add a mesh-bearing node named `ET_Static_{n}` | Names are strict, case-sensitive, positive, and unique |
| Create a standalone static MSH | Export glTF from Blender, then `msh import` | Use canonical owner/helper names and explicit resource bindings |
| Create or modify a dynamic MSH | Start from an EarthTool dynamic export | Keep `ET_Dynamic_{n}_{Effect}` names and supply TEX/MSH bindings explicitly |

Back up the original WD archive and MSH before starting.

## Export An MSH

```bash
EarthTool.CLI wd extract Data01.wd --filter "model.msh" -o ./work

EarthTool.CLI msh export \
  --tex-root "$(pwd)/work" \
  --msh-root "$(pwd)/work" \
  --output ./work/export \
  --report ./work/export-report.json \
  ./work/model.msh
```

`--tex-root` resolves decoded TEX previews. `--msh-root` resolves static MSH
geometry used by dynamic `ScalableObject` previews. These are export-only preview
roots; they do not embed game resource keys in glTF metadata.

Review export diagnostics before editing. `ETG1030` warns that a source-only MSH
representation cannot be carried by glTF and will not survive later canonical
creation.

## Import Into Blender

Set the scene to **24 FPS** with an FPS base of `1.0` before importing. Use
**File > Import > glTF 2.0** and select the GLB or separate glTF manifest.

Recommended settings:

| Setting | Value |
|---|---|
| Pack Images | On |
| Merge Vertices | Off |
| Import Scene Extras | On |
| Shading | Normals |

EarthTool exports typed values in the `earthtoolAuthoring` custom property on
strict canonical named nodes. Keep that property when its MSH-only values are
needed. It contains no source MSH, interchange identity, or resource binding.
Legacy `earthtool` properties are ignored and can be removed.

## Edit The Scene

- Keep final geometry triangular, finite, and supplied with normals. Textured
  primitives also require UVs.
- Keep vertex merging off while working from an export. Equal-position vertices
  can have different visible attributes.
- Preserve exact case in `ET_Static_{n}`, `ET_Dynamic_{n}_{Effect}`, helper, and
  light names. Ordinals are positive and have no leading zero.
- Mesh-bearing `ET_Static_{n}` nodes define the static source-object tree. Native
  parent/child transforms and primitive material assignment author the result.
- Transform-only groups collapse into descendant effective poses.
- Material images, names, and pixels are previews. They never select a TEX key.
- Borrowed `ScalableObject` geometry is a preview. It never selects an MSH key.
- Attachment empties support translation and horizontal heading. Unsupported
  pitch, roll, shear, or non-decomposable transforms fail canonical creation.
- Unknown scene objects remain scene-only or produce diagnostics. They do not
  acquire MSH meaning from display-name similarity.

There are no copied object, mesh, material, or scope identities to remove when
duplicating geometry. Assign every authored MSH owner a unique canonical name and
ensure the resulting hierarchy is unambiguous.

## Preview Animation Classes

EarthTool exports classes as `EarthTool A` through `EarthTool D`. Blender
normally activates the first imported animation and stores others as muted NLA
strips. Run this in Blender's **Scripting** workspace to enable the strips:

```python
import bpy

animation_names = {f"EarthTool {name}" for name in "ABCD"}

for obj in bpy.context.scene.objects:
    animation_data = obj.animation_data
    if animation_data is None:
        continue

    action_strips = []
    for track in animation_data.nla_tracks:
        if track.name not in animation_names:
            continue
        track.mute = False
        track.is_solo = False
        for strip in track.strips:
            strip.mute = False
            strip.influence = 1.0
            if strip.action is not None:
                action_strips.append(strip)

    if len(action_strips) == 1:
        strip = action_strips[0]
        animation_data.action = strip.action
        animation_data.action_slot = strip.action_slot
```

For authored animations, use only the four exact action names, integer frames
`0..254`, and finite supported TRS channels. One mesh object can participate in
at most one class.

## Export From Blender

Use **File > Export > glTF 2.0** and choose **glTF Binary (`.glb`)** or separate
glTF.

| Setting | Value | Why |
|---|---|---|
| Custom Properties / Extras | On | Carries `earthtoolAuthoring` typed values |
| Attributes | On | Exports supported mesh attributes |
| Lights | On | Exports canonical punctual-light artist objects |
| Animations | On | Exports `EarthTool A` through `EarthTool D` actions |
| Animation mode | Actions | Keeps independent class clips |
| Always Sample Animations | On | Produces dense supported TRS channels |
| Sampling rate | `1` | Samples every integer frame |
| Y Up | On | Keeps the glTF coordinate contract |
| Apply Modifiers | Off | Avoids unexpected topology changes |

## Create The Canonical MSH

Resource-free packages can be imported directly:

```bash
mkdir -p ./work/built
EarthTool.CLI msh import \
  ./work/model-edited.glb \
  --output ./work/built \
  --report ./work/import-report.json
```

Use a version-3 plan when the package needs values that glTF cannot safely
express:

```bash
EarthTool.CLI msh import \
  ./work/model-edited.glb \
  --plan ./work/import-plan.json \
  --output ./work/built \
  --report ./work/import-report.json
```

### Plan Template

Start from this template and edit the values that apply. There is one canonical
creation mode; old edit-mode plans and removed source bindings are rejected.

```json
{
  "format": "earthtool.msh.import-plan",
  "version": 3,
  "semanticOverrides": {
    "textureResourceBindings": [
      {
        "material": 1,
        "resourceKey": "Textures\\my\\hull.tex"
      }
    ],
    "meshResourceBindings": [],
    "footprint": null,
    "horizontalExtents": null,
    "objectRoles": [],
    "staticLightOptions": []
  }
}
```

| Field | Meaning |
|---|---|
| `textureResourceBindings` | One entry per textured material used by a mesh primitive; `material` is the one-based document-local material handle, `resourceKey` is the game TEX key |
| `meshResourceBindings` | One entry per dynamic `ScalableObject`; `node` is the owning node handle, `resourceKey` is the game MSH key. Omit when there are none |
| `footprint` | Replace `null` with `presenceMask`, `topElevations` (16 values), and `cornerPassageFlags` (16 values) to override the one-cell default |
| `horizontalExtents` | Replace `null` with `positiveY`, `negativeY`, `positiveX`, `negativeX` to override geometry bounds |
| `objectRoles` | Entries with `node`, `roles` (`"viewerFaced"`, `"barrel"`, `"rotor"`), and `barrelMaximumAngle` |
| `staticLightOptions` | Entries with `light`, `targetDistance`, and `terrainLightAmplitude` |

Every `textureResourceBindings`, `objectRoles`, and `staticLightOptions` array
must be present; leave them empty when unused. `meshResourceBindings` may be
omitted entirely when there are no dynamic `ScalableObject` references.
`footprint` and `horizontalExtents` use `null` when the default applies.

| Typed input | When it is needed |
|---|---|
| TEX resource binding | Every textured material used by a mesh primitive; map its one-based document-local material handle to the game TEX key |
| MSH resource binding | Every dynamic `ScalableObject`; map its owning node handle to the game MSH key |
| Footprint | To replace the one-cell maximum-height default |
| Horizontal extents | To replace effective geometry bounds |
| Object roles and barrel angle | For `ViewerFaced`, `Barrel`, or `Rotor` semantics not expressed by native glTF |
| Static-light values | For MSH-only target distance or terrain-light amplitude |

The CLI report records operation status, paths, diagnostics, asset kind, and the
new mesh creation GUID. It has no source-restoration, conflict-action, or
preservation section.

Install the new MSH into a copy of the archive:

```bash
EarthTool.CLI wd add Data01.wd ./work/built/model-edited.msh -o ModdedData01.wd
```

The archive entry name must match the resource name expected by the game.

## Static Authoring Defaults

| Semantic | Without a typed option |
|---|---|
| Source object tree and partitions | Derived from `ET_Static_{n}` mesh nodes, hierarchy, transforms, primitives, and material assignment |
| Attachments, cannons, emitters, and static lights | Derived from exact case-sensitive canonical identifiers |
| Animation classes | Derived from unique `EarthTool A` through `EarthTool D` clips |
| TEX or MSH key | No key is guessed; a required missing binding fails |
| Footprint | One occupied cell whose top is the maximum effective mesh height |
| Horizontal extents | Effective root-local geometry bounds |
| Spot target distance | Positive glTF range; otherwise a typed value is required |
| Terrain-light amplitude | `1.0`; photometric intensity is ignored |
| Non-marker object roles | No role is guessed from display names |

## Attachment Identifier Cheat Sheet

Attachment helpers are Blender Empty nodes unless the table says **Light**. Keep
them inside the single rooted source-object tree. Names are case-sensitive and
identify physical MSH targets.

| Physical records | Blender identifier(s) | Legacy ID | Purpose |
|---:|---|---|---|
| `1..4` | `ET_Turret_1` through `ET_Turret_4` | `BC1..4`, `SC1..4` | Weapon mount pose and yaw limit; translation authors the canonical position and heading authors direction |
| `5..8` | `ET_Emitter_1` through `ET_Emitter_4` | `MI1..4` | Marker attachment owned by the nearest source-object ancestor |
| `9..12` | `ET_TurretMuzzle_1` through `ET_TurretMuzzle_4` | `SS1..4` | Weapon muzzle positions |
| `13..16` | `ET_SpotLight_1` through `ET_SpotLight_4` | Spot lights `1..4` | **Spot Light** nodes with matching definition names |
| `17..20` | `ET_OmniLight_1` through `ET_OmniLight_4` | Omni lights `1..4` | **Point Light** nodes with matching definition names |
| `21..24` | `ET_UnloadPoint_1` through `ET_UnloadPoint_4` | `TR1..4` | Transport and placement positions |
| `25..28` | `ET_HitPoint_1` through `ET_HitPoint_4` | `HT1..4` | Optional hit positions |
| `29..32` | `ET_SmokePoint_1` through `ET_SmokePoint_4` | `SM1..4` | Optional effect or smoke positions |
| `33..36` | `ET_WT_1` through `ET_WT_4` | `WT1..4` | Reserved game slots |
| `37..38` | `ET_Chimney_1`, `ET_Chimney_2` | `CH1..2` | Paired emitter anchors |
| `39..40` | `ET_SmokeTrace_1`, `ET_SmokeTrace_2` | `ST1..2` | Paired state-specific emitter anchors |
| `41..42` | `ET_Exhaust_1`, `ET_Exhaust_2` | `SE1..2` | Paired effect-emitter anchors |
| `43..44` | `ET_KeelTrace_1`, `ET_KeelTrace_2` | `SK1..2` | Paired alternate-mode emitter anchors |
| `45` | `ET_InterfacePivot_1` | `IN0` or `IN1` | Child alignment pivot |
| `46` | `ET_CenterPivot_1` | `CE0` or `CE1` | General center pivot |
| `47` | `ET_ProductionSpotStart_1` | `PR1` | Production position and heading |
| `48` | `ET_ProductionSpotEnd_1` | `MV1` | Movement position and heading |
| `49` | `ET_LandingSpot_1` | `LN1` | Landing position and heading |

An `ET_Emitter_n` belongs to its nearest `ET_Static_{n}` ancestor, including
across transform-only groups. Canonical creation sets the matching
`MarkerAttachmentN` role on that owner's first material partition. One source
object can own several distinct emitter numbers.

### Directional Empty Presentation In Blender

glTF cannot prescribe Blender Empty display shapes. This optional script shows
directional helpers as arrows without adding glTF or MSH semantics:

```python
import bpy

directional_helpers = {f"ET_Turret_{number}" for number in range(1, 5)}
directional_helpers.update(
    f"ET_UnloadPoint_{number}" for number in range(1, 5)
)
directional_helpers.update({
    "ET_ProductionSpotStart_1",
    "ET_ProductionSpotEnd_1",
    "ET_LandingSpot_1",
})

for obj in bpy.context.scene.objects:
    if obj.type == "EMPTY" and obj.name in directional_helpers:
        obj.empty_display_type = "SINGLE_ARROW"
```

## Fast Checks Before Import

- The package is GLB or separate glTF 2.0 with one intended scene.
- Every authored owner and helper name matches the canonical spelling exactly.
- `earthtoolAuthoring` remains a string envelope with format
  `earthtool.msh.authoring`, version 1.
- Required TEX and MSH resource bindings are in a current source-bound plan.
- Geometry is triangular and finite; textured primitives have UVs.
- Animation names, frame limits, and 24 FPS sampling follow this guide.
- Blender export includes Extras, Attributes, Lights, and Animations as needed.
- Import diagnostics contain no unresolved required values or duplicate owners.
- The original MSH and WD archive remain backed up.

For binary details, see
[MSH format: Attachments and slots](MSH_FORMAT.md#attachments-and-slots). For the
complete canonical creation and dynamic preview contracts, see the
[glTF API](api/gltf.md).
