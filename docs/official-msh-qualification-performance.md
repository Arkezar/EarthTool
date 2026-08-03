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
the same corpus fingerprint, Release build, .NET SDK, Node.js, SharpGLTF, and
Khronos validator versions. Use one fixed worker count for every optimized run.
Build before timing, then collect three measured runs without a discarded
warmup. Record every duration and compare medians.

Baseline command:

```bash
dotnet build EarthTool.sln --configuration Release
for run in 1 2 3; do
  time -p node test-tools/official-corpus-qualification.mjs \
    --evidence artifacts/official-msh-corpus-qualification.json
done
```

Optimized command:

```bash
dotnet build EarthTool.sln --configuration Release
for run in 1 2 3; do
  time -p node test-tools/official-corpus-qualification.mjs \
    --workers "$(( $(nproc) / 2 > 0 ? $(nproc) / 2 : 1 ))" \
    --timings true \
    --evidence artifacts/official-msh-corpus-qualification.json
done
```

## Measured Result

Measured on 2026-08-03 using an AMD Ryzen 9 9950X3D with 16 cores and
32 logical processors, Linux 7.1.5-arch1-2, .NET SDK 10.0.302, Node.js
v25.9.0, SharpGLTF 1.0.6, and Khronos glTF Validator 2.0.0-dev.3.10.
Both detached worktrees were built in Release before timing. The optimized
runs used a fixed 16 workers.

| Measurement | Baseline | Optimized |
| --- | ---: | ---: |
| Commit | `89948f4` | `416ca9e` |
| Corpus fingerprint | `2b3a67e46aec82e5851effeba748cb262dd907009dba60300195e3bf5360ce7f` | same |
| Worker count | 1 | 16 |
| Run 1 | 1193.05 s | 93.50 s |
| Run 2 | 1200.46 s | 93.92 s |
| Run 3 | 1189.04 s | 93.63 s |
| Median | 1193.05 s | 93.63 s |
| Median reduction | - | **92.15%** |
| Speedup | 1.00x | **12.74x** |

Every run passed with 1,151 assets, 22,981 successful oracle operations,
3,828 Khronos validations, and zero operation failures, aggregate failures,
validator errors, or validator warnings. Evidence was byte-identical across
all three runs of each revision. The baseline evidence SHA-256 was
`1bf6370099ed389a93867b81a1b60c9839f71d7d7fc62554230a0d7da1aaeaf2`;
the optimized evidence SHA-256 was
`745eab2a5e3193076b42d8f3b016392218a6fb9c5b874479f65c7e2cb3d12f0a`.

Median optimized aggregate worker time identified published CLI process work
as the dominant cost:

| Stage | Total worker time | Average operation |
| --- | ---: | ---: |
| glTF CLI export | 341.424 s | 356.8 ms |
| GLB CLI export | 313.552 s | 327.6 ms |
| glTF CLI unchanged import | 307.981 s | 321.8 ms |
| GLB CLI unchanged import | 304.326 s | 318.0 ms |
| Direct GLB export | 87.849 s | 91.8 ms |
| Direct glTF export | 82.248 s | 85.9 ms |
| All Khronos validations | 8.153 s | 2.1 ms |
| All SharpGLTF validations | 7.955 s | 2.1 ms |
| Explicit harness/package I/O | 0.439 s | - |
| Khronos worker startup | 0.112 s | 7.0 ms |

Stage totals sum overlapping work from 16 workers. Direct and CLI export
stages include package writes performed inside those operations. The result
exceeds the required 50% median reduction without export batching, so the
fallback is not needed.

If the optimized median reduction is below 50%, batch GLB and separate glTF
exports through the existing multi-input `msh export` command, leave edit
imports unchanged, and repeat this protocol.
