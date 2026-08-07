# Qualification tools

`gltf-validator` is pinned here as the independent package oracle used by the
MSH/glTF contract tests.

## Local pre-publish qualification

The maintainer must run this local Linux x64 gate before pushing a release tag.
It must start from a clean Git worktree and requires the private official MSH
corpus. It resolves and runs every Blender lane, runs the complete reproducible
test and approval contract, packs and consumes the public libraries, publishes
all Linux release applications, and checks the GLTF-present and DAE-absent
package, repository, CLI, documentation, migration, and artifact boundary.

Installations, validator failures, missing corpus data, failed tests, incomplete
test discovery, unapproved snapshots, missing artifacts, and omitted Blender or
corpus qualification results are failures. The version-1 aggregate evidence file
is written only after every required gate passes:

```bash
EARTHTOOL_OFFICIAL_MSH_CORPUS=/absolute/private/corpus \
  npm run qualify:release --prefix test-tools -- \
  --workers 4 \
  --evidence artifacts/release-qualification-v1.json
```

The tag-triggered GitHub release workflow handles public builds and publishing
only. It does not access the private corpus or run corpus qualification.

Use `--blender-cache /absolute/cache` to control where the exact official
Blender archives and installations are retained. The report records the commit,
host, tools, operation profiles, complete gate inventory, Blender results, and
privacy-safe aggregate corpus results. It never records the corpus root, asset
names, relative paths, or TEX/MSH resource keys.

## Blender matrix

`blender-matrix.v1.json` names the required compatibility lanes and their
release series. `blender-qualification.mjs` resolves the highest official patch
in each series, reads the official SHA-256 manifest, and runs an archive only
once when multiple requested lanes resolve to the same checksum.

The qualification is fail-closed: an unavailable archive, checksum mismatch,
missing executable, version mismatch, missing validator, failed or unexecuted
test, incomplete canonical-creation inventory, or unsupported host fails the
command. Every lane checks strict canonical owner names and version-1
`earthtool.msh.authoring` envelopes. Stock-Blender round trips also apply
SharpGLTF strict validation and the pinned Khronos validator before the
source-free canonical creation oracle accepts output.

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
policies, import/export options, canonical-creation scenarios, test counts, and
outcomes.

## Official MSH corpus

The official corpus gate discovers `.msh` assets case-insensitively beneath a
private extraction directory outside the checkout and
emits only versioned aggregate evidence. The corpus, temporary glTF packages,
TRX output, operation messages, paths, names, and TEX/MSH resource keys are never
uploaded or copied into the repository.

The checked-in `official-corpus-profile.v2.json` defines the expected corpus
counts, dynamic effect/domain coverage, `Export All Meshes` batch inventory, and
exact aggregate operation-diagnostic histogram. The profile has no
per-asset exceptions: a read, validation, write, semantic-equivalence,
idempotence, package, or canonical-creation failure blocks the entire gate. Every
accepted static and dynamic asset receives public API and packaged CLI GLB and
separate-glTF stages. The gate checks canonical named-owner envelopes, explicit
TEX/MSH binding coverage, package manifests, sidecars, reports, and `ETG1030`
source-loss warnings. Diagnostic drift also blocks the gate. Khronos errors and
warnings fail; informational and hint findings are retained in the evidence
histogram.

The path-independent corpus fingerprint hashes the multiset of each file's byte
length and SHA-256 digest. Neither file names nor relative paths enter the
fingerprint preimage.

The recursive corpus contains 1,151 accepted assets. The `Export All Meshes`
launch profile intentionally targets the top-level `meshes/*.msh` inventory:
1,149 assets (955 static and all 194 dynamic). The other two static assets still
receive every per-asset oracle. The private corpus currently contains 14 of the
15 recognized effect values and only positive ribbon widths. Release discovery
therefore also requires named synthetic gates for the complete 15-effect recipe
set, negative-width orientation/winding, resource limits, and both package forms;
those tests must pass in the reproducible full-suite gate.

Run the gate after installing the pinned validator and building the test
assembly:

```bash
npm ci --prefix test-tools
dotnet build EarthTool.sln --configuration Release
EARTHTOOL_OFFICIAL_MSH_CORPUS=/absolute/private/corpus \
  npm run qualify:corpus --prefix test-tools -- \
  --workers 4 \
  --timings true \
  --evidence artifacts/official-msh-corpus-qualification.json
```

The worker count defaults to half of the logical processors visible to .NET,
with a minimum of one. `--timings true` prints privacy-safe aggregate stage
timings from a temporary event without adding nondeterministic fields to the
qualification evidence. See the
[performance protocol](../docs/official-msh-qualification-performance.md) for
the three-run before/after procedure.

The version-2 evidence records the corpus fingerprint and counts, exact dynamic
coverage, the 1,149-file launch-profile batch result, input and generated byte
totals, every oracle's pass/fail totals, operation profiles, diagnostic and
validator histograms, exact tool versions, and the tested commit.
The driver suppresses child-process output and reduces infrastructure failures
to closed categories so private values cannot enter release logs or evidence.
