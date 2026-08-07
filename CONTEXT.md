# MSH Format

This context describes the domain language used for Earth-series MSH geometry and effect records.

## Language

**Serialized representation**:
An exact value or region carried by an accepted MSH file. It remains authoritative inside that loaded mesh asset and through direct MSH writes, even when a semantic view exists; deliberate glTF asset creation instead emits canonical authored representations.
_Avoid_: Cached semantic value, normalized value

**Semantic view**:
A named interpretation derived from serialized representations, with its role and units made explicit. It does not silently rewrite or reconcile the representations it interprets.
_Avoid_: Raw field, synchronized field

**Known value view**:
A semantic view that names recognized values of a numeric serialized representation. An unrecognized value remains available as its exact number rather than being collapsed into a generic unknown case.
_Avoid_: Catch-all Unknown value

**Unclassified representation**:
A serialized representation whose location, shape, or source code is known but whose intended semantic role is not. Its name states those known facts rather than using a numbered `Unknown` placeholder.
_Avoid_: Unknown1, hidden bytes, guessed meaning

**Canonical authored representation**:
A coherent set of serialized representations produced from explicit authoring inputs for a new or deliberately converted model. It is distinct from preserving a loaded file's accepted values.
_Avoid_: Repaired file, normalized loaded model

**Canonical authoring default**:
A deterministic value emitted when glTF provides no reliable semantic evidence and no typed authoring input is supplied. It does not claim to recover source intent.
_Avoid_: Inferred semantic, retained value

**Structural hazard**:
A serialized declaration or reference that prevents safe, bounded, and unambiguous materialization of a mesh asset. Structural hazards are rejected without producing a partial mesh.
_Avoid_: Malformed warning, recoverable corruption

**Compatibility anomaly**:
A safely bounded and exactly representable departure from canonical authored or confirmed game-compatible expectations. It remains authoritative when loaded and is surfaced without silent normalization.
_Avoid_: Corruption, normalization target

**Base header**:
The common `0x368`-byte `MESH` record shared by static meshes and dynamic effects. It excludes the static trailing hierarchy unwind count and dynamic effect extension.
_Avoid_: Static descriptor, subtype header

**Canonical dynamic base header**:
The exact canonical authored base-header representation for a dynamic object: supported version 1, dynamic mesh kind, zero-valued inherited shared regions, and 49 canonical absent attachments. A loaded dynamic base header need not be canonical and remains authoritative; a departure is a compatibility anomaly.
_Avoid_: Valid dynamic base header, normalized dynamic header

**Mesh asset**:
A top-level framed MSH and its parsed static or dynamic payload.
_Avoid_: Earth mesh, model file

**Mesh asset origin**:
A nonserialized classification of how an immutable mesh asset was constructed: loaded from accepted bytes, canonically authored, or created from an expert-supplied exact representation. It does not change serialized authority.
_Avoid_: Source provenance, file origin

**Static mesh asset**:
A mesh asset whose mesh kind is `Static` and whose payload is an authoritative static render-object sequence plus its reconstructed source object tree.
_Avoid_: Static payload, static model

**Dynamic mesh asset**:
A mesh asset whose mesh kind is `Dynamic` and whose payload is a root dynamic object.
_Avoid_: Dynamic payload, dynamic model

**Static render object**:
One serialized static geometry/material record. Multiple static render objects can be material partitions of one source object.
_Avoid_: Model part, geometry part

**Static render-object sequence**:
The authoritative linear serialized order of static render objects. The source object tree groups records from this sequence without becoming a second ordering authority.
_Avoid_: Flattened tree, unordered geometry list

**Canonical static render-object sequence**:
The deterministic static render-object sequence emitted by canonical authoring and every glTF asset creation. It places each source object's first static render object before its child source objects, followed recursively by those children, then places that source object's remaining static render objects; only direct MSH read and write preserve a loaded noncanonical sequence.
_Avoid_: Tree order, glTF primitive order, minimally edited sequence

**Static render-object flags**:
The recognized object-flag roles `ViewerFaced`, `Barrel`, `Rotor`, `BeginsNestedSourceObject`, and `MarkerAttachment1` through `MarkerAttachment4`. Zero means no recognized role, not a distinct base-part type.
_Avoid_: Part type, Base, Subpart, Emitter

**Hierarchy unwind count**:
The low-byte object-flag value that returns from completed source-object levels before a static render object is processed.
_Avoid_: Backtrack depth

**Unclassified object-flags high word**:
Serialized object-flag bits 16 through 31, for which no generated nonzero value or semantic consumer is confirmed.
_Avoid_: Empty, reserved semantics

**Triangle render-pass flags**:
The triangle mask whose normal-material and snow-overlay bits select render-pass membership.
_Avoid_: Face flags, triangle type

**Shared vertex index**:
An earlier absolute render-vertex index reused for either transformed position or lighting-normal results; `0xFFFF` means no shared vertex.
_Avoid_: Vector index, recursive link

**Vertex-block padding**:
Physical vertex-block lanes or excess blocks beyond the declared active render-vertex count. They are not render vertices; loaded bytes remain opaque serialized data while canonical authoring writes zero-filled minimum padding.
_Avoid_: Padding vertices, inferred geometry

**Reserved texture component**:
The third serialized texture-coordinate lane. The producer writes zero and known renderers do not consume it, but accepted values remain part of the serialized representation.
_Avoid_: Texture W coordinate, homogeneous component

**Next-record marker**:
The static render-object dword whose zero/nonzero state links the next record. A nonzero serialized value is process-address noise rather than an offset and has canonical value `1` when rewritten.
_Avoid_: Next pointer, record offset

**Source object tree**:
The source-object hierarchy reconstructed from the linear static render-object sequence. Each source object node groups its material-partition render objects.
_Avoid_: Parts tree, render-object tree

**Dynamic object**:
A base header plus dynamic extension and its ordered child dynamic objects.
_Avoid_: Dynamic part, submesh

**Root dynamic object**:
The top-level dynamic object owned by a mesh asset. Root status is structural and independent of effect type; a Group effect may be nested and a non-Group effect may be the root.
_Avoid_: Group root, root effect type

**Group effect**:
Dynamic effect value zero, whose confirmed role is a child-container root with no own primary geometry or terrain light. `Group` is a semantic name, not a recovered original source symbol.
_Avoid_: Unknown effect, recovered Group enum

**Dynamic effect name**:
A corrected English API name for a known dynamic effect value, such as `ScalableObject`, `Lightning`, or `Keelwater`, independent of historical AOD spellings `ScaleableObject`, `Lighting`, and `Kilwater`. The serialized numeric value remains authoritative.
_Avoid_: Wire-format spelling

**Canonical dynamic object identifier**:
The strict case-sensitive `ET_Dynamic_{n}_{Effect}` glTF object name that declares a dynamic effect and binds its local typed authoring metadata. Renaming changes the authored semantic, duplicate identifiers are conflicts, and glTF hierarchy rather than the numeric label authors child structure.
_Avoid_: Dynamic scope identity, display name

**Dynamic effect behavior**:
The recognized applicability, canonical defaults, validation rules, and semantic evaluation associated with a known dynamic effect value. It interprets exact serialized representations without replacing their authority. An unrecognized effect value has no primary effect-specific behavior but may still use explicitly selected context-generic attached-particle evaluation.
_Avoid_: Dynamic extension layout, unknown-effect fallback

**Animation class**:
One of four frame-selection domains `A`, `B`, `C`, and `D`, serialized as numeric values `0`, `1`, `2`, and `3`. The classes do not have universal movement, action, lift, two-way, or single-playback meanings.
_Avoid_: Animation type, movement frames, action frames, building frames

**Animation class assignment**:
The animation class selected by one static render object. Every static render object has an assignment; canonical authoring uses class A when no canonical animation clip assigns another class.
_Avoid_: Optional animation class, playback behavior

**Animation lengths**:
The four reverse-packed declared frame-count bytes for animation classes A through D in the base header.
_Avoid_: Frames, total animation frames

**Animation frame indices**:
The four reverse-packed current-frame bytes for animation classes A through D in the base header.
_Avoid_: Header flags, reserved animation field

**Footprint representations**:
The independently serialized box presence mask, box top elevations, box corner-passage flags, rotated occupancy descriptors, and rotated corner-passage maps. A loaded representation does not derive authority from or synchronize with another.
_Avoid_: Coverage cache, derived footprint

**Box top elevation**:
An unsigned 8.8 fixed-point local top elevation for one logical footprint cell.
_Avoid_: Box height, cell floor, thickness

**Box corner-passage flags**:
Four per-cell exception bits that preserve diagonal passage around selected occupied-cell corners.
_Avoid_: Direction flags, side flags

**Rotated occupancy descriptor**:
One quarter-turn footprint descriptor containing an occupancy mask, iteration anchors, and producer midpoint biases.
_Avoid_: Coverage descriptor

**Rotated corner-passage map**:
One quarter-turn map of per-cell corner-passage flags aligned with a rotated occupancy descriptor.
_Avoid_: Coverage bitmap, directional bitmap

**Horizontal extents**:
Four unsigned 8.8 fixed-point magnitudes from the root pivot in `+Y`, `-Y`, `+X`, and `-X` order.
_Avoid_: Bounding rectangle, symmetric radius

**Mesh kind**:
The base-header discriminator whose recognized values `Static` and `Dynamic` declare the root payload shape.
_Avoid_: Archive type, Model

**Archive type**:
The optional archive-framing dword used by the game to select its runtime mesh class: an absent field defaults to zero, zero selects static, and every nonzero value selects dynamic. It is independently serialized from mesh kind and can disagree with it.
_Avoid_: Mesh kind, payload-shape discriminator

**Dynamic child record**:
A nested base header and dynamic extension declared by a parent dynamic object's child array. It has no archive framing and conventionally declares dynamic mesh kind.
_Avoid_: Nested archive, static child

**Archive framing**:
The required top-level prefix before `MESH`. Its low 24 bits are the `0xD0A1FF` archive signature; upper-byte bit `0x10` declares a following archive-type dword and bit `0x20` declares a following 16-byte GUID. A top-level payload beginning directly at `MESH` is not a framed MSH.
_Avoid_: MESH header

**Framed MSH**:
A MSH payload reached through archive framing with the `0xD0A1FF` low-24-bit signature. A bad-signature or raw `MESH` payload is not a framed MSH.
_Avoid_: Canonical MSH, raw MESH

**Stored trailing hierarchy unwind count**:
The pending source-hierarchy unwind serialized after a static MSH base header when no later render record remains to carry it.
_Avoid_: ConverterObjectCounter, StaticObjectCount, RegularMeshSubType, Unit/Building subtype

**Expected trailing hierarchy unwind count**:
The value derived from the final reconstructed source depth plus one and used to validate the stored trailing hierarchy unwind count.
_Avoid_: Stored value, object count

**Attachment table**:
The 49 independently serialized attachment records in the base header, including source slots and quantized light mirrors.
_Avoid_: Model slots, locator table

**Attachment record**:
One numbered record containing signed 8.8 fixed-point coordinates, a heading byte, and an extra byte. Semantic roles depend on its physical attachment number.
_Avoid_: Generic slot, synchronized position

**Canonical absent attachment**:
An attachment record whose three coordinate words all contain the `0x8000` sentinel emitted for an unset record.
_Avoid_: Invalid slot

**Runtime-available attachment**:
An attachment record whose X coordinate is not `0x8000`, matching the game accessors' availability test even when Y or Z contains unusual values.
_Avoid_: Canonical attachment, valid slot

**Cannon yaw half-range**:
The cannon-only interpretation of attachment records 1 through 4's extra byte. Other attachment records preserve the byte without assigning this angle meaning.
_Avoid_: Universal extra angle

**Cannon render position**:
One of four full-precision vectors used to translate visible attached child meshes. It remains independently serialized from the corresponding quantized cannon attachment.
_Avoid_: Mount point, synchronized cannon position

**Attachment range name**:
A descriptive category based on confirmed use, such as Cannon, Marker, Spot Light, Omni Light, Transport, Smoke Effect, Child Alignment, Center, Production, Movement, or Landing. Ranges whose intended category remains unconfirmed retain their source code, such as SS, HT, WT, CH, ST, SE, or SK.
_Avoid_: Turret, Barrel Muzzle, Production End, guessed source-code expansion

**Attachment artist identifier**:
The case-sensitive `ET_...` glTF object name used for authoring an attachment. These identifiers use concise historical editing labels such as Turret, Emitter, Turret Muzzle, Unload Point, Hit Point, Smoke Point, and Production Spot End. They identify physical records but do not replace the confirmed runtime range names or constitute evidence about game behavior.
_Avoid_: Runtime semantic name, physical-number-only helper name

**Canonical authoring identifier**:
A reserved case-sensitive glTF name whose spelling declares an MSH authoring semantic and binds any local typed authoring metadata, including `ET_...` artist identifiers and `EarthTool A` through `EarthTool D` animation names. Renaming changes or removes that semantic target, and duplicate identifiers are conflicts rather than aliases.
_Avoid_: Display name, stable scope identity

**Canonical helper family**:
A set of numbered canonical artist identifiers with one semantic prefix, node shape, and typed-value contract. Renumbering retargets the object within its family, while changing family changes its authored semantic.
_Avoid_: Storage-compatible helper, interchangeable attachment range

**Static light**:
A base-header spot or omni light record numbered 1 through 4 and composed of a position, RGB color, terrain-light amplitude, and shape-specific values. A static light is not itself a coordinate vector, and gaps in light numbers are meaningful.
_Avoid_: Light vector

**Active static light**:
A static light whose corresponding quantized attachment record is runtime-available. Activity is never inferred from the full-precision light position.
_Avoid_: Available light, nonzero-position light

**Light attachment**:
The quantized attachment record corresponding to a static light. It controls light activity but remains independently serialized from the full-precision light record.
_Avoid_: Synced light position

**Static-light color**:
The three RGB floating-point values stored after a static spot or omni light position and consumed by terrain lighting and visible spot rendering.
_Avoid_: Light parameters, unidentified vector

**Spot heading**:
The exact one-byte target heading in a static spot light record, using 256 units per turn, followed by three separately preserved reserved bytes. Radians are a derived view of the exact byte.
_Avoid_: Direction dword

**Spot shape values**:
The values `ApproximateTargetDistance`, `ConeHalfAngleTangent`, `HalfFalloffAngleDistanceProduct`, `VerticalTargetSlope`, and `TerrainLightAmplitude` in a static spot light record.
_Avoid_: Length, Width, U3, Tilt, Ambience

**Effect rectangle**:
A four-lane dynamic-effect rectangle. Dynamic objects carry independent start and end effect rectangles rather than a generic pair of sizes.
_Avoid_: Size, rectangle vector

**Frame period ticks**:
The dynamic object's sprite timing value. Zero selects lifetime-based frame progression; a nonzero value is the simulation-tick period per frame, not a rate.
_Avoid_: Framerate, frames per second

**Ribbon half-width**:
The signed dynamic-effect value used by ribbon effects. Its sign controls strip side, texture orientation, and winding, so it is not an unsigned radius.
_Avoid_: Radius, absolute width

**Additive flag**:
The raw dynamic-object dword whose zero/nonzero semantic view controls additive blending. Accepted nonzero values remain distinct serialized representations.
_Avoid_: Additive boolean

**Terrain-light color**:
The dynamic object's RGB values used when contributing terrain light.
_Avoid_: Light vector, unidentified light parameters

**Visible effect color**:
The dynamic object's RGB values used for visible effect geometry, paired with a separate visible terrain-light gain.
_Avoid_: Dynamic color vector, color parameter

**Alpha timing mode**:
The raw dynamic-object value whose zero/nonzero view selects frame-phase or lifetime-progress alpha interpolation.
_Avoid_: AlphaInt, alpha boolean

**Child translation**:
A dynamic child's start or end translation, interpolated by its parent object's phase. A root object's own pair is not applied to itself.
_Avoid_: Position1, Position2, object position

**TEX resource binding**:
The game-authoritative association between a static render object and an existing TEX resource, represented in MSH by its texture path. A decoded glTF image previews the bound resource but does not replace it or create a new TEX resource.
_Avoid_: Embedded texture, image filename binding

**Canonical TEX resource key**:
The safe relative `Textures\...\.tex` spelling used for a newly authored TEX resource binding. It uses backslash separators, has no traversal segments, and is compared using the game's ASCII case-insensitive identity rules.
_Avoid_: Normalized loaded path, host filesystem path

**Reference-only TEX binding**:
An explicit TEX resource binding whose resource or matching decoded preview is unavailable. It remains writable because MSH stores only the key, but resource compatibility and preview fidelity are unvalidated and diagnosed.
_Avoid_: Missing texture, embedded TEX

**Decoded texture preview**:
The highest-resolution RGBA image projected into an unlit glTF material to represent a TEX resource to an artist. It is packaging data rather than game-authoritative resource identity.
_Avoid_: Source texture, TEX replacement

**EarthTool metadata envelope**:
A versioned document carried atomically on one canonical named glTF owner through stock Blender as a string-valued custom property. It contains only typed authoring inputs that artist-visible glTF cannot express; it never carries exact source representations, source identity, or projection fingerprints.
_Avoid_: Arbitrary extras object, add-on-only metadata

**Metadata applicability**:
The condition under which typed authoring metadata has one uniquely named canonical owner and a supported value. Missing or invalid local metadata uses a safe canonical authoring default with a warning; duplicate canonical names and required semantics with no safe default prevent asset creation.
_Avoid_: Source restoration, fingerprint match

**Reconstructable authoring semantic**:
An MSH meaning fully and unambiguously evidenced by artist-visible glTF state. Retained metadata is not an alternate authority for it.
_Avoid_: Metadata-backed value, preserved semantic

**Projection-bound authoring semantic**:
An MSH meaning intentionally exposed through a native glTF property by EarthTool despite no general format-level equivalence. That property is authoring evidence only within its matching EarthTool native projection, not in metadata-free glTF.
_Avoid_: Core glTF semantic, generic inference

**Typed authoring input**:
An explicit asset-creation value for a semantic that artist-visible glTF does not evidence safely. Retained values are carried on their canonical named owners while resource bindings are supplied through creation options; each is serialized canonically rather than preserving a source representation.
_Avoid_: Exact source representation, inference override

**Metadata conflict**:
A condition where typed authoring metadata cannot bind unambiguously because a canonical authoring identifier is duplicated. Asset creation fails rather than selecting one owner; missing, malformed, unsupported, or out-of-domain local values instead use canonical authoring defaults with warnings.
_Avoid_: First match, identity fallback

**Metadata-backed asset creation**:
Canonical creation of a new immutable mesh asset from artist-visible glTF plus applicable typed authoring inputs on canonical named owners. Every serialized representation is regenerated; no source mesh asset, exact source representation, lineage, scope identity, projection fingerprint, or preservation report participates.
_Avoid_: MSH edit, source restoration

**Metadata-free asset creation**:
Canonical creation of a new immutable mesh asset from artist-visible glTF without an applicable typed authoring input. Each unavailable nonrepresentable meaning receives a safe canonical authoring default and a warning; a required semantic with no safe default prevents asset creation.
_Avoid_: Silent canonical fallback, source restoration

**Attachment artist object**:
The artist-facing interchange representation of one runtime-available attachment record. Its required canonical artist identifier authors the physical attachment number, while its presence and pose expose activity, position, and heading; active cannon and light attachments use their corresponding compound artist objects instead.
_Avoid_: Stable scope identity, physical-number-only object

**Cannon artist object**:
The compound artist-facing representation of one active cannon slot, combining its canonically regenerated quantized attachment and full-precision render position under one canonical identifier.
_Avoid_: Separate cannon attachment, separate render-position helper

**Static-light artist object**:
The artist-facing interchange representation that combines one active spot or omni light with its independently serialized light attachment. Every glTF asset creation canonically regenerates both representations from its pose and typed authoring inputs.
_Avoid_: Light vector, separate light-attachment helper

**Emitter marker ownership**:
The authored relationship in which `ET_Emitter_n` belongs to its nearest source-object ancestor across intervening transform-only groups. Each distinct owned emitter contributes its corresponding `MarkerAttachmentN` role, and one source object may own several such roles.
_Avoid_: Direct glTF parent ownership, root fallback ownership
