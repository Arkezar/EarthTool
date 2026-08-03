# Official MSH Qualification Performance

The official corpus gate processes assets with a bounded worker queue. The
default worker count is half of the logical processors reported to .NET, with a
minimum of one. Pass `--workers` to use a fixed positive count:

```bash
EARTHTOOL_OFFICIAL_MSH_CORPUS=/absolute/private/corpus \
  npm run qualify:corpus --prefix test-tools -- \
  --workers 4 \
  --evidence artifacts/official-msh-corpus-qualification.json
```

Each worker owns its MSH and glTF services and lazily starts one persistent
Khronos validator process. Assets are assigned dynamically. Aggregate results
are reduced in corpus order after all workers finish, so scheduling does not
change qualification evidence.

## Stage Profiling

Profiling is explicitly opt-in and does not change the version-1 qualification
evidence. `--timings true` writes a temporary aggregate event, prints it after
the gate passes, and removes it with the other private intermediate files:

```bash
EARTHTOOL_OFFICIAL_MSH_CORPUS=/absolute/private/corpus \
  npm run qualify:corpus --prefix test-tools -- \
  --workers 4 \
  --timings true \
  --evidence artifacts/official-msh-corpus-qualification.json
```

The first line reports end-to-end wall time. Stage lines report invocation
count, summed elapsed work, and average elapsed work. Summed stage times overlap
when workers run concurrently and therefore can exceed wall time. Direct and
published CLI export timings include package creation and writes performed by
those operations. Separate I/O stages cover harness source/output reads, direct
GLB writes, package inventory, directory creation, and cleanup. SharpGLTF
validation, Khronos worker startup, Khronos validation, and imports have their
own stages.

The profile contains only fixed stage names, aggregate counts, worker count,
and durations. It contains no asset names, paths, texture keys, or package
identities.

## Before/After Protocol

Run the baseline and optimized revisions on the same controlled machine with
the same corpus fingerprint, Release build, worker configuration, .NET SDK,
Node.js, SharpGLTF, and Khronos validator versions. Build before timing, then
collect three measured runs without a discarded warmup. Record every duration
and compare medians.

Baseline command:

```bash
dotnet build EarthTool.sln --configuration Release
for run in 1 2 3; do
  /usr/bin/time -f "run=$run elapsed=%e" \
    node test-tools/official-corpus-qualification.mjs \
    --evidence artifacts/official-msh-corpus-qualification.json
done
```

Optimized command:

```bash
dotnet build EarthTool.sln --configuration Release
for run in 1 2 3; do
  /usr/bin/time -f "run=$run elapsed=%e" \
    node test-tools/official-corpus-qualification.mjs \
    --workers "$(( $(nproc) / 2 > 0 ? $(nproc) / 2 : 1 ))" \
    --timings true \
    --evidence artifacts/official-msh-corpus-qualification.json
done
```

Record the controlled-run result before closing issue 161:

| Measurement | Baseline | Optimized |
| --- | ---: | ---: |
| Commit | `89948f4` | pending |
| Corpus fingerprint | `2b3a67e46aec82e5851effeba748cb262dd907009dba60300195e3bf5360ce7f` | same |
| Worker count | 1 | CPU / 2 |
| Run 1 | pending | pending |
| Run 2 | pending | pending |
| Run 3 | pending | pending |
| Median | pending | pending |
| Median reduction | - | must be at least 50% |

If the optimized median reduction is below 50%, batch GLB and separate glTF
exports through the existing multi-input `msh export` command, leave edit
imports unchanged, and repeat this protocol.
