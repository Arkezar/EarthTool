# MSH API

EarthTool exposes framed version-1 MSH through immutable assets, bounded
operations, canonical authoring, and an explicit expert construction boundary.
The complete serialized layout is documented in [MSH_FORMAT.md](../MSH_FORMAT.md).

## Asset model

`MeshAsset` is a closed hierarchy with `StaticMeshAsset` and `DynamicMeshAsset`
branches. Use `MeshAsset.Match` when both mesh kinds are accepted. Every ordered
collection is copied at the public boundary, and an accepted asset cannot be
mutated behind an operation.

`StaticMeshAsset.StaticRenderObjectSequence` is the authoritative serialized
order. Its source object tree groups material partitions without becoming a
second ordering authority. `CommonMeshBaseHeader` retains independently
serialized animation declarations, footprint representations, attachment
records, cannon render positions, static lights, and horizontal extents.

`DynamicMeshAsset` retains the root dynamic object and its ordered children.
Each `DynamicObject` owns its common base header and complete dynamic effect
extension. Known-value views name recognized effect and light values while the
numeric serialized representations remain authoritative.

## Bounded operations

`IMshReader`, `IMshWriter`, and `IMshValidator` return operation-scoped results
and diagnostics. `MshOperationProfile` places finite bounds on input, output,
geometry, animation, hierarchy, dynamic objects, strings, trailing bytes, and
retained diagnostics. Structural hazards return no partial asset. Caller-owned
streams remain open, and path-based writes stage, validate, and atomically
replace the destination.

Register these operations with dependency injection:

```csharp
services.AddMshServices();
```

## Authoring

`StaticMeshBuilder` and `DynamicMeshBuilder` create coherent canonical authored
representations. They derive only the fields declared by their authoring
contracts and reject non-finite, out-of-range, cyclic, reused, or over-limit
input.

`MshExpert` accepts exact serialized representations for tooling that must retain
unrecognized values. Expert construction still passes through the bounded
structural decoder and does not weaken the safe operation contract.

```csharp
var build = StaticMeshBuilder.Create(creationGuid)
  .SetRenderObject(vertices, triangles)
  .Build();

build.TryGetValue(out var asset);
```

## glTF interchange

Static assets and dynamic assets composed of `Group`, `Explosion`, `Track`,
`MappedExplosion`, `FlatExplosion`, `Smoke`, `Laser`, `LaserWall`,
`ElectricalCannon`, `Lightning`, `Shockwave`, `Line`, `Sphere`, and `Keelwater`
and resource-backed `ScalableObject` cross the artist boundary through
`EarthTool.GLTF.GltfInterchange`.
glTF edit import owns interchange identity and preservation reporting while MSH
assets remain immutable conversion values.
See the [glTF API](gltf.md) for package, metadata, reconciliation, validation,
plan, and report contracts.

## Migrating identity and editing callers

MSH lineage IDs, render-object IDs, source-object IDs, edit sessions, and MSH
preservation reports have been removed. Keep direct object references while an
immutable asset is in scope, and use `StaticRenderObjectSequence` when serialized
ordering matters. Create changed assets through canonical builders, exact expert
construction, or glTF edit import; glTF results now carry interchange identity
and preservation details.

The mutable generic conversion model was removed as a deliberate major-version
break. See the [COLLADA to glTF migration guide](../migration-collada-to-gltf.md).
