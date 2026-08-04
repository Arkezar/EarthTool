import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { corpusInterchangeStages } from "./official-corpus-contract.mjs";

import {
  buildEvidence,
  requiredGates,
  validateArtifactInventory,
  validateDynamicQualificationTests,
  validateGateResults,
  validateRepositoryState,
  validateReleaseBoundary
} from "./release-qualification.mjs";

const commit = "a".repeat(40);

function passingGates() {
  return requiredGates.map(name => ({ name, outcome: "passed" }));
}

test("aggregate evidence names every completed gate, tool, profile, and result", () => {
  const gates = passingGates();
  const blender = {
    format: "earthtool.blender-qualification-evidence",
    version: 1,
    outcome: "passed",
    platform: "linux-x64",
    earthToolCommit: commit,
    requestedLanes: [
      { name: "latest-4.5-lts", series: "4.5" },
      { name: "latest-stable", series: "5.2" },
      { name: "latest-lts", series: "5.2" }
    ],
    builds: [
      {
        version: "4.5.12",
        buildHash: "abcdef0",
        addonVersion: "4.5.12",
        requestedLanes: ["latest-4.5-lts"],
        outcome: "passed"
      },
      {
        version: "5.2.0",
        buildHash: "abcdef1",
        addonVersion: "5.2.17",
        requestedLanes: ["latest-stable", "latest-lts"],
        outcome: "passed"
      }
    ]
  };
  const corpus = {
    format: "earthtool.official-msh-qualification-evidence",
    version: 2,
    outcome: "passed",
    platform: "linux-x64",
    earthToolCommit: commit,
    profile: { format: "earthtool.official-msh-corpus-profile", version: 2 },
    tools: { sharpGltf: "1.0.6", khronosValidator: "2.0.0-dev.3.10" },
    corpus: { fingerprint: "b".repeat(64), assets: 1151, staticAssets: 957, dynamicAssets: 194 },
    dynamicCoverage: {
      assets: 194,
      objects: 445,
      unknownEffectObjects: 0,
      effectTypes: [{ effectType: "Group", count: 56 }]
    },
    operations: corpusInterchangeStages.map(stage => ({
      stage,
      attempted: 1151,
      passed: 1151,
      failed: 0
    })),
    diagnostics: [],
    validators: { khronos: { packages: 4604, errors: 0, warnings: 0 } },
    exportAllMeshes: {
      assets: 1149,
      staticAssets: 955,
      dynamicAssets: 194,
      succeeded: 1149,
      failed: 0,
      cancelled: 0,
      unsupportedDomainDiagnostics: 0,
      outputFiles: 1149
    },
    passFail: { passedOperations: 4604, failedOperations: 0, aggregateFailures: 0 }
  };

  const evidence = buildEvidence({
    commit,
    platform: "linux-x64",
    os: "Linux 6.8",
    tools: { node: "v24.0.0", npm: "11.0.0", dotnet: "8.0.407", git: "2.45.0" },
    gates,
    blender,
    corpus
  });

  assert.equal(evidence.format, "earthtool.release-qualification-evidence");
  assert.equal(evidence.version, 1);
  assert.equal(evidence.outcome, "passed");
  assert.deepEqual(evidence.gates.map(gate => gate.name), requiredGates);
  assert.deepEqual(evidence.profiles.blender, blender.requestedLanes);
  assert.deepEqual(evidence.profiles.corpus, corpus.profile);
  assert.equal(evidence.results.blender, blender);
  assert.equal(evidence.results.officialCorpus, corpus);
  assert.ok(!JSON.stringify(evidence).includes("corpusRoot"));

  const incompleteBlender = structuredClone(blender);
  incompleteBlender.builds.pop();
  assert.throws(() => buildEvidence({
    commit,
    platform: "linux-x64",
    os: "Linux 6.8",
    tools: { node: "v24.0.0", npm: "11.0.0", dotnet: "8.0.407", git: "2.45.0" },
    gates,
    blender: incompleteBlender,
    corpus
  }), /Blender qualification evidence/i);

  const missingCorpusPlatform = structuredClone(corpus);
  delete missingCorpusPlatform.platform;
  assert.throws(() => buildEvidence({
    commit,
    platform: "linux-x64",
    os: "Linux 6.8",
    tools: { node: "v24.0.0", npm: "11.0.0", dotnet: "8.0.407", git: "2.45.0" },
    gates,
    blender,
    corpus: missingCorpusPlatform
  }), /corpus evidence/i);

  const staticOnlyCorpus = structuredClone(corpus);
  staticOnlyCorpus.operations.forEach(operation => {
    operation.attempted = 957;
    operation.passed = 957;
  });
  staticOnlyCorpus.validators.khronos.packages = 3828;
  assert.throws(() => buildEvidence({
    commit,
    platform: "linux-x64",
    os: "Linux 6.8",
    tools: { node: "v24.0.0", npm: "11.0.0", dotnet: "8.0.407", git: "2.45.0" },
    gates,
    blender,
    corpus: staticOnlyCorpus
  }), /corpus evidence/i);
});

test("aggregate validation rejects failed, skipped, missing, and foreign evidence", () => {
  assert.doesNotThrow(() => validateGateResults(passingGates(), commit));

  const failed = passingGates();
  failed[0] = { name: failed[0].name, outcome: "failed" };
  assert.throws(() => validateGateResults(failed, commit), /did not pass/i);

  const skipped = passingGates();
  skipped[0] = { name: skipped[0].name, outcome: "skipped" };
  assert.throws(() => validateGateResults(skipped, commit), /did not pass/i);

  assert.throws(() => validateGateResults(passingGates().slice(1), commit), /inventory/i);
  assert.throws(() => validateGateResults(passingGates(), "not-a-commit"), /commit/i);
});

test("release discovery requires named dynamic family, sign, limit, and batch gates", () => {
  const msh = [
    "EveryKnownDynamicEffectHasAnExplicitCanonicalRecipe",
    "SpriteEffectsExportThroughThePublicGlbSeam",
    "RibbonEffectsExportThroughThePublicGlbSeam",
    "AttachedAndProceduralEffectsExportWithExplicitPreviewContexts",
    "RibbonPreviewRetainsHalfWidthSignTextureSideAndWinding",
    "UnsupportedEffectAndObjectLimitFailWithoutOutput",
    "ScalableObjectUsesAReferencedStaticMeshPreviewAndRoundTripsExactly",
    "SeparateGltfRoundTripsTheExactDynamicMsh"
  ].join("\n");
  const cli = "BatchExportsAllSupportedDynamicEffectsAsSeparateGltf";

  assert.doesNotThrow(() => validateDynamicQualificationTests(msh, cli));
  assert.throws(
    () => validateDynamicQualificationTests(msh.replace("RibbonPreviewRetainsHalfWidthSignTextureSideAndWinding", ""), cli),
    /dynamic qualification test inventory/i);
});

test("release qualification requires a clean source tree", () => {
  assert.doesNotThrow(() => validateRepositoryState(""));
  assert.throws(
    () => validateRepositoryState(" M test-tools/release-qualification.mjs\n"),
    /clean Git worktree/i);
});

test("release boundary requires GLTF and migration links and forbids DAE", () => {
  const boundary = {
    solution: "Project(\"x\") = \"EarthTool.GLTF\", \"EarthTool.GLTF/EarthTool.GLTF.csproj\"",
    cliProject: "<ProjectReference Include=\"..\\EarthTool.GLTF\\EarthTool.GLTF.csproj\" />",
    migration: [
      "v0.4.4",
      "https://github.com/Arkezar/EarthTool/releases/tag/v0.4.4",
      "https://github.com/Arkezar/EarthTool/tree/v0.4.4",
      "https://github.com/Arkezar/EarthTool/issues/133"
    ].join("\n"),
    documentationIndex: "[COLLADA to glTF migration](migration-collada-to-gltf.md)",
    activeDocumentation: "EarthTool.GLTF msh export msh import edit msh import new all 15 recognized dynamic effect types",
    paths: new Set([
      "EarthTool.GLTF/EarthTool.GLTF.csproj",
      "docs/api/gltf.md",
      "docs/migration-collada-to-gltf.md"
    ])
  };

  assert.doesNotThrow(() => validateReleaseBoundary(boundary));

  boundary.paths.add("EarthTool.DAE/EarthTool.DAE.csproj");
  assert.throws(() => validateReleaseBoundary(boundary), /DAE/i);
});

test("release boundary rejects active documentation that advertises DAE", () => {
  const boundary = {
    solution: "EarthTool.GLTF",
    cliProject: "EarthTool.GLTF",
    migration: [
      "v0.4.4",
      "https://github.com/Arkezar/EarthTool/releases/tag/v0.4.4",
      "https://github.com/Arkezar/EarthTool/tree/v0.4.4",
      "https://github.com/Arkezar/EarthTool/issues/133"
    ].join("\n"),
    documentationIndex: "migration-collada-to-gltf.md",
    activeDocumentation: "EarthTool.GLTF msh export msh import edit msh import new all 15 recognized dynamic effect types supports COLLADA import",
    paths: new Set([
      "EarthTool.GLTF/EarthTool.GLTF.csproj",
      "docs/api/gltf.md",
      "docs/migration-collada-to-gltf.md"
    ])
  };

  assert.throws(() => validateReleaseBoundary(boundary), /Active documentation/i);
});

test("artifact inventory is exactly the Linux release set", () => {
  const artifacts = [
    "EarthTool.CLI-Linux-x64.tar.gz",
    "EarthTool.WD.GUI-Linux-x64.tar.gz",
    "EarthTool.PAR.GUI-Linux-x64.tar.gz",
    "EarthTool.TEX.GUI-Linux-x64.tar.gz"
  ];

  assert.deepEqual(validateArtifactInventory(artifacts), artifacts);
  assert.throws(
    () => validateArtifactInventory([...artifacts, "EarthTool.CLI-macOS-x64.tar.gz"]),
    /artifact inventory/i);
  assert.throws(
    () => validateArtifactInventory(artifacts.slice(1)),
    /artifact inventory/i);
});

test("release workflow publishes Windows and Linux without a macOS lane", async () => {
  const workflow = await readFile(
    new URL("../.github/workflows/release.yml", import.meta.url),
    "utf8");

  assert.match(workflow, /os: \[windows-latest, ubuntu-latest\]/);
  assert.doesNotMatch(workflow, /macos-latest|osx-x64|macOS-x64/);
  assert.doesNotMatch(workflow, /^  blender-qualification:/m);
  assert.match(workflow, /EARTHTOOL_CLI_EXECUTABLE="\$CLI" dotnet test/);
  assert.match(workflow, /CliProcessExposesOnlyTheGltfMshCommandTreeAndStableExitStatuses/);
});

test("release creation waits for security and groups only features and fixes", async () => {
  const workflow = await readFile(
    new URL("../.github/workflows/release.yml", import.meta.url),
    "utf8");
  const releaseNotesStep = workflow.match(
    /- name: Generate release notes([\s\S]*?)- name: Create Release/)?.[1] ?? "";

  assert.match(workflow, /needs: \[build-and-test, code-quality, security-scan\]/);
  assert.match(releaseNotesStep, /local pattern=/);
  assert.match(releaseNotesStep, /append_commits "feat" "Features"/);
  assert.match(releaseNotesStep, /append_commits "fix" "Fixes"/);
});

test("official corpus qualification remains a local pre-publish gate", async () => {
  const workflow = await readFile(
    new URL("../.github/workflows/release.yml", import.meta.url),
    "utf8");

  assert.equal(requiredGates.at(-1), "official-corpus");
  assert.doesNotMatch(workflow, /official-msh-corpus-qualification|OFFICIAL_MSH_CORPUS_ROOT/);
});
