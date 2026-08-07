# Project Structure

```text
EarthTool.Common/          Shared contracts and infrastructure
EarthTool.MSH/             Immutable MSH domain and bounded binary operations
  Assets/                  Closed static and dynamic asset hierarchy
  Authoring/               Canonical builders and dynamic effect recipes
  Expert/                  Exact serialized construction boundary
  Internal/                Binary decoding, serialization, and validation
  Operations/              Public operation contracts and profiles
  Services/                Reader, writer, and validator implementations
EarthTool.GLTF/            GLB and separate glTF interchange facade
  Internal/                Adapter, metadata, package, and filesystem details
EarthTool.WD/              WD archive reading, writing, and manipulation
EarthTool.TEX/             TEX texture decoding and preview support
EarthTool.PAR/             PAR parameter parsing and serialization
EarthTool.CLI/             Published command-line application
  Commands/MSH/            Export and canonical import commands
EarthTool.Common.GUI/      Shared Avalonia UI services and controls
EarthTool.WD.GUI/          WD archive desktop application
EarthTool.TEX.GUI/         TEX texture desktop application
EarthTool.PAR.GUI/         PAR parameter desktop application
EarthTool.Consumer.Tests/  Public consumer compile gate
EarthTool.MSH.Tests/       MSH, glTF, approval, and validator contract tests
EarthTool.CLI.Tests/       CLI behavior and published-process acceptance tests
EarthTool.WD.Tests/        WD behavior and integration tests
EarthTool.TEX.Tests/       TEX behavior tests
EarthTool.PAR.Tests/       PAR behavior tests
test-tools/                Pinned independent glTF validator
docs/                      User, API, format, architecture, and migration docs
```

## Dependency direction

- `EarthTool.MSH` depends on `EarthTool.Common`.
- `EarthTool.GLTF` depends on `EarthTool.MSH`, `EarthTool.TEX`, and
  `EarthTool.Common`.
- `EarthTool.CLI` composes the format modules and does not expose adapter-library
  types.
- Tests consume public seams unless a focused internal fixture is required for
  deterministic failure injection.

## Public packages

`EarthTool.MSH` provides the binary and authoring API. `EarthTool.GLTF` provides
the artist-interchange API and registers its facade through `AddGltfServices`.
The CLI release references both projects and ships one coherent glTF-capable
artifact.

The removed COLLADA assembly and mutable generic MSH model are not compatibility
dependencies. Migration is documented in
[migration-collada-to-gltf.md](migration-collada-to-gltf.md).
