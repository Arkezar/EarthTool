# Revised MSH Public API Prototype

> THROWAWAY PROTOTYPE. This project is not production code and must not be merged.

This compile-checked sketch answers one question: what cohesive breaking public API can expose exact MSH representations, semantic views, diagnostics, preservation state, canonical authoring, static geometry, and dynamic effects without implementation casts or glTF/SharpGLTF leakage?

Run it with:

```bash
dotnet run --project prototypes/EarthTool.MSH.ApiPrototype
```

The proposed shape is demonstrated in `UsageScenarios.cs`. `PublicApi.cs` contains only enough stub implementation to compile those call sites.

## Proposed shape

- `MeshAsset` is a closed root union with public `StaticMeshAsset` and `DynamicMeshAsset` branches. `Match` supports branch handling without casting; branch-specific APIs accept the typed asset directly.
- Public domain types are sealed immutable records. Collections use immutable snapshots. Binary serialization is not implemented by model objects.
- Raw authorities and semantic views live together in domain-specific value types. `Float32Bits` retains exact NaN payloads. Unknown numeric values remain exact and expose nullable known-value views.
- `MshReader` and `MshWriter` own binary I/O. Expected content failures use typed result objects and stable structured diagnostics; I/O and cancellation remain exceptions.
- `StaticMeshBuilder` and `DynamicMeshBuilder` produce canonical assets from semantic inputs. `EarthTool.MSH.Expert` accepts complete raw inputs for deliberate bounded anomalies. Both paths produce the same immutable model.
- `StaticMeshEditSession` creates a new asset while preserving untouched loaded representations. Commit returns a `PreservationReport` that names retained, regenerated, and invalidated paths.
- The static linear record sequence is authoritative. `SourceObjectTree` contains asset-scoped record IDs rather than copied records.
- No type in this project references glTF, Blender, COLLADA, or SharpGLTF. A future `EarthTool.GLTF` adapter consumes `StaticMeshAsset`.

## Accepted decisions

- Use the intentionally closed `MeshAsset` hierarchy with public `StaticMeshAsset` and `DynamicMeshAsset` branches. Generic callers use `Match`; branch-specific adapters and operations accept the typed branch directly.
- Edit loaded assets through a one-shot scoped edit session. The session accumulates related changes, then `Commit` returns one new immutable snapshot, diagnostics, and a report of retained, regenerated, invalidated, and canonicalized field paths. It is distinct from canonical authoring.
- Scope static render-object and source-object IDs to an explicit asset lineage. Retained objects keep IDs across edit-session commits; new objects receive new IDs. IDs are not serialized by MSH, so an unrelated reread starts a new lineage unless interchange metadata restores it.
- Keep diagnostics on operation results rather than assets. Read, build, edit, validate, and write results each describe that operation; callers use `MshValidator` to refresh diagnostics after transformations instead of carrying stale warnings on domain objects.
- Expose ordered immutable collections as `ImmutableArray<T>`. Constructors and builders own snapshots rather than aliasing caller arrays or lists.
- Return expected canonical-authoring failures as stable structured build diagnostics with no asset. Programmer contract violations remain exceptions.
- Isolate complete raw authoring under `EarthTool.MSH.Expert`. Ordinary APIs expose immutable data and canonical semantic builders; expert inputs can express bounded compatibility anomalies but cannot bypass structural safety.

## Deliberate omissions

- Method bodies, validation algorithms, and complete field inventories.
- glTF conversion APIs; those belong to `EarthTool.GLTF`.
- Compatibility aliases for the retiring API.
- Dynamic-effect glTF transport.
