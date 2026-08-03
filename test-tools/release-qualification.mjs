import { spawn } from "node:child_process";
import { access, mkdir, readFile, readdir, rm, writeFile } from "node:fs/promises";
import { platform as hostPlatform, release as osRelease } from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { corpusInterchangeStages } from "./official-corpus-contract.mjs";

export const requiredGates = Object.freeze([
  "tooling",
  "node-tests",
  "restore",
  "build",
  "test-discovery",
  "reproducible-tests",
  "public-packages",
  "approved-snapshots",
  "release-boundary",
  "linux-artifacts",
  "blender-matrix",
  "official-corpus"
]);

export const expectedTestCounts = Object.freeze({ msh: 442, cli: 31 });

const requiredDynamicQualificationTests = Object.freeze({
  msh: [
    "EveryKnownDynamicEffectHasAnExplicitCanonicalRecipe",
    "SpriteEffectsExportThroughThePublicGlbSeam",
    "RibbonEffectsExportThroughThePublicGlbSeam",
    "AttachedAndProceduralEffectsExportWithExplicitPreviewContexts",
    "RibbonPreviewRetainsHalfWidthSignTextureSideAndWinding",
    "UnsupportedEffectAndObjectLimitFailWithoutOutput",
    "ScalableObjectUsesAReferencedStaticMeshPreviewAndRoundTripsExactly",
    "SeparateGltfRoundTripsTheExactDynamicMsh"
  ],
  cli: ["BatchExportsAllSupportedDynamicEffectsAsSeparateGltf"]
});

const expectedArtifacts = Object.freeze([
  "EarthTool.CLI-Linux-x64.tar.gz",
  "EarthTool.WD.GUI-Linux-x64.tar.gz",
  "EarthTool.PAR.GUI-Linux-x64.tar.gz",
  "EarthTool.TEX.GUI-Linux-x64.tar.gz"
]);

const releaseProjects = Object.freeze([
  ["EarthTool.CLI", "EarthTool.CLI/EarthTool.CLI.csproj"],
  ["EarthTool.WD.GUI", "EarthTool.WD.GUI/EarthTool.WD.GUI.csproj"],
  ["EarthTool.PAR.GUI", "EarthTool.PAR.GUI/EarthTool.PAR.GUI.csproj"],
  ["EarthTool.TEX.GUI", "EarthTool.TEX.GUI/EarthTool.TEX.GUI.csproj"]
]);

const publicProjects = Object.freeze([
  ["EarthTool.Common", "EarthTool.Common/EarthTool.Common.csproj"],
  ["EarthTool.WD", "EarthTool.WD/EarthTool.WD.csproj"],
  ["EarthTool.MSH", "EarthTool.MSH/EarthTool.MSH.csproj"],
  ["EarthTool.GLTF", "EarthTool.GLTF/EarthTool.GLTF.csproj"],
  ["EarthTool.PAR", "EarthTool.PAR/EarthTool.PAR.csproj"],
  ["EarthTool.TEX", "EarthTool.TEX/EarthTool.TEX.csproj"]
]);

const requiredReleasePaths = Object.freeze([
  "EarthTool.GLTF/EarthTool.GLTF.csproj",
  "docs/api/gltf.md",
  "docs/migration-collada-to-gltf.md"
]);

const forbiddenReleasePaths = Object.freeze([
  "EarthTool.DAE/EarthTool.DAE.csproj",
  "EarthTool.DAE.Tests/EarthTool.DAE.Tests.csproj"
]);

function fail(message) {
  throw new Error(message);
}

export function validateGateResults(gates, commit) {
  if (!/^[a-f0-9]{40}$/i.test(commit ?? "")) {
    fail("Release qualification requires a full Git commit.");
  }
  if (!Array.isArray(gates)
    || gates.length !== requiredGates.length
    || gates.some((gate, index) => gate?.name !== requiredGates[index])) {
    fail("Release qualification gate inventory is incomplete or out of order.");
  }
  const incomplete = gates.find(gate => gate.outcome !== "passed");
  if (incomplete) {
    fail(`Required gate ${incomplete.name} did not pass.`);
  }
  return gates;
}

export function validateDynamicQualificationTests(mshTests, cliTests) {
  if (requiredDynamicQualificationTests.msh.some(test => !mshTests.includes(test))
    || requiredDynamicQualificationTests.cli.some(test => !cliTests.includes(test))) {
    fail("Dynamic qualification test inventory is incomplete.");
  }
}

function validateBlenderEvidence(blender, commit, platform) {
  const requestedLanes = blender?.requestedLanes?.map(lane => lane.name) ?? [];
  const requestedByName = new Map(
    (blender?.requestedLanes ?? []).map(lane => [lane.name, lane]));
  const completedLanes = blender?.builds?.flatMap(build => build.requestedLanes ?? []) ?? [];
  if (blender?.format !== "earthtool.blender-qualification-evidence"
    || blender.version !== 1
    || blender.outcome !== "passed"
    || blender.platform !== platform
    || blender.earthToolCommit !== commit
    || !Array.isArray(blender.requestedLanes)
    || blender.requestedLanes.length === 0
    || requestedLanes.some(name => !name)
    || new Set(requestedLanes).size !== requestedLanes.length
    || !Array.isArray(blender.builds)
    || blender.builds.length === 0
    || blender.builds.some(build => build.outcome !== "passed"
      || !build.version
      || !build.buildHash
      || !build.addonVersion
      || !Array.isArray(build.requestedLanes)
      || build.requestedLanes.length === 0
      || build.requestedLanes.some(name => {
        const request = requestedByName.get(name);
        return !request || !build.version.startsWith(`${request.series}.`);
      }))
    || completedLanes.length !== requestedLanes.length
    || new Set(completedLanes).size !== completedLanes.length
    || requestedLanes.some(name => !completedLanes.includes(name))) {
    fail("Blender qualification evidence is incomplete or does not match this release.");
  }
}

function validateCorpusEvidence(corpus, commit, platform) {
  const assetCount = corpus?.corpus?.assets;
  const operations = new Map((corpus?.operations ?? []).map(operation => [operation.stage, operation]));
  if (corpus?.format !== "earthtool.official-msh-qualification-evidence"
    || corpus.version !== 2
    || corpus.profile?.format !== "earthtool.official-msh-corpus-profile"
    || corpus.profile.version !== 2
    || corpus.outcome !== "passed"
    || corpus.earthToolCommit !== commit
    || corpus.platform !== platform
    || !Number.isSafeInteger(assetCount)
    || assetCount <= 0
    || !Number.isSafeInteger(corpus.corpus?.dynamicAssets)
    || corpus.corpus.dynamicAssets <= 0
    || corpus.corpus.staticAssets + corpus.corpus.dynamicAssets !== assetCount
    || corpus.dynamicCoverage?.assets !== corpus.corpus.dynamicAssets
    || corpus.dynamicCoverage?.objects < corpus.dynamicCoverage?.assets
    || corpus.dynamicCoverage?.unknownEffectObjects !== 0
    || !Array.isArray(corpus.dynamicCoverage?.effectTypes)
    || corpus.dynamicCoverage.effectTypes.length === 0
    || corpusInterchangeStages.some(stage => {
      const operation = operations.get(stage);
      return operation?.attempted !== assetCount
        || operation.passed !== assetCount
        || operation.failed !== 0;
    })
    || corpus.validators?.khronos?.packages !== assetCount * 4
    || corpus.validators.khronos.errors !== 0
    || corpus.validators.khronos.warnings !== 0
    || corpus.exportAllMeshes?.assets <= 0
    || corpus.exportAllMeshes?.dynamicAssets !== corpus.corpus.dynamicAssets
    || corpus.exportAllMeshes?.staticAssets + corpus.exportAllMeshes?.dynamicAssets
      !== corpus.exportAllMeshes?.assets
    || corpus.exportAllMeshes?.succeeded !== corpus.exportAllMeshes?.assets
    || corpus.exportAllMeshes?.failed !== 0
    || corpus.exportAllMeshes?.cancelled !== 0
    || corpus.exportAllMeshes?.unsupportedDomainDiagnostics !== 0
    || corpus.exportAllMeshes?.outputFiles !== corpus.exportAllMeshes?.assets
    || (corpus.diagnostics ?? []).some(diagnostic => diagnostic.code === "ETG1002")
    || corpus.passFail?.failedOperations !== 0
    || corpus.passFail?.aggregateFailures !== 0) {
    fail("Official corpus evidence is incomplete or does not match this release.");
  }
}

function validateSubordinateEvidence(blender, corpus, commit, platform) {
  validateBlenderEvidence(blender, commit, platform);
  validateCorpusEvidence(corpus, commit, platform);
}

export function buildEvidence({ commit, platform, os, tools, gates, blender, corpus }) {
  validateGateResults(gates, commit);
  validateSubordinateEvidence(blender, corpus, commit, platform);
  for (const name of ["node", "npm", "dotnet", "git"]) {
    if (!tools?.[name]) {
      fail(`Tool provenance is missing: ${name}.`);
    }
  }
  return {
    format: "earthtool.release-qualification-evidence",
    version: 1,
    outcome: "passed",
    earthToolCommit: commit,
    platform,
    os,
    tools: {
      ...tools,
      sharpGltf: corpus.tools?.sharpGltf ?? null,
      khronosValidator: corpus.tools?.khronosValidator ?? null,
      blender: blender.builds.map(build => ({
        version: build.version,
        buildHash: build.buildHash,
        addonVersion: build.addonVersion ?? null
      }))
    },
    profiles: {
      platforms: [platform],
      blender: blender.requestedLanes,
      corpus: corpus.profile,
      msh: corpus.profiles?.msh ?? null,
      gltf: corpus.profiles?.gltf ?? null
    },
    gates,
    results: {
      blender,
      officialCorpus: corpus
    }
  };
}

export function validateArtifactInventory(artifacts) {
  if (!Array.isArray(artifacts)
    || artifacts.length !== expectedArtifacts.length
    || artifacts.some((artifact, index) => artifact !== expectedArtifacts[index])) {
    fail("Linux release artifact inventory is incomplete or contains an unsupported artifact.");
  }
  return artifacts;
}

export function validateRepositoryState(status) {
  if (status.trim()) {
    fail("Release qualification requires a clean Git worktree.");
  }
}

export function validateReleaseBoundary(boundary) {
  if (!boundary.solution?.includes("EarthTool.GLTF")
    || boundary.solution.includes("EarthTool.DAE")
    || !boundary.cliProject?.includes("EarthTool.GLTF")
    || boundary.cliProject.includes("EarthTool.DAE")
    || requiredReleasePaths.some(required => !boundary.paths?.has(required))
    || forbiddenReleasePaths.some(forbidden => boundary.paths?.has(forbidden))) {
    fail("Release project boundary must contain GLTF and exclude DAE.");
  }
  const migrationRequirements = [
    "v0.4.4",
    "https://github.com/Arkezar/EarthTool/releases/tag/v0.4.4",
    "https://github.com/Arkezar/EarthTool/tree/v0.4.4",
    "https://github.com/Arkezar/EarthTool/issues/133"
  ];
  if (migrationRequirements.some(requirement => !boundary.migration?.includes(requirement))
    || !boundary.documentationIndex?.includes("migration-collada-to-gltf.md")) {
    fail("Release migration documentation is incomplete.");
  }
  const requiredDocumentation = [
    "EarthTool.GLTF",
    "msh export",
    "msh import edit",
    "msh import new",
    "all 15 recognized dynamic effect"
  ];
  const unsupportedLegacyReferences = (boundary.activeDocumentation ?? "")
    .split(/\r?\n/)
    .filter(line => /\b(?:DAE|COLLADA)\b/i.test(line)
      && !/\b(?:remov\w*|migrat\w*)\b/i.test(line));
  if (requiredDocumentation.some(requirement => !boundary.activeDocumentation?.includes(requirement))
    || unsupportedLegacyReferences.length > 0) {
    fail("Active documentation does not match the glTF release boundary.");
  }
  if (boundary.releaseWorkflow
    && /macos-latest|osx-x64|macOS-x64/.test(boundary.releaseWorkflow)) {
    fail("Release workflow still contains a macOS release lane.");
  }
  return boundary;
}

function parseArguments(arguments_) {
  const options = {};
  for (let index = 0; index < arguments_.length; index += 2) {
    const key = arguments_[index];
    const value = arguments_[index + 1];
    if (!key?.startsWith("--") || value === undefined) {
      fail(`Invalid argument: ${key ?? "<missing>"}`);
    }
    options[key.slice(2)] = value;
  }
  return options;
}

function run(executable, arguments_, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(executable, arguments_, {
      cwd: options.cwd,
      env: options.env,
      shell: false,
      windowsHide: true
    });
    let stdout = "";
    let stderr = "";
    child.stdout?.on("data", data => {
      stdout += data;
      if (options.stream !== false) {
        process.stdout.write(data);
      }
    });
    child.stderr?.on("data", data => {
      stderr += data;
      if (options.stream !== false) {
        process.stderr.write(data);
      }
    });
    child.on("error", reject);
    child.on("close", code => {
      if (code === 0) {
        resolve({ stdout, stderr });
      } else {
        reject(new Error(`${executable} exited with ${code}.`));
      }
    });
  });
}

async function readJson(filePath) {
  return JSON.parse((await readFile(filePath, "utf8")).replace(/^\uFEFF/, ""));
}

async function exists(filePath) {
  try {
    await access(filePath);
    return true;
  } catch {
    return false;
  }
}

async function collectReceivedFiles(directory) {
  const received = [];
  async function visit(current) {
    for (const entry of await readdir(current, { withFileTypes: true })) {
      const entryPath = path.join(current, entry.name);
      if (entry.isDirectory()) {
        await visit(entryPath);
      } else if (entry.isFile() && entry.name.includes(".received.")) {
        received.push(entryPath);
      }
    }
  }
  await visit(directory);
  return received;
}

function countDiscoveredTests(output) {
  return output.split(/\r?\n/).filter(line => /^\s{4}\S/.test(line)).length;
}

function readNodeTestSummary(output) {
  const values = Object.fromEntries(
    [...output.matchAll(/^ℹ (tests|pass|fail|skipped) (\d+)$/gm)]
      .map(match => [match[1], Number(match[2])]));
  if (!Number.isSafeInteger(values.tests)
    || values.tests <= 0
    || values.pass !== values.tests
    || values.fail !== 0
    || values.skipped !== 0) {
    fail("Node test result summary is incomplete or contains non-passing tests.");
  }
  return values;
}

function readDotnetTestSummary(output) {
  const summaries = [...output.matchAll(
    /Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)/g)]
    .map(match => ({
      failed: Number(match[1]),
      passed: Number(match[2]),
      skipped: Number(match[3]),
      total: Number(match[4])
    }));
  if (summaries.length === 0
    || summaries.some(summary => summary.failed !== 0
      || summary.skipped !== 0
      || summary.passed !== summary.total)) {
    fail(".NET test result summary is incomplete or contains non-passing tests.");
  }
  return summaries.reduce((aggregate, summary) => ({
    projects: aggregate.projects + 1,
    failed: aggregate.failed + summary.failed,
    passed: aggregate.passed + summary.passed,
    skipped: aggregate.skipped + summary.skipped,
    total: aggregate.total + summary.total
  }), { projects: 0, failed: 0, passed: 0, skipped: 0, total: 0 });
}

async function inspectReleaseBoundary(root) {
  const paths = new Set();
  for (const candidate of [...requiredReleasePaths, ...forbiddenReleasePaths]) {
    if (await exists(path.join(root, candidate))) {
      paths.add(candidate);
    }
  }
  return validateReleaseBoundary({
    solution: await readFile(path.join(root, "EarthTool.sln"), "utf8"),
    cliProject: await readFile(path.join(root, "EarthTool.CLI", "EarthTool.CLI.csproj"), "utf8"),
    migration: await readFile(path.join(root, "docs", "migration-collada-to-gltf.md"), "utf8"),
    documentationIndex: await readFile(path.join(root, "docs", "README.md"), "utf8"),
    activeDocumentation: (await Promise.all([
      "README.md",
      "docs/README.md",
      "docs/overview.md",
      "docs/architecture.md",
      "docs/project-structure.md",
      "docs/quickstart.md",
      "docs/api/msh.md",
      "docs/api/gltf.md"
    ].map(file => readFile(path.join(root, file), "utf8")))).join("\n"),
    releaseWorkflow: await readFile(path.join(root, ".github", "workflows", "release.yml"), "utf8"),
    paths
  });
}

export async function validatePublicPackages(root, workDirectory) {
  const packageDirectory = path.join(workDirectory, "packages");
  const consumerDirectory = path.join(workDirectory, "package-consumer");
  const packageCache = path.join(workDirectory, "nuget-packages");
  await rm(packageDirectory, { recursive: true, force: true });
  await rm(consumerDirectory, { recursive: true, force: true });
  await rm(packageCache, { recursive: true, force: true });
  await mkdir(packageDirectory, { recursive: true });
  for (const [, project] of publicProjects) {
    await run("dotnet", [
      "pack", project,
      "--configuration", "Release",
      "--no-restore",
      "--output", packageDirectory
    ], { cwd: root });
  }

  const packages = (await readdir(packageDirectory))
    .filter(file => file.endsWith(".nupkg") && !file.endsWith(".snupkg"))
    .sort();
  if (packages.length !== publicProjects.length
    || publicProjects.some(([name]) => !packages.some(file => file.startsWith(`${name}.`)))) {
    fail("Public package inventory is incomplete.");
  }
  for (const packageName of packages) {
    const packageId = publicProjects.find(([name]) => packageName.startsWith(`${name}.`))?.[0];
    const entries = await run(
      "unzip", ["-Z1", path.join(packageDirectory, packageName)], { cwd: root, stream: false });
    const manifest = packageId
      ? await run(
        "unzip",
        ["-p", path.join(packageDirectory, packageName), `${packageId}.nuspec`],
        { cwd: root, stream: false })
      : { stdout: "EarthTool.DAE" };
    if (/EarthTool\.DAE/i.test(entries.stdout + manifest.stdout)
      || (packageName.startsWith("EarthTool.GLTF.")
        && !entries.stdout.includes("EarthTool.GLTF.dll"))) {
      fail(`Public package boundary failed for ${packageName}.`);
    }
  }

  const gltfPackage = packages.find(file => file.startsWith("EarthTool.GLTF."));
  const version = gltfPackage?.match(/^EarthTool\.GLTF\.(.+)\.nupkg$/)?.[1];
  if (!version) {
    fail("EarthTool.GLTF package version could not be determined.");
  }
  await mkdir(consumerDirectory, { recursive: true });
  await writeFile(path.join(consumerDirectory, "PackageConsumer.csproj"), `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="EarthTool.GLTF" Version="${version}" />
    <PackageReference Include="EarthTool.MSH" Version="${version}" />
  </ItemGroup>
</Project>
`);
  await writeFile(path.join(consumerDirectory, "Program.cs"), `using System;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;

Console.WriteLine($"{typeof(GltfInterchange).FullName} {typeof(MeshAsset).FullName}");
`);
  const nugetConfig = path.join(consumerDirectory, "NuGet.config");
  await writeFile(nugetConfig, `<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="earthtool-local" value="${packageDirectory}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="earthtool-local">
      <package pattern="EarthTool.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="Microsoft.*" />
      <package pattern="System.*" />
      <package pattern="NETStandard.*" />
      <package pattern="runtime.*" />
      <package pattern="SharpGLTF.*" />
      <package pattern="SkiaSharp*" />
      <package pattern="HarfBuzzSharp*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
`);
  const consumerProject = path.join(consumerDirectory, "PackageConsumer.csproj");
  const consumerEnvironment = { ...process.env, NUGET_PACKAGES: packageCache };
  await run("dotnet", [
    "restore", consumerProject,
    "--no-cache",
    "--configfile", nugetConfig
  ], { cwd: root, env: consumerEnvironment });
  await run("dotnet", [
    "build", consumerProject,
    "--configuration", "Release",
    "--no-restore"
  ], { cwd: root, env: consumerEnvironment });
  return packages;
}

async function publishArtifacts(root, workDirectory) {
  const artifactsDirectory = path.join(workDirectory, "artifacts");
  const publishDirectory = path.join(workDirectory, "publish");
  await rm(artifactsDirectory, { recursive: true, force: true });
  await rm(publishDirectory, { recursive: true, force: true });
  await mkdir(artifactsDirectory, { recursive: true });
  for (const [name, project] of releaseProjects) {
    const output = path.join(publishDirectory, name);
    await run("dotnet", [
      "publish", project,
      "--configuration", "Release",
      "--runtime", "linux-x64",
      "--self-contained", "true",
      "--output", output,
      "-p:PublishSingleFile=true",
      "-p:IncludeNativeLibrariesForSelfExtract=true",
      "-p:EnableCompressionInSingleFile=true",
      "-p:DebugType=none",
      "-p:DebugSymbols=false"
    ], { cwd: root });
    const executable = path.join(output, name);
    const bundleStrings = await run("strings", [executable], { cwd: root, stream: false });
    if (/EarthTool\.DAE/i.test(bundleStrings.stdout)
      || (name === "EarthTool.CLI" && !bundleStrings.stdout.includes("EarthTool.GLTF.dll"))) {
      fail(`Published application boundary failed for ${name}.`);
    }
    const archive = `${name}-Linux-x64.tar.gz`;
    await run("tar", ["-czf", path.join(artifactsDirectory, archive), "-C", output, "."], { cwd: root });
    const archiveEntries = await run(
      "tar", ["-tzf", path.join(artifactsDirectory, archive)], { cwd: root, stream: false });
    if (/EarthTool\.DAE/i.test(archiveEntries.stdout)
      || !archiveEntries.stdout.split(/\r?\n/).includes(`./${name}`)) {
      fail(`Release archive boundary failed for ${archive}.`);
    }
  }

  const cli = path.join(publishDirectory, "EarthTool.CLI", "EarthTool.CLI");
  const rootHelp = await run(cli, ["--help"], { cwd: root, stream: false });
  const mshHelp = await run(cli, ["msh", "--help"], { cwd: root, stream: false });
  const importHelp = await run(cli, ["msh", "import", "--help"], { cwd: root, stream: false });
  const combinedHelp = `${rootHelp.stdout}\n${mshHelp.stdout}\n${importHelp.stdout}`;
  if (!combinedHelp.includes("export <INPUT>")
    || !combinedHelp.includes("edit <INPUT>")
    || !combinedHelp.includes("new <INPUT>")
    || combinedHelp.includes("dae <InputFilePath>")) {
    fail("Published CLI command tree does not match the glTF release contract.");
  }
  const artifacts = (await readdir(artifactsDirectory)).sort((left, right) => {
    const leftIndex = expectedArtifacts.indexOf(left);
    const rightIndex = expectedArtifacts.indexOf(right);
    return leftIndex - rightIndex;
  });
  return { artifacts: validateArtifactInventory(artifacts), cli };
}

async function main() {
  const options = parseArguments(process.argv.slice(2));
  const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
  const corpusRoot = options.corpus ?? process.env.EARTHTOOL_OFFICIAL_MSH_CORPUS;
  const evidencePath = path.resolve(options.evidence
    ?? path.join(root, "artifacts", "release-qualification-v1.json"));
  const workDirectory = path.join(root, "artifacts", "release-qualification");
  const blenderEvidencePath = path.join(workDirectory, "blender-qualification-linux-x64.json");
  const corpusEvidencePath = path.join(workDirectory, "official-msh-corpus-qualification.json");
  await rm(evidencePath, { force: true });
  if (hostPlatform() !== "linux" || process.arch !== "x64") {
    fail(`Release qualification requires linux-x64; current host is ${hostPlatform()}-${process.arch}.`);
  }
  if (!corpusRoot) {
    fail("Release qualification requires --corpus or EARTHTOOL_OFFICIAL_MSH_CORPUS.");
  }
  const corpus = path.resolve(corpusRoot);
  await access(corpus);

  const gates = [];
  async function gate(name, action) {
    if (name !== requiredGates[gates.length]) {
      fail(`Unexpected release gate order: ${name}.`);
    }
    process.stdout.write(`\n== ${name} ==\n`);
    const details = await action();
    gates.push({ name, outcome: "passed", ...(details === undefined ? {} : { details }) });
  }

  let tools;
  let commit;
  let blenderEvidence;
  let corpusEvidence;
  let publishedCli;
  await gate("tooling", async () => {
    const [npm, dotnet, gitVersion, gitCommit, gitStatus, tar, unzip, strings] = await Promise.all([
      run("npm", ["--version"], { cwd: root, stream: false }),
      run("dotnet", ["--version"], { cwd: root, stream: false }),
      run("git", ["--version"], { cwd: root, stream: false }),
      run("git", ["rev-parse", "HEAD"], { cwd: root, stream: false }),
      run("git", ["status", "--porcelain", "--untracked-files=all"], { cwd: root, stream: false }),
      run("tar", ["--version"], { cwd: root, stream: false }),
      run("unzip", ["-v"], { cwd: root, stream: false }),
      run("strings", ["--version"], { cwd: root, stream: false })
    ]);
    commit = gitCommit.stdout.trim();
    validateRepositoryState(gitStatus.stdout);
    tools = {
      node: process.version,
      npm: npm.stdout.trim(),
      dotnet: dotnet.stdout.trim(),
      git: gitVersion.stdout.trim(),
      tar: tar.stdout.split(/\r?\n/, 1)[0].trim(),
      unzip: unzip.stdout.split(/\r?\n/, 1)[0].trim(),
      strings: strings.stdout.split(/\r?\n/, 1)[0].trim()
    };
    validateGateResults(requiredGates.map(name => ({ name, outcome: "passed" })), commit);
    return tools;
  });
  await rm(workDirectory, { recursive: true, force: true });
  await mkdir(workDirectory, { recursive: true });
  await gate("node-tests", async () => {
    await run("npm", ["ci", "--prefix", "test-tools"], { cwd: root });
    const result = await run("npm", ["test", "--prefix", "test-tools"], { cwd: root });
    return readNodeTestSummary(result.stdout);
  });
  await gate("restore", async () => {
    await run("dotnet", ["restore", "EarthTool.sln"], { cwd: root });
    return { solution: "EarthTool.sln" };
  });
  await gate("build", async () => {
    await run(
      "dotnet",
      ["build", "EarthTool.sln", "--configuration", "Release", "--no-restore"],
      { cwd: root });
    return { solution: "EarthTool.sln", configuration: "Release" };
  });
  await gate("test-discovery", async () => {
    const msh = await run("dotnet", [
      "test", "EarthTool.MSH.Tests/EarthTool.MSH.Tests.csproj",
      "--configuration", "Release", "--no-build", "--list-tests"
    ], { cwd: root, stream: false });
    const cli = await run("dotnet", [
      "test", "EarthTool.CLI.Tests/EarthTool.CLI.Tests.csproj",
      "--configuration", "Release", "--no-build", "--list-tests"
    ], { cwd: root, stream: false });
    const counts = { msh: countDiscoveredTests(msh.stdout), cli: countDiscoveredTests(cli.stdout) };
    if (counts.msh !== expectedTestCounts.msh || counts.cli !== expectedTestCounts.cli) {
      fail(`Unexpected test discovery: MSH=${counts.msh}, CLI=${counts.cli}.`);
    }
    validateDynamicQualificationTests(msh.stdout, cli.stdout);
    return counts;
  });
  await gate("reproducible-tests", async () => {
    const result = await run("dotnet", [
      "test", "EarthTool.sln",
      "--configuration", "Release",
      "--no-build",
      "--filter", "Category!=BlenderQualification&Category!=OfficialCorpusQualification"
    ], {
      cwd: root,
      env: { ...process.env, EARTHTOOL_RUN_KHRONOS_VALIDATOR: "1" }
    });
    return readDotnetTestSummary(result.stdout);
  });
  await gate("public-packages", async () => {
    const packages = await validatePublicPackages(root, workDirectory);
    return { packages, consumerBuild: "passed" };
  });
  await gate("approved-snapshots", async () => {
    const received = [
      ...await collectReceivedFiles(path.join(root, "EarthTool.MSH.Tests")),
      ...await collectReceivedFiles(path.join(root, "EarthTool.CLI.Tests"))
    ];
    if (received.length > 0) {
      fail(`Unapproved snapshots remain: ${received.map(file => path.basename(file)).join(", ")}.`);
    }
    return { received: 0 };
  });
  await gate("release-boundary", async () => {
    await inspectReleaseBoundary(root);
    return { interchange: "GLTF", removed: "DAE", migrationFrom: "v0.4.4" };
  });
  await gate("linux-artifacts", async () => {
    const published = await publishArtifacts(root, workDirectory);
    publishedCli = published.cli;
    return { artifacts: published.artifacts };
  });
  await gate("blender-matrix", async () => {
    const arguments_ = [
      "test-tools/blender-qualification.mjs",
      "--platform", "linux-x64",
      "--evidence", blenderEvidencePath
    ];
    if (options["blender-cache"]) {
      arguments_.push("--cache", path.resolve(options["blender-cache"]));
    }
    await run(process.execPath, arguments_, { cwd: root });
    blenderEvidence = await readJson(blenderEvidencePath);
    validateBlenderEvidence(blenderEvidence, commit, "linux-x64");
    return {
      requestedLanes: blenderEvidence.requestedLanes,
      builds: blenderEvidence.builds.map(build => ({
        version: build.version,
        buildHash: build.buildHash,
        outcome: build.outcome
      }))
    };
  });
  await gate("official-corpus", async () => {
    const arguments_ = [
      "test-tools/official-corpus-qualification.mjs",
      "--corpus", corpus,
      "--evidence", corpusEvidencePath,
      "--cli", publishedCli
    ];
    if (options.workers) {
      arguments_.push("--workers", options.workers);
    }
    await run(process.execPath, arguments_, { cwd: root });
    corpusEvidence = await readJson(corpusEvidencePath);
    validateSubordinateEvidence(blenderEvidence, corpusEvidence, commit, "linux-x64");
    return {
      corpus: corpusEvidence.corpus,
      passFail: corpusEvidence.passFail,
      validators: corpusEvidence.validators
    };
  });

  const evidence = buildEvidence({
    commit,
    platform: "linux-x64",
    os: `${hostPlatform()} ${osRelease()}`,
    tools,
    gates,
    blender: blenderEvidence,
    corpus: corpusEvidence
  });
  await mkdir(path.dirname(evidencePath), { recursive: true });
  await writeFile(evidencePath, JSON.stringify(evidence, null, 2) + "\n");
  process.stdout.write(`\nRelease qualification passed: ${evidencePath}\n`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => {
    console.error(`Release qualification failed: ${error instanceof Error ? error.message : error}`);
    process.exitCode = 1;
  });
}
