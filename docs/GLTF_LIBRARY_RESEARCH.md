# glTF .NET foundation research

## Decision

Use `SharpGLTF.Core` 1.0.6 as the initial implementation dependency for the
future `EarthTool.GLTF` module. Do not take a production dependency on
`SharpGLTF.Toolkit` unless a later spike identifies a helper that cannot alter
MSH topology or identity.

Keep every SharpGLTF type internal to `EarthTool.GLTF`. Public conversion APIs
should accept and return EarthTool domain models and operation results, not
`ModelRoot`, `Accessor`, builders, or other third-party types.

The historical `gltf` branch validates the package family, basic scene mapping,
and DI seam. It should not be merged or used as the production implementation.

## Historical branch verification

The local `gltf` branch contains one prototype commit,
[`5da6caf`](https://github.com/Arkezar/EarthTool/tree/5da6caf4e6fa9d108f7d39f226cbdd1379d156cc).

It directly references:

- `SharpGLTF.Core` 1.0.5
- `SharpGLTF.Toolkit` 1.0.5

The package declarations are in the branch's
[`Directory.Packages.props`](https://github.com/Arkezar/EarthTool/blob/5da6caf4e6fa9d108f7d39f226cbdd1379d156cc/Directory.Packages.props),
and both packages are referenced by
[`EarthTool.glTF.csproj`](https://github.com/Arkezar/EarthTool/blob/5da6caf4e6fa9d108f7d39f226cbdd1379d156cc/EarthTool.glTF/EarthTool.glTF.csproj).

The prototype uses Toolkit scene, geometry, material, vertex, and animation
builders together with the Core `Schema2.ModelRoot` API. It demonstrates:

- a source-object hierarchy exported as glTF nodes;
- static render-object material partitions exported as mesh primitives;
- positions, normals, UVs, triangles, and transform animation channels;
- DI registration through `IReader<IMesh>` and `IWriter<IMesh>`;
- text `.gltf` output through `ModelRoot.SaveGLTF`.

The implementation is incomplete:

- [`GltfReader`](https://github.com/Arkezar/EarthTool/blob/5da6caf4e6fa9d108f7d39f226cbdd1379d156cc/EarthTool.glTF/Services/GltfReader.cs)
  throws `NotImplementedException`.
- [`GltfWriter`](https://github.com/Arkezar/EarthTool/blob/5da6caf4e6fa9d108f7d39f226cbdd1379d156cc/EarthTool.glTF/Services/GltfWriter.cs)
  emits only separate `.gltf`, despite adding both GLTF and GLB file types.
- [`GltfModelFactory`](https://github.com/Arkezar/EarthTool/blob/5da6caf4e6fa9d108f7d39f226cbdd1379d156cc/EarthTool.glTF/Factories/GltfModelFactory.cs)
  uses a magenta material, a 23.976 FPS constant, and a correction root instead
  of the decided texture, 24 FPS, and baked-axis policies.
- There is no EarthTool metadata, extras, attachment/light mapping, texture
  resolution, GLB packaging, input validation, diagnostics, or test project.
- The factory includes unrelated skinned-tentacle sample code.

Most importantly, the Toolkit geometry builder is incompatible with exact MSH
projection. `PrimitiveBuilder.UseVertex` reuses equal vertices, and triangle
creation rejects degenerate triangles. See
[`PrimitiveBuilder.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Toolkit/Geometry/PrimitiveBuilder.cs#L186-L199)
and
[`PrimitiveBuilder.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Toolkit/Geometry/PrimitiveBuilder.cs#L746-L761).
The MSH corpus contains deliberate duplicate render vertices and degenerate
triangles, so production export must construct accessors and indices directly.

## SharpGLTF assessment

SharpGLTF is an active MIT-licensed reader/writer. Version 1.0.6 was released on
2025-12-30, and the repository received commits in 2026. See the
[`v1.0.6` release](https://github.com/vpenades/SharpGLTF/releases/tag/v1.0.6),
the
[`SharpGLTF` repository](https://github.com/vpenades/SharpGLTF), and the
[`SharpGLTF.Core` NuGet package](https://www.nuget.org/packages/SharpGLTF.Core/1.0.6).

Both Core and Toolkit target `netstandard2.0`, `netstandard2.1`, `net6.0`,
`net8.0`, and `net10.0`, so Core is compatible with EarthTool's current
`netstandard2.1` format libraries. The target frameworks are declared in
[`SharpGLTF.Core.csproj`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/SharpGLTF.Core.csproj#L3-L8).

### Required capabilities

| Requirement | Evidence | Assessment |
|---|---|---|
| GLB and separate glTF | `ModelRoot.SaveGLB`, `SaveGLTF`, and extension-selecting `Save` are implemented in [`Serialization.WriteSettings.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/Serialization.WriteSettings.cs#L149-L224). | Meets |
| Resource packaging | `WriteSettings` controls image embedding, satellite files, buffer merging, and write callbacks in [`Serialization.WriteSettings.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/Serialization.WriteSettings.cs#L12-L127). | Meets |
| Low-level accessors | Core creates accessors/buffer views, assigns exact formats, and exposes typed array views in [`gltf.Accessors.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/gltf.Accessors.cs). | Meets |
| Arbitrary extras | Every schema object exposes `System.Text.Json.Nodes.JsonNode Extras`; values are deep-cloned, parsed, and serialized in [`gltf.ExtraProperties.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/gltf.ExtraProperties.cs#L14-L66). | Meets |
| Unknown extension preservation | Unregistered extensions deserialize into `UnknownNode` and serialize under their original names in [`gltf.ExtraProperties.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/gltf.ExtraProperties.cs#L246-L319). | Meets |
| Typed custom extensions | `ExtensionsFactory.RegisterExtension` registers parent-specific extension factories in [`gltf.ExtensionsFactory.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/gltf.ExtensionsFactory.cs#L81-L110). | Meets, but registration is process-global |
| Animations | Core supports exact animation input/output accessors and TRS samplers; Toolkit adds curve builders. Core implementations are in [`gltf.AnimationSampler.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/gltf.AnimationSampler.cs). | Meets |
| Unlit materials and punctual lights | Core registers `KHR_materials_unlit` and `KHR_lights_punctual` in [`gltf.ExtensionsFactory.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/gltf.ExtensionsFactory.cs#L34-L70). | Meets |
| Validation | Loading and writing default to strict validation; `ModelRoot.Validate` is available in [`Serialization.ReadSettings.cs`](https://github.com/vpenades/SharpGLTF/blob/0a00735b32a6f3be262037c9f82a63011ca840bf/src/SharpGLTF.Core/Schema2/Serialization.ReadSettings.cs#L31-L100). | Meets for in-process schema/content checks; still run Khronos glTF Validator in acceptance tests |
| Module isolation | Core types are ordinary implementation objects with no framework registration requirement beyond optional custom extensions. | Meets by architectural wrapper; inference from API shape |

The glTF specification permits both separate `.gltf` resources and GLB, defines
accessors as binary-data descriptors, and defines `extras` and extensions on
schema objects. See the official
[`glTF 2.0 specification`](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html).

## Alternative: Khronos glTF2Loader

The strongest lower-level alternative is the Khronos reference
[`glTF-CSharp-Loader`](https://github.com/KhronosGroup/glTF-CSharp-Loader),
published as
[`glTF2Loader` 2.0.0](https://www.nuget.org/packages/glTF2Loader/2.0.0).
It is active, BSD-2-Clause licensed, and targets `netstandard2.0` and `net8.0`.
Those facts are declared in
[`glTFLoader.csproj`](https://github.com/KhronosGroup/glTF-CSharp-Loader/blob/5e41d732a7b9593857572fa4c9458b8733437e20/glTFLoader/glTFLoader.csproj).

Despite its name, it can serialize text glTF and GLB through `SaveModel`,
`SaveBinaryModel`, and `SaveBinaryModelPacked`; see
[`Interface.cs`](https://github.com/KhronosGroup/glTF-CSharp-Loader/blob/5e41d732a7b9593857572fa4c9458b8733437e20/glTFLoader/Interface.cs#L317-L468).
It provides generated low-level schema types and raw extension dictionaries.

It is not the recommended foundation because EarthTool would need to build most
of the missing infrastructure itself:

- no built-in glTF validation API was found in its public interface;
- buffer/accessor construction and resource packing are manual;
- custom extensions are untyped `Dictionary<string, object>` values;
- the generated `Extras` type is empty, rather than an arbitrary JSON container,
  in [`Extras.cs`](https://github.com/KhronosGroup/glTF-CSharp-Loader/blob/5e41d732a7b9593857572fa4c9458b8733437e20/glTFLoader/Schema/Extras.cs);
- there are no scene, geometry, material, or animation authoring helpers.

It remains useful as a reference implementation, but choosing it would amount
to maintaining an EarthTool-specific glTF SDK or fork to recover capabilities
already present in SharpGLTF.Core.

## Package and architecture recommendation

1. Create `EarthTool.GLTF` and add `SharpGLTF.Core` 1.0.6 through the repository's
   central package-management workflow.
2. Do not initially add `SharpGLTF.Toolkit` or `SharpGLTF.Runtime` directly.
3. Build geometry, indices, custom attributes, animation accessors, extras, and
   primitive boundaries through `SharpGLTF.Schema2` so EarthTool controls
   ordering and identity.
4. Use SharpGLTF strict validation during load/write and the Khronos glTF
   Validator as an independent acceptance-test oracle.
5. Keep EarthTool metadata in `extras` for stock Blender survival unless the
   metadata-contract ticket proves a typed extension is required. SharpGLTF can
   support either choice.
6. Isolate package APIs behind EarthTool reader/writer and operation-result
   contracts. This limits dependency churn and permits replacement if a future
   SharpGLTF release changes append-only schema APIs.

## Risks and required spikes

- Verify Blender 4.5 import/export survival of `JsonNode` extras and custom
  underscore-prefixed accessors in the dedicated Blender research ticket.
- Verify that Core strict validation accepts every generated EarthTool document
  and does not silently repair identity-sensitive data.
- Pin package version 1.0.6 initially; review newer versions deliberately rather
  than inheriting behavior changes during implementation.
- Avoid Toolkit mesh builders for source topology. If Toolkit is later used for
  materials or convenience transforms, test that the chosen path cannot merge
  vertices, discard degenerate triangles, reorder primitives, or hide generated
  accessors.
- Treat the historical branch's 23.976 FPS, correction-root transform, magenta
  material, naming, and service API as discarded prototype decisions.
