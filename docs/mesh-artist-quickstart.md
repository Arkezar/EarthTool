# Mesh Artist Quick Start And Cheat Sheet

This guide is for graphics artists who want to modify an existing static Earth 2150 mesh, add geometry to an existing static mesh, or author a new static MSH in Blender. EarthTool uses glTF 2.0 as its artist interchange format and targets Blender 4.5 LTS or later. Dynamic MSH exports are effect previews with effect-specific edit rules; see the [dynamic effect-preview contract](api/gltf.md#dynamic-effect-preview-contract) instead of applying the static workflow below.

## Choose The Correct Workflow

| Goal | Command | Important rule |
|---|---|---|
| Modify an existing MSH | `msh export`, then `msh import edit` | Keep the EarthTool metadata and use the baseline IDs from the export report |
| Add a mesh object to an existing MSH | `msh export`, add an untagged Blender object, then `msh import edit` | Do not duplicate EarthTool object or mesh identities |
| Create a standalone MSH | Export metadata-free glTF from Blender, then `msh import new` | Do not use an EarthTool-exported GLB as new-model input |

Back up the original WD archive and MSH before starting.

The command examples use Bash line continuations. In PowerShell, put the same command and options on one line, replace `$(pwd)` with an absolute path, and use `New-Item -ItemType Directory -Force <path>` in place of `mkdir -p <path>`.

## Modify An Existing Mesh

### 1. Extract and export

```bash
EarthTool.CLI wd extract Data01.wd --filter "model.msh" -o ./work

EarthTool.CLI msh export \
  --tex-root "$(pwd)/work" \
  --msh-root "$(pwd)/work" \
  --output ./work/export \
  --report ./work/export-report.json \
  ./work/model.msh
```

`--tex-root` resolves game TEX previews. `--msh-root` resolves static MSH geometry used by dynamic `ScalableObject` previews.

Keep `export-report.json`. Its first operation contains the values required for edit import:

```text
operations[0].identities.baseline.assetLineageId
operations[0].identities.baseline.documentId
```

### 2. Import into Blender

Before importing, set the Blender scene to **24 FPS** with an FPS base of `1.0`. Blender converts glTF seconds to frame numbers during import, so changing FPS afterward can retime the clips.

Use **File > Import > glTF 2.0** and select `model.glb`.

Recommended import settings:

| Setting | Value |
|---|---|
| Pack Images | On |
| Merge Vertices | Off |
| Import Scene Extras | On |
| Shading | Normals |

Keep the generated `earthtool` custom properties. They identify the source objects, mesh data, materials, lights, and metadata needed for a safe edit.

### 3. Edit or add geometry

- Edit mesh geometry, UVs, normals, transforms, hierarchy, material assignment, animation, attachment empties, and supported lights within the constraints below.
- Keep triangles as the final topology. Apply triangulation before export if the result must be predictable.
- Keep vertex merging off. Equal-position vertices can carry different MSH meaning.
- Material images, colors, and names are previews; changing them does not select another game TEX resource. Existing texture bindings remain in EarthTool metadata.
- Do not rename `ET_...` attachment, cannon, or light helpers.
- Attachment empties support translation and heading/yaw. Pitch, roll, shear, or a non-decomposable transform is not a supported MSH attachment pose; finite scale is ignored.
- Do not delete custom properties from existing EarthTool objects.
- To add an object, duplicate or create it, make its mesh data single-user, and remove the `earthtool` custom property from only the new Object and its new Mesh datablock. EarthTool will allocate fresh identities during edit import.
- A duplicate that retains an existing `earthtool` identity is ambiguous and the import will fail instead of guessing.

### 4. Preview all animation classes

EarthTool exports the four MSH animation classes as `EarthTool A` through `EarthTool D`. Blender activates the first imported animation and normally stores the others as muted NLA strips. Run this in Blender's **Scripting** workspace to enable every EarthTool track and attach each object's Action slot so its keys appear in the Dope Sheet's Action Editor:

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

Each EarthTool mesh object belongs to at most one animation class, so the script attaches its single imported Action/slot. For a standalone new model, name animation Actions exactly `EarthTool A`, `EarthTool B`, `EarthTool C`, or `EarthTool D`. Use integer frames `0..254`; each mesh object may participate in only one class.

### 5. Export from Blender

Use **File > Export > glTF 2.0** and choose **glTF Binary (`.glb`)**.

Required or recommended export settings:

| Setting | Value | Why |
|---|---|---|
| Custom Properties / Extras | On | Preserves EarthTool metadata |
| Attributes | On | Preserves supported custom mesh attributes |
| Lights | On | Preserves editable attachment lights |
| Animations | On | Exports `EarthTool A..D` Actions |
| Animation mode | Actions | Keeps independent class clips |
| Always Sample Animations | On | Produces the supported dense TRS projection |
| Sampling rate | `1` | Samples every integer frame |
| Y Up | On | Keeps the glTF coordinate contract |
| Apply Modifiers | Off | Avoids changing identity-sensitive topology unexpectedly |

Export to a new file so the original EarthTool baseline remains available for comparison and recovery.

### 6. Import the edit and install it

Replace the two UUIDs with `assetLineageId` and `documentId` from the export report:

```bash
mkdir -p ./work/built
EarthTool.CLI msh import edit \
  ./work/model-edited.glb \
  --expected-lineage 00000000-0000-0000-0000-000000000000 \
  --expected-document 00000000-0000-0000-0000-000000000000 \
  --output ./work/built \
  --report ./work/import-report.json
```

Install the resulting MSH into a copy of the archive:

```bash
EarthTool.CLI wd add Data01.wd ./work/built/model-edited.msh -o ModdedData01.wd
```

The archive entry name must match the resource name expected by the game. Rename the built file before adding it when replacing an existing resource.

## Create A Standalone MSH

1. Start with a clean Blender file that contains no `earthtool` custom properties.
2. Build one rooted object tree with triangle meshes, finite positions and normals, UVs for textured materials, and optional helpers from the table below.
3. Export GLB using the settings above.
4. Import it with:

```bash
mkdir -p ./built
EarthTool.CLI msh import new model.glb --output ./built --report ./new-report.json
```

New-model import creates a static MSH. A material with a base-color image also needs a typed `--plan` that maps its document-local material handle to the game TEX resource key. The CLI does not guess a TEX resource from an image filename. A plan is also the explicit route for object roles, non-default light values, footprints, horizontal extents, helper bindings that do not use canonical names, and animation-class overrides. See the [glTF API](api/gltf.md) for the complete new-model contract.

## Attachment Identifier Cheat Sheet

Attachments are childless Blender Empty nodes unless the table says **Light**. Keep them inside the model's single rooted object tree, preferably as direct children of its root. Canonical names are case-sensitive and identify the physical MSH target. The legacy code is the name used by the original AOD converter and game research.

| Physical records | Blender identifier(s) | Legacy ID | Purpose |
|---:|---|---|---|
| `1..4` | `ET_Cannon_1_Attachment_1` through `ET_Cannon_4_Attachment_4` | `BC1..4`, `SC1..4` | Combined full-precision render position, quantized weapon mount, center heading, and yaw limit. One helper is emitted per active slot; translation edits both position records, heading rotation edits only the attachment direction, finite scale is ignored, and the yaw-limit byte remains preserved. `BC` and `SC` are aliases. |
| `5..8` | `ET_Attachment_05_Marker_1` through `ET_Attachment_08_Marker_4` | `MI1..4` | Attaches render objects by marker bit. `MI1` also anchors effects and projectiles. |
| `9..12` | `ET_Attachment_09_SS_1` through `ET_Attachment_12_SS_4` | `SS1..4` | `SS1` supplies a position relative to the owner. No normal consumer for `SS2..4` is confirmed. |
| `13..16` | `ET_SpotLight_1_Attachment_13` through `ET_SpotLight_4_Attachment_16` | Spot lights `1..4` | **Spot Light** objects. Position both drives the light and occupies the corresponding quantized attachment record. New-model lights need a positive custom distance/range or a plan-supplied target distance. |
| `17..20` | `ET_OmniLight_1_Attachment_17` through `ET_OmniLight_4_Attachment_20` | Omni lights `1..4` | **Point Light** objects. Position both drives the light and occupies the corresponding quantized attachment record. |
| `21..24` | `ET_Attachment_21_Transport_1` through `ET_Attachment_24_Transport_4` | `TR1..4` | Transport and placement matching; `TR1..3` are observed in game assets. |
| `25..28` | `ET_Attachment_25_HT_1` through `ET_Attachment_28_HT_4` | `HT1..4` | Four optional world positions; the game can select the nearest present slot. |
| `29..32` | `ET_Attachment_29_SmokeEffect_1` through `ET_Attachment_32_SmokeEffect_4` | `SM1..4` | Optional effect or smoke positions with nearest-slot selection. |
| `33..36` | `ET_Attachment_33_WT_1` through `ET_Attachment_36_WT_4` | `WT1..4` | Preserved slots; no normal gameplay transform consumer is confirmed in the examined build. |
| `37..38` | `ET_Attachment_37_CH_1`, `ET_Attachment_38_CH_2` | `CH1..2` | Paired emitter anchors used by direct setup paths. |
| `39..40` | `ET_Attachment_39_ST_1`, `ET_Attachment_40_ST_2` | `ST1..2` | Paired state-specific emitter anchors. |
| `41..42` | `ET_Attachment_41_SE_1`, `ET_Attachment_42_SE_2` | `SE1..2` | Paired general effect-emitter anchors. |
| `43..44` | `ET_Attachment_43_SK_1`, `ET_Attachment_44_SK_2` | `SK1..2` | Paired general effect-emitter anchors using the alternate setup mode. |
| `45` | `ET_Attachment_45_ChildAlignment_1` | `IN0` or `IN1` | Child alignment offset subtracted from a parent `MI` anchor. Original AOD commonly uses number `0`. |
| `46` | `ET_Attachment_46_Center_1` | `CE0` or `CE1` | General center anchor for placement, previews, HUD, and render auxiliaries. Original AOD commonly uses number `0`. |
| `47` | `ET_Attachment_47_Production_1` | `PR1` | Production and placement position plus heading. |
| `48` | `ET_Attachment_48_Movement_1` | `MV1` | Movement and placement position plus heading; paired with production. |
| `49` | `ET_Attachment_49_Landing_1` | `LN1` | Landing and placement position plus heading. |

### Directional Empty Presentation In Blender

glTF cannot prescribe Blender viewport Empty display shapes. As a Blender-only presentation step, run this in the **Scripting** workspace after import to show directional attachment helpers as arrows:

```python
import bpy

directional_helpers = {
    f"ET_Cannon_{number}_Attachment_{number}" for number in range(1, 5)
}
directional_helpers.update(
    f"ET_Attachment_{number + 20:02d}_Transport_{number}" for number in range(1, 5)
)
directional_helpers.update({
    "ET_Attachment_47_Production_1",
    "ET_Attachment_48_Movement_1",
    "ET_Attachment_49_Landing_1",
})

for obj in bpy.context.scene.objects:
    if obj.type == "EMPTY" and obj.name in directional_helpers:
        obj.empty_display_type = "SINGLE_ARROW"
```

This changes only the Blender viewport presentation; it does not add glTF or MSH semantics. The arrow follows the helper's encoded horizontal heading.

## Fast Checks Before Import

- The file is GLB or separate glTF 2.0 and has one intended scene.
- Existing edits still contain their `earthtool` custom properties.
- New objects have no copied EarthTool object or mesh identity.
- Attachment helper names match the table exactly and helpers have no mesh or children.
- The scene is 24 FPS; animation names and frame limits follow the animation section above.
- Geometry is triangular, finite, and has normals; textured primitives have UVs.
- Blender export includes Extras, Attributes, Lights, and Animations.
- `msh import edit` uses the IDs from the same GLB's export report.
- The original MSH and WD archive remain backed up.

For binary details and deeper runtime evidence, see [MSH format: Attachments and slots](MSH_FORMAT.md#attachments-and-slots). For metadata reconciliation, topology rules, and animation behavior, see the [glTF API](api/gltf.md).
