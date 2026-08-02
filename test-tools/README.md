# Qualification tools

`gltf-validator` is pinned here as the independent package oracle used by the
MSH/glTF contract tests.

## Blender matrix

`blender-matrix.v1.json` names the required compatibility lanes and their
release series. `blender-qualification.mjs` resolves the highest official patch
in each series, reads the official SHA-256 manifest, and runs an archive only
once when multiple requested lanes resolve to the same checksum.

The qualification is fail-closed: an unavailable archive, checksum mismatch,
missing executable, version mismatch, missing validator, failed or unexecuted
test, incomplete ownership inventory, unexpected preservation effect, or
unsupported host fails the command. Every lane runs the stock-Blender ownership
contract while the ordinary Linux and Windows jobs run the remaining public
glTF facade contract. The stock-Blender round trips additionally apply
SharpGLTF strict validation and the pinned Khronos validator to each Blender
output before the ownership-aware import oracle accepts it.

Resolve the current Linux matrix without downloading Blender:

```bash
node test-tools/blender-qualification.mjs --resolve-only true --platform linux-x64
```

Run the gate after building `EarthTool.MSH.Tests` and installing the pinned Node
dependencies:

```bash
npm ci --prefix test-tools
dotnet build EarthTool.MSH.Tests/EarthTool.MSH.Tests.csproj --configuration Release
node test-tools/blender-qualification.mjs \
  --platform linux-x64 \
  --evidence artifacts/blender-qualification-linux-x64.json
```

The version-1 evidence records requested and deduplicated lanes, official source
and checksum URLs, archive checksums, exact Blender versions and build hashes,
the stock `io_scene_gltf2` version, host OS, EarthTool commit, validator
policies, import/export options, ownership domains, test counts, and outcomes.
