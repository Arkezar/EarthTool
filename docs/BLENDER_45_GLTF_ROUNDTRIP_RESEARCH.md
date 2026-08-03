# Blender 4.5 glTF round-trip research

<!-- markdownlint-disable MD013 -->

## Conclusion

A stock Blender 4.5 LTS round trip is suitable for ordinary static geometry,
single-scene object hierarchy, shared mesh instances, basic TRS animation,
`KHR_materials_unlit` base color, and most punctual-light fields. It is not a
transparent glTF editor. Blender reconstructs its own scene, mesh, action,
material-node, image, and light data, then generates a new glTF asset.

The safe contract is semantic rather than representational:

- Use native glTF for artist-editable geometry, object transforms and
  hierarchy, material assignment, unlit base color, UV selection, supported
  alpha settings, and supported TRS animation.
- Preserve MSH-only state in versioned EarthTool metadata attached only to
  Blender-backed scopes. Do not place required metadata on primitives,
  accessors, animation channels/samplers, images, textures, or samplers.
- Require the Blender export options **Custom Properties** and **Attributes**
  when those respective channels are used. Both are off by default in 4.5.
- Keep **Merge Vertices** off. It is off by default and is destructive to
  distinguishable duplicate vertices and custom-attribute values.
- Do not promise primitive order, accessor identity, vertex/index identity,
  degenerate triangles, multiple-scene membership, exact cubic tangents,
  light range, texture URI/object identity, or arbitrary JSON extras through
  stock Blender.

The strongest newly discovered blocker is a Blender 4.5.12 importer defect:
a valid mesh containing underscore attributes of different accessor arities
can raise `ValueError` during import. Until that is narrowed or fixed, a stock
workflow should not depend on a heterogeneous set of custom attributes.

Dynamic-effect glTF remains out of scope.

## Evidence model

Claims use these evidence levels:

- **[G] glTF guarantee**: normative Khronos glTF 2.0 or ratified-extension
  text. This says what a valid asset means, not what Blender preserves.
- **[D] Blender documented behavior**: Blender 4.5 manual or Python API.
- **[S] Blender 4.5 source-derived behavior**: bundled `io_scene_gltf2` at
  Blender tag `v4.5.12`, commit
  [`84afd5f785f7569b97cf3257000403e7847120a8`][blender-commit].
- **[E] Blender 4.5 observation**: the deterministic headless experiment
  described below, run with the official Blender 4.5.12 LTS Linux build.

Where they differ, [E] describes this tested patch and option set, [S]
explains the implementation, [D] describes the user-facing intent, and [G]
only defines the input/output meaning. No empirical result is elevated into a
format guarantee.

## Tested build and fixture

The official portable binary was
`blender-4.5.12-linux-x64.tar.xz`, downloaded from Blender's
[4.5 release archive][blender-download]. Its SHA-256 was
`95e3a2dfedba3bd32ca54fc355eac6b15a11986954ccb02815a07535d0120a25`,
matching Blender's [published checksum file][blender-checksum]. `blender
--version` reported:

```text
Blender 4.5.12 LTS
build date: 2026-07-21
build hash: 84afd5f785f7
build branch: blender-v4.5-release
build type: Release
```

The source tag resolves to the same commit. The bundled exporter identifies
itself in output as `Khronos glTF Blender I/O v4.5.51`.

### Diagnostic asset

The generated, validator-clean separate glTF fixture deliberately contained:

- two scenes, including one root node shared by both scenes;
- empty transform nodes, a matrix-authored node, mesh nodes, and three nodes
  referencing one mesh;
- one mesh with two ordered primitives, different materials, duplicate
  positions with different `_SCALAR` values, and one degenerate triangle;
- scene, node, mesh, primitive, material, camera, light, animation, channel,
  target, sampler, accessor, image, texture, buffer, asset, and root extras;
- string, Boolean, integer, large integer, float, null, homogeneous-array,
  mixed-array, and nested-object extra values;
- three animations named `LinearMove`, `StepScale`, and `CubicRotate`, with
  times `0`, `1/24`, and `2/24` seconds;
- directional, point, and spot lights with distinct color, intensity, range,
  cone, transform, and extras values;
- two unlit materials, one textured through `TEXCOORD_1`, with distinct base
  colors, alpha modes, cutoff, sidedness, and deliberately irrelevant unlit
  PBR/emissive fallback values;
- a 2x2 RGBA PNG and a sampler with distinguishable filters and wraps.

Khronos `gltf-validator` `2.0.0-dev.3.10` reported zero errors and zero
warnings. Its hints were the intentional degenerate triangle, omitted
recommended buffer-view targets, and deliberately unused diagnostic
accessors/UVs. The glTF specification discourages rather than forbids
degenerate triangles (`SHOULD NOT`), so the input remained valid
([G: geometry and application attributes][spec-geometry]).

No edit was made between import and export. This isolates conversion behavior;
ordinary artist edits can cause additional changes.

### Stock options

The principal default-identity run explicitly supplied these import options:

```python
import_pack_images=True
merge_vertices=False
import_shading='NORMALS'
import_scene_extras=True
import_scene_as_collection=True
import_merge_material_slots=True
export_import_convert_lighting_mode='SPEC'
```

These export options were used, once for GLB and once for separate glTF in
fresh Blender processes:

```python
export_extras=True
export_attributes=True
export_cameras=True
export_lights=True
export_yup=True
export_animations=True
export_force_sampling=True
export_frame_step=1
export_sampling_interpolation_fallback='LINEAR'
export_animation_mode='ACTIONS'
export_materials='EXPORT'
export_image_format='AUTO'
export_keep_originals=False
export_apply=False
export_shared_accessors=False
export_import_convert_lighting_mode='SPEC'
```

`export_extras`, `export_attributes`, `export_cameras`, and `export_lights`
were intentional opt-ins. Their 4.5 defaults are false. `export_yup`,
`export_animations`, and `export_force_sampling` default true; frame step
defaults to one; animation mode defaults to Actions
([S: exporter option declarations][source-export-options]). Import
`merge_vertices` defaults false, while scene extras, scene-as-collection, and
material-slot merging default true
([S: importer option declarations][source-import-options]).

Additional runs changed exactly one policy at a time:

- `merge_vertices=True` tested the opt-in merge path.
- `export_force_sampling=False` tested direct F-curve export.
- A validator-clean mixed-attribute fixture added float scalar, VEC2, VEC3,
  VEC4, and normalized unsigned-short VEC4 custom semantics to the same
  primitives and tested importer robustness.

Every completed GLB and separate glTF output validated with zero errors and
zero warnings. GLB is therefore a packaging default, not a different semantic
contract; separate glTF is supported under the same constraints.

## Extras and custom properties

glTF permits `extras` on all core property objects, and an extras value may
have any JSON type, although an object is recommended for portability
([G: extras schema][spec-extras]). Blender does not retain a generic glTF
object graph. It only copies selected extras dictionaries into Blender custom
properties.

The importer only accepts an extras value that is a dictionary. For each key,
it attempts an ID-property assignment and, on failure, stores `str(value)`.
The exporter walks custom properties, filters a blacklist, converts supported
values, and drops values it cannot convert
([S: extras conversion][source-extras]). **Custom Properties** export is false
by default ([S: option][source-export-options]); the Blender manual also
describes custom properties as glTF extras
([D: glTF custom properties][manual-gltf]).

### Scope survival matrix

| glTF scope | Stock Blender storage | Result with required options | Evidence |
| --- | --- | --- | --- |
| glTF root and `asset` | None | Lost | [S] No import storage path; [E] both markers disappeared |
| Default scene | Existing Blender scene custom properties | Conditional, lossy JSON; `import_scene_extras` and `export_extras` must be true | [S: default-scene-only code][source-scene]; [E] marker survived |
| Non-default scene | No extras copy | Lost | [S: default-scene-only code][source-scene]; [E] secondary marker disappeared |
| Node/object, including empty, mesh, camera, and light nodes | Blender Object custom properties | Conditional, lossy JSON; `export_extras` must be true | [S: node creation][source-node]; [E] all node markers survived |
| Mesh | Blender Mesh custom properties | Conditional, lossy JSON; `export_extras` must be true | [S: mesh extras][source-mesh-import]; [E] marker survived |
| Primitive | None | Lost | [S] exporter hard-codes primitive `extras=None` ([source-primitives]); [E] both markers disappeared |
| Material | Blender Material custom properties | Conditional, lossy JSON; `export_extras` must be true | [S: material import][source-material-import]; [E] markers survived |
| Camera definition | Blender Camera custom properties | Conditional, lossy JSON; `export_extras` must be true | [S: camera import][source-camera], [S: camera export][source-camera-export]; [E] marker survived |
| Punctual light definition | Blender Light custom properties | Conditional, lossy JSON; `export_extras` must be true | [S: light import/export][source-light-import]; [E] markers survived |
| Animation | New Blender Action, but no extras copy | Input extras lost. Blender-authored Action properties can export as animation extras | [S: action creation][source-animation-action-create], [S: action export][source-animation-action-export]; [E] action properties were empty |
| Animation channel, target, sampler | No stock custom-property mapping | Lost | [S] generated objects use `extras=None` ([source-animation-output]); [E] all markers disappeared |
| Accessor, buffer view, buffer | Rebuilt binary data | Lost | [S] generated animation accessors use `extras=None` ([source-animation-sampler]); [E] all markers disappeared |
| Image, texture, texture info, texture sampler | Blender image and shader-node representation | Extras lost | [S: image reconstruction][source-image], [S: sampler reconstruction][source-sampler]; [E] all markers disappeared |
| Nested camera projection, PBR, texture-info, light-extension, and spot extras | No stock custom-property mapping | Lost | [S] only top camera/material/light datablocks receive extras; [E] markers disappeared |

### JSON value behavior

The following values were placed at scene, root-node, mesh, and first-material
scopes. The output was the same at each surviving scope.

| Input JSON | Output JSON | Assessment |
| --- | --- | --- |
| String | Same string | Survived |
| Boolean | Same Boolean | Survived |
| Small integer `42` | Same integer | Survived |
| Integer `5000000000` | String `"5000000000"` | Type lost |
| Float `1.25` | Same number | Survived in this exact case; normal binary-float precision still applies |
| Top-level custom-property null | Property imported, then omitted on export | Lost |
| Homogeneous integer array | Same JSON array | Survived |
| Mixed array `[1, "two", false]` | String `"[1, 'two', False]"` | Structure and JSON spelling lost |
| Nested object with integer and null | Same object in this fixture | Survived, but only because Blender accepted and reconverted this shape |

This is consistent with the source's best-effort assignment/string fallback
and narrow exporter conversion ([S: extras conversion][source-extras]). It is
not safe to treat Blender custom properties as an arbitrary JSON DOM.

**Constraint:** exact versioned EarthTool metadata should use a string-valued
serialized payload or a deliberately tested ID-property subset. A JSON object
may be the outer `extras` container, but arbitrary nested JSON values cannot be
promised. This constrains, but does not choose, the later metadata schema.

## Underscore-prefixed custom attributes

glTF application-specific vertex semantics must start with `_` and must not
use unsigned-int components. All attributes of a primitive have the same
element count ([G: attribute rules][spec-geometry]). glTF has no Blender
POINT/CORNER domain concept; it has one value per glTF vertex.

Blender 4.5 imports every recognized underscore semantic into a `POINT` mesh
attribute. It gathers values once per used source vertex and inserts zeroes
where another primitive lacks the semantic
([S: custom-attribute import][source-attribute-import]). On export, **Attributes**
must be enabled; it is false by default. Blender keeps attributes beginning
with `_`, ignores EDGE-domain attributes, supports POINT, CORNER, and FACE
gathering, and uppercases the exported semantic
([S: attribute selection][source-attribute-export],
[S: domain gathering][source-attribute-domains]).

### Supported import shapes

The source maps these accessor forms to Blender types
([S: type conversion][source-attribute-types]):

| glTF accessor | Blender type | Re-export tendency |
| --- | --- | --- |
| FLOAT SCALAR | `FLOAT` | FLOAT SCALAR |
| UNSIGNED_BYTE SCALAR | `INT` | FLOAT SCALAR; component type changes |
| FLOAT VEC2 | `FLOAT2` | FLOAT VEC2 |
| FLOAT VEC3 | `FLOAT_VECTOR` | FLOAT VEC3 |
| FLOAT VEC4 | `FLOAT_COLOR` | FLOAT VEC4 |
| UNSIGNED_BYTE or UNSIGNED_SHORT VEC4 | `BYTE_COLOR` | normalized UNSIGNED_SHORT VEC4 |
| FLOAT MAT4 | `FLOAT4X4` | FLOAT MAT4 |
| Other legal application-specific forms | No mapping | Skipped |

Normalized integer data is decoded to Blender values; representation identity
is not retained. Exported BYTE_COLOR and underscore attributes using that path
are quantized to normalized unsigned short
([S: attribute accessor generation][source-attribute-accessor]).

### Identity, order, collision, and merge behavior

- The default fixture's `_SCALAR` values and duplicate-position identities
  survived semantically: first primitive values remained `10.25` through
  `14.25`, and its two equal positions remained separate. Primitive-local
  vertex order happened to survive. [E]
- This is not an ordering guarantee. Blender reconstructs vertices, then uses
  structured uniqueness when generating each primitive; CORNER data can split
  one Blender point into multiple glTF vertices
  ([S: primitive generation][source-primitive-generation]). Any topology,
  normal, UV, or attribute edit may reindex values.
- Source semantics are imported with their original spelling, but export
  uppercases them. `_foo` becomes `_FOO`. Two Blender attributes that uppercase
  to the same semantic collide; Blender warns and ignores one
  ([S: collision handling][source-attribute-export]).
- With `merge_vertices=True`, the fixture's vertex count changed from eight to
  seven. The two equal positions with source values `10.25` and `13.25` were
  merged, and both exported splits carried `10.25`; vertex ordering also
  changed. This exactly matches the merge implementation, which excludes
  custom attributes from its equality key and retains one arbitrary value
  ([S: merge implementation][source-vertex-merge]). [E]
- The mixed-arity fixture was valid according to the Khronos validator, but
  `bpy.ops.import_scene.gltf` raised `ValueError` while concatenating custom
  arrays of different widths. Blender reported the Python exception and quit
  normally; the process itself did not crash. The import source appends data
  in one order, then enumerates a set in another order, allowing metadata/data
  widths to mismatch ([S: importer loop][source-attribute-import]). [E]

**Constraint:** keep vertex merging off and do not use a custom attribute as
the sole authoritative copy of exact MSH vertex identity. One simple scalar
attribute is empirically viable for diagnostics or fingerprints; a
heterogeneous attribute set is not currently stock-4.5.12-safe.

## Nodes, hierarchy, scenes, and transforms

glTF requires a disjoint forest: every node has zero or one parent. A root may
appear in multiple scenes, and a node may reference a reusable mesh. Nodes may
use a decomposable matrix or TRS, but animated nodes may not use a matrix
([G: scenes and hierarchy][spec-scenes]). Multi-parent node instancing is
therefore not valid glTF; mesh instancing is the relevant mechanism.

| Construct | Round-trip result | Evidence and constraint |
| --- | --- | --- |
| Empty/non-mesh transform node | Survived as Blender Empty and returned as glTF node | [S: empty creation][source-node]; [E] |
| Mesh node | Survived as Blender Object plus Mesh datablock | [S: node creation][source-node]; [E] |
| Parent/child hierarchy in default scene | Survived in the fixture | [S: parent assignment][source-node]; [E] |
| Multiple nodes referencing one mesh | Survived as three objects sharing one Mesh and one output glTF mesh | [S: mesh caching][source-mesh-export]; [E] |
| Matrix-authored node | Transform survived semantically but exported as TRS | [G] matrix/TRS are equivalent local-transform forms; [E] |
| `+Y`-up conversion | glTF values converted to Blender coordinates and returned to original glTF semantics with `export_yup=True` | [S: Y-up conversion][source-yup]; [E] |
| Scene name and roots | Default input scene name became existing Blender scene name `Scene`; root ordering changed | [S] importer reuses the current scene ([source-scene]); [E] |
| Multiple scenes | Not preserved under default `import_scene_as_collection=True` | [S] non-default scenes/collections are reconstructed ([source-multiscene]); [E] secondary output scene was empty and its mesh became a primary-scene root |
| Same root in multiple scenes | Not preserved in the fixture | [G] allowed by glTF; [E] shared camera returned only in the primary scene |
| Animated node's static TRS | Not a stable rest/default representation in this run | [E] imported actions changed the exported node's static rotation/scale state; channels themselves survived |

**Constraint:** target one glTF scene for the stock workflow. Use a strict
object tree and mesh references for geometry sharing. Do not encode semantic
identity in scene-root order, node array indices, or matrix-vs-TRS spelling.
Animated-node rest/default transforms require a later explicit contract.

## Animations

glTF sampler inputs are strictly increasing FLOAT SCALAR seconds; supported
interpolations are LINEAR, STEP, and CUBICSPLINE. CUBICSPLINE stores
in-tangent, value, and out-tangent for each key
([G: animation samplers][spec-animation]). glTF does not define playback,
looping, or authoring-tool action mapping.

Blender creates Actions and NLA tracks using each glTF animation name, making
names pairwise unique when needed. One imported animation can use one Action
with slots for its target objects
([S: name dispatch][source-animation-dispatch],
[S: action creation][source-animation-action-create],
[S: NLA storage][source-animation-actions]). Imported seconds are
multiplied by scene FPS to create Blender frame coordinates
([S: time conversion][source-animation-channel]). Export divides frame numbers
by scene FPS ([S: keyframe conversion][source-animation-keyframe]); CUBICSPLINE
tangents are scaled to per-second units
([S: sampler conversion][source-animation-sampler]).

| Feature | Default sampled export | Direct export (`Always Sample` off) |
| --- | --- | --- |
| Names | Unique names survived; animation array order changed from Linear/Step/Cubic to Cubic/Linear/Step | Same |
| 24 FPS integer-frame timing | `0`, `1/24`, `2/24` survived as float seconds within float32 precision | Same |
| LINEAR | Remained LINEAR with the three expected values | Remained LINEAR |
| STEP | Remained STEP with the three expected values | Remained STEP |
| CUBICSPLINE | Became three LINEAR samples | Remained CUBICSPLINE, but tangents changed |
| Animation/channel/target/sampler/accessor extras | Lost | Lost |

The default is `export_force_sampling=True`, frame step one, LINEAR fallback
([S: defaults][source-export-options]). The importer explicitly discards
incoming cubic tangents and keeps only every middle value before creating
F-curves ([S: cubic import][source-animation-channel]). In the direct run, the
input's zero tangents were regenerated as nonzero Blender auto-handle tangents;
only interpolation type and evaluated key values survived. [E]

**Constraint:** 24 FPS integer frames map cleanly to glTF seconds when Blender
FPS is exactly 24 and `fps_base` is 1. Treat key times and evaluated TRS as the
portable data. Do not promise exact CUBICSPLINE tangents, animation ordering,
or imported animation metadata. The later animation contract must choose
sampled versus direct behavior and define action/rest-state handling.

## Punctual lights

`KHR_lights_punctual` defines directional, point, and spot lights referenced by
nodes. Point/spot use candela, directional uses lux, point/spot may have a
positive range, and spot cone angles are radians. Position and direction come
from the node; light properties are unaffected by node scale
([G: punctual-light specification][spec-lights]).

Blender maps these types to SUN, POINT, and SPOT. In Standard/SPEC mode it
converts between Blender energy and glTF photometric units; spot size is twice
the outer angle and spot blend derives from the inner/outer ratio
([S: light import][source-light-import], [S: light export][source-light-export]).

| Field | Result | Evidence |
| --- | --- | --- |
| Type and name | Survived for directional, point, and spot | [E] |
| Node transform and hierarchy | Survived semantically, including light-node extras | [E] |
| Linear RGB color | Survived within float precision | [E] |
| Intensity in SPEC mode | Survived within conversion/float precision: 2.5, 125, and 250 returned approximately | [S] [E] |
| Point/spot range | Lost; output omitted 12.5 and 8.25 | Import contains an explicit `TODO range` ([source-light-import]); [E] |
| Spot inner/outer cones | Survived approximately as 0.2 and 0.6 | [S] [E] |
| Light-definition extras | Survived with `export_extras=True` | [S] [E] |
| Root extension and nested spot extras | Lost | No Blender storage path; [E] |

Punctual-light export itself is off by default
([S: option][source-export-options]). Range can be newly authored if a Blender
light has custom distance enabled, but an imported glTF range does not set it
([S: range export][source-light-export]).

**Constraint:** native glTF may own supported light type, node transform,
color, intensity, and cone values. Any MSH value that depends on preserving
range or non-glTF light/attachment state needs EarthTool metadata. This does
not decide the later attachment/light mapping.

## Meshes, primitives, and topology

glTF meshes are ordered arrays of primitives, each binding attributes,
optional indices, and an optional material. Different primitives commonly
represent material partitions ([G: mesh primitives][spec-geometry]). Blender
imports all primitives of a glTF mesh into one Mesh, concatenates their faces,
and assigns material indices
([S: mesh reconstruction][source-mesh-import]).

| Property | Default result | Caveat |
| --- | --- | --- |
| Two primitive/material partitions | Survived as two Blender material slots and two output primitives | Materials were distinct |
| Primitive ordering | First/second survived in this fixture | Export buckets triangles by sorted material index, not source primitive ordinal ([source-primitive-split]) |
| Same-material primitive boundaries | Not safe | Default `import_merge_material_slots=True` can collapse slots; primitive extras are lost |
| Duplicate positions/vertex identity | Survived with merge off | Not an API guarantee; edits or CORNER splits can reindex |
| Opt-in vertex merge | Destructive | Seven vertices replaced eight and one custom value was chosen arbitrarily |
| Index values/order | Valid topology survived but buffers/accessors were rebuilt | Do not compare accessor/index identity |
| Degenerate triangle | Lost | Import created four source-valid faces, then `mesh.validate()` left three ([source-mesh-import]); [E] |
| Primitive extras | Lost | No storage/export path |
| Material slots | Distinct slots survived | Same-material and user slot edits can merge/reorder partitions |

The output first primitive had six indices instead of nine because the
degenerate triangle was removed. The two nondegenerate source triangles and
the second primitive's triangle remained. Both GLB and separate outputs were
validator-clean. [E]

**Constraint:** native glTF can carry artist geometry and material partitions,
but EarthTool must reconstruct game partitions semantically and validate them.
Exact MSH duplicate vertices and degenerate triangles cannot be promised after
stock Blender. Low-level SharpGLTF construction remains necessary on the
EarthTool side; Blender does not remove the need for topology fingerprints.

## Unlit materials and textures

`KHR_materials_unlit` uses core base color, alpha, vertex color, and
double-sided behavior while ignoring lighting-related PBR fields
([G: unlit specification][spec-unlit]). Core glTF defines base-color factor and
texture multiplication, optional UV-set selection, alpha mode/cutoff, and
double-sided rendering ([G: materials][spec-materials]).

Blender imports unlit materials into a recognizable emission/light-path node
graph and detects that graph again on export
([S: unlit import][source-unlit-import],
[S: unlit detection][source-unlit-export]). Material extras live on the
Material datablock, while image/texture/sampler identity is reconstructed from
shader nodes.

| Material data | Result | Evidence and limits |
| --- | --- | --- |
| Unique material names | Survived | [E]; names are not stable IDs under collisions |
| `KHR_materials_unlit` | Survived | [S] [E]; editing the recognized node graph can prevent detection |
| `baseColorFactor` | Survived within float precision | [E] |
| `baseColorTexture` | Survived | [E] |
| UV set (`texCoord: 1`) | Survived | [E] |
| `alphaMode: MASK` and cutoff 0.42 | Survived approximately | [S: alpha export][source-material-export]; [E] |
| `alphaMode: BLEND` | Survived | [E] |
| `doubleSided: true/false` | Survived semantically; false is omitted as default | [S: culling mapping][source-material-import]; [E] |
| Material extras | Survived with `export_extras=True`, subject to JSON loss | [E] |
| PBR/texture-info nested extras | Lost | No datablock mapping; [E] |
| Unlit metallic/roughness fallback | Input 0.37/0.61 became canonical 0/0.9 | Lighting fields are ignored by unlit and reconstructed as fallback ([spec-unlit]); [E] |
| Input emissive factor | Lost | Not part of the recognized unlit color contract; [E] |
| PNG payload | Exact bytes survived this no-edit AUTO run, including GLB embedding | SHA-256 remained `68a807...453`; do not generalize across image edits/formats |
| Image URI and packaging | Separate output used `diagnostic.png`; GLB used an image buffer view | Packaging is regenerated; URI/index identity is not portable |
| Image/texture/sampler names and extras | Lost | [S: reconstruction][source-image]; [E] |
| Sampler wrap | Clamp-S and mirrored-T survived | [S: wrap reconstruction][source-sampler]; [E] |
| Sampler filters | Nearest magnification survived; input nearest minification became nearest-mipmap-nearest | Blender shader filtering has less state than glTF sampler filtering ([source-sampler]); [E] |
| Unsupported material properties/extensions | No generic passthrough | Only recognized node mappings are generated; unknown exact JSON must be treated as lost |

**Constraint:** the decided unlit base-color workflow fits stock Blender if its
generated node graph remains recognizable. Native glTF may own base color,
texture, UV set, alpha, and sidedness. EarthTool must not depend on exact PBR
fallback values, sampler filter identity, image/texture indices, or URI
spelling. This constrains, but does not complete, the material workflow.

## Decision consequences for later tickets

### Native glTF candidates

Subject to the constraints above, later specifications may safely treat these
as artist-editable native glTF data:

- single-scene strict object hierarchy, empties, mesh nodes, and local TRS;
- shared mesh datablocks referenced by multiple nodes;
- positions, normals, UVs, triangles, and material assignment, where semantic
  topology rather than source buffer identity is authoritative;
- one simple underscore scalar attribute when explicitly enabled, if a later
  ticket establishes a concrete need and failure policy;
- unique animation names, evaluated TRS keys, 24 FPS integer-frame timing, and
  LINEAR/STEP interpolation; CUBICSPLINE only under a chosen lossy policy;
- punctual-light type, transform, color, intensity, and spot cones;
- unlit material name, base-color factor/texture, UV set, alpha mode/cutoff,
  and sidedness.

### EarthTool metadata requirements

Versioned EarthTool metadata is required for MSH-only data and for any native
projection whose source representation must be recovered exactly. Required
metadata may live only on scopes Blender actually backs:

- default-scene custom properties for genuinely asset-wide state in a
  single-scene workflow;
- Object/node custom properties for source-object-specific state;
- Mesh custom properties for state shared by all instances of a mesh;
- Material, Camera, or Light custom properties for state owned by those
  datablocks.

Do not put the only copy on glTF root/asset, non-default scenes, primitives,
accessors, buffers, animations, channels, samplers, images, textures, texture
info, or nested extension objects. Do not assume arbitrary JSON types. A
versioned serialized string under a custom-property key is the least lossy
stock-compatible carrier demonstrated by this research, but the later
metadata-contract ticket must choose its schema and locations.

### Required fingerprints and invalidation

Because Blender regenerates identity-sensitive structures, later import must
validate metadata against native data before using it. At minimum, the design
needs fingerprints or explicit invariants for:

- object-tree shape and stable logical IDs independent of names and node array
  order;
- mesh instance ownership versus per-object state;
- material-partition membership independent of primitive order;
- triangle count and canonicalized topology, including detection that a
  degenerate source triangle was removed;
- vertex count and distinguishable duplicate/custom-attribute values,
  detecting merge or reindexing;
- material name/role, base-color texture and UV-set linkage;
- animation target/path, integer-frame times, evaluated values, and chosen
  interpolation policy;
- light type and supported native values, with range treated as absent after
  stock import;
- metadata version, scope, and source-content fingerprint.

An invalid fingerprint must produce a diagnostic and invoke a later-defined
rebuild, discard, or user-resolution policy. It must not silently apply stale
MSH serialized representations to edited geometry.

### What stock Blender cannot promise

- exact GLB bytes, JSON spelling, object-array indices, accessors, buffer
  layout, index values, or vertex order;
- primitive ordering or boundaries, especially when materials are shared;
- preservation of degenerate triangles or deliberately duplicated vertices
  after merge/edit operations;
- heterogeneous underscore attributes in Blender 4.5.12;
- arbitrary JSON extras or extras on unsupported scopes;
- multiple-scene roots, membership, names, and extras under default import;
- exact CUBICSPLINE tangents, animation ordering, or animated-node rest state;
- punctual-light range;
- exact material fallback fields, sampler minification mode, image URI,
  texture/sampler object identity, or unsupported material extensions;
- preservation of EarthTool metadata if the user exports without **Custom
  Properties**, or custom attributes without **Attributes**.

Ordinary geometry does not require an add-on. An optional add-on may improve
validation, option presets, metadata UI, or unsupported-scope preservation,
but the base static-model workflow must remain useful without it.

## Sharpened follow-up questions

These are ticket-sized evidence gaps; no tracker changes were made.

1. What is the minimal stock-safe custom-attribute encoding on every supported
   Blender 4.5 LTS patch, given the 4.5.12 mixed-arity importer failure?
2. Can same-material source partitions be preserved acceptably with
   `import_merge_material_slots=False`, and how do common artist operations
   affect them?
3. What explicit rest/default-transform and Action/NLA policy is required for
   animated nodes so export is independent of the currently restored action?
4. Should the supported workflow reject multiple scenes at ingest or flatten
   them with an explicit diagnostic?
5. What exact texture-content fingerprint survives expected artist image
   edits, repacking, format conversion, and GLB/separate packaging?
6. What size, character, and UI-edit limits apply to a serialized EarthTool
   metadata string in Blender custom properties?
7. Should EarthTool always sample animations at 24 FPS, or permit direct
   LINEAR/STEP curves while rejecting or resampling imported CUBICSPLINE?

## Primary sources

- Blender 4.5 LTS manual: [glTF 2.0 import/export][manual-gltf] and
  [Custom Properties][manual-custom-properties].
- Blender 4.5 Python API: [ID datablocks and custom-property access][api-id].
- Blender `v4.5.12` source commit: [`84afd5f...`][blender-commit].
- Khronos glTF source commit: [`77b44be7...`][gltf-commit].
- Khronos [glTF 2.0 specification][spec-main],
  [`KHR_lights_punctual`][spec-lights], and
  [`KHR_materials_unlit`][spec-unlit].
- Official Blender [4.5 download archive][blender-download] and
  [4.5.12 checksums][blender-checksum].

[api-id]: https://docs.blender.org/api/4.5/bpy.types.ID.html
[blender-checksum]: https://download.blender.org/release/Blender4.5/blender-4.5.12.sha256
[blender-commit]: https://github.com/blender/blender/tree/84afd5f785f7569b97cf3257000403e7847120a8
[blender-download]: https://download.blender.org/release/Blender4.5/
[gltf-commit]: https://github.com/KhronosGroup/glTF/tree/77b44be7bef26e01fb0b140e3d5bb1716421c5e9
[manual-custom-properties]: https://docs.blender.org/manual/en/4.5/files/custom_properties.html
[manual-gltf]: https://docs.blender.org/manual/en/4.5/addons/import_export/scene_gltf2.html
[source-animation-actions]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/animation.py#L20-L139
[source-animation-action-create]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/animation_utils.py#L92-L139
[source-animation-action-export]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/animation/action.py#L691-L709
[source-animation-dispatch]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/blender_gltf.py#L221-L244
[source-animation-channel]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/animation_node.py#L52-L167
[source-animation-keyframe]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/animation/keyframes.py#L11-L15
[source-animation-output]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/animation/fcurves/channels.py#L278-L298
[source-animation-sampler]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/animation/fcurves/sampler.py#L146-L217
[source-attribute-accessor]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/primitive_attributes.py#L174-L209
[source-attribute-domains]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/primitive_extract.py#L1327-L1408
[source-attribute-export]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/primitive_extract.py#L173-L227
[source-attribute-import]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/mesh.py#L72-L279
[source-attribute-types]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/com/conversion.py#L88-L185
[source-camera]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/camera.py#L17-L71
[source-camera-export]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/cameras.py#L14-L50
[source-export-options]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/__init__.py#L557-L943
[source-extras]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/com/extras.py#L10-L89
[source-image]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/image.py#L20-L94
[source-import-options]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/__init__.py#L1868-L1974
[source-light-export]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/lights.py#L18-L195
[source-light-import]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/light.py#L19-L143
[source-material-export]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/material/materials.py#L220-L250
[source-material-import]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/material.py#L41-L116
[source-mesh-export]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/mesh.py#L20-L79
[source-mesh-import]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/mesh.py#L34-L547
[source-multiscene]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/vnode.py#L132-L200
[source-node]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/node.py#L30-L129
[source-primitive-generation]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/primitive_extract.py#L893-L956
[source-primitive-split]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/primitive_extract.py#L385-L415
[source-primitives]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/primitives.py#L90-L104
[source-sampler]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/sampler.py#L29-L122
[source-scene]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/scene.py#L20-L66
[source-unlit-export]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/material/unlit.py#L16-L70
[source-unlit-import]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/KHR_materials_unlit.py#L8-L56
[source-vertex-merge]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/imp/mesh.py#L820-L920
[source-yup]: https://github.com/blender/blender/blob/84afd5f785f7569b97cf3257000403e7847120a8/scripts/addons_core/io_scene_gltf2/blender/exp/primitive_extract.py#L1119-L1128
[spec-animation]: https://github.com/KhronosGroup/glTF/blob/77b44be7bef26e01fb0b140e3d5bb1716421c5e9/specification/2.0/Specification.adoc#L2397-L2633
[spec-extras]: https://github.com/KhronosGroup/glTF/blob/77b44be7bef26e01fb0b140e3d5bb1716421c5e9/specification/2.0/schema/extras.schema.json#L1-L8
[spec-geometry]: https://github.com/KhronosGroup/glTF/blob/77b44be7bef26e01fb0b140e3d5bb1716421c5e9/specification/2.0/Specification.adoc#L1287-L1424
[spec-lights]: https://github.com/KhronosGroup/glTF/blob/77b44be7bef26e01fb0b140e3d5bb1716421c5e9/extensions/2.0/Khronos/KHR_lights_punctual/README.md#L25-L127
[spec-main]: https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html
[spec-materials]: https://github.com/KhronosGroup/glTF/blob/77b44be7bef26e01fb0b140e3d5bb1716421c5e9/specification/2.0/Specification.adoc#L2014-L2225
[spec-scenes]: https://github.com/KhronosGroup/glTF/blob/77b44be7bef26e01fb0b140e3d5bb1716421c5e9/specification/2.0/Specification.adoc#L725-L816
[spec-unlit]: https://github.com/KhronosGroup/glTF/blob/77b44be7bef26e01fb0b140e3d5bb1716421c5e9/extensions/2.0/Khronos/KHR_materials_unlit/README.md#L31-L150
