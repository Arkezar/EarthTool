import { readFile, readdir, mkdir, rm, writeFile } from "node:fs/promises";
import { platform as hostPlatform, release as osRelease } from "node:os";
import path from "node:path";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const binaryStages = [
  "msh.read",
  "msh.validate",
  "msh.write",
  "msh.semantic-equivalence",
  "msh.canonical-idempotence"
];
const staticStages = [
  "glb.export",
  "glb.sharp-gltf-validate",
  "glb.khronos-validate",
  "glb.unchanged-import",
  "glb.canonical-baseline",
  "gltf.export",
  "gltf.sharp-gltf-validate",
  "gltf.khronos-validate",
  "gltf.unchanged-import",
  "gltf.canonical-baseline",
  "glb.cli-export",
  "glb.cli-sharp-gltf-validate",
  "glb.cli-khronos-validate",
  "glb.cli-unchanged-import",
  "gltf.cli-export",
  "gltf.cli-sharp-gltf-validate",
  "gltf.cli-khronos-validate",
  "gltf.cli-unchanged-import"
];
const requiredByteTotals = [
  "canonicalMsh",
  "glb",
  "gltfManifest",
  "gltfSidecars",
  "unchangedImportedMsh",
  "cliPackages",
  "cliImportedMsh"
];

class QualificationError extends Error {
  constructor(category) {
    super(category);
    this.category = category;
  }
}

function fail(category) {
  throw new QualificationError(category);
}

function canonicalDiagnostics(diagnostics) {
  return [...diagnostics]
    .map(item => ({
      stage: item.stage,
      code: item.code,
      eventId: item.eventId,
      severity: item.severity,
      count: item.count
    }))
    .sort((left, right) =>
      left.stage.localeCompare(right.stage)
      || left.code.localeCompare(right.code)
      || left.eventId - right.eventId
      || left.severity.localeCompare(right.severity));
}

function canonicalValidatorCodes(codes) {
  return [...codes]
    .map(item => ({ code: item.code, count: item.count }))
    .sort((left, right) => left.code.localeCompare(right.code));
}

export function validateQualificationSummary(summary, profile) {
  if (summary?.format !== "earthtool.official-msh-corpus-event" || summary.version !== 1) {
    fail("event-contract");
  }
  if (profile?.format !== "earthtool.official-msh-corpus-profile" || profile.version !== 1) {
    fail("profile-contract");
  }
  const corpus = summary.corpus;
  if (!/^[a-f0-9]{64}$/.test(corpus?.fingerprint ?? "")
    || corpus.fingerprintAlgorithm !== "sha256-content-multiset-v1"
    || !Number.isSafeInteger(corpus.assets)
    || corpus.assets <= 0
    || !Number.isSafeInteger(corpus.discoveredMshFiles)
    || !Number.isSafeInteger(corpus.excludedNonFramedOrUnsupported)
    || !Number.isSafeInteger(corpus.excludedByProfile)
    || corpus.assets + corpus.excludedNonFramedOrUnsupported + corpus.excludedByProfile
      !== corpus.discoveredMshFiles
    || corpus.staticAssets + corpus.dynamicAssets !== corpus.assets
    || corpus.assets !== profile.corpus?.assets
    || corpus.staticAssets !== profile.corpus?.staticAssets
    || corpus.dynamicAssets !== profile.corpus?.dynamicAssets
    || corpus.discoveredMshFiles !== profile.corpus?.discoveredMshFiles
    || corpus.excludedNonFramedOrUnsupported !== profile.corpus?.excludedNonFramedOrUnsupported
    || corpus.excludedByProfile !== profile.corpus?.excludedByProfile
    || !/^[a-f0-9]{64}$/.test(profile.corpus?.fingerprint ?? "")
    || corpus.fingerprint !== profile.corpus.fingerprint) {
    fail("corpus-mismatch");
  }
  if (!summary.bytes
    || Object.keys(summary.bytes).length !== requiredByteTotals.length
    || requiredByteTotals.some(key => !Number.isSafeInteger(summary.bytes[key]) || summary.bytes[key] < 0)
    || !summary.profiles?.msh
    || !summary.profiles?.gltf
    || Object.keys(summary.profiles.msh).length === 0
    || Object.keys(summary.profiles.gltf).length === 0) {
    fail("evidence-contract");
  }

  if (!Array.isArray(summary.operations)) {
    fail("oracle-inventory");
  }
  const operations = new Map(summary.operations.map(item => [item.stage, item]));
  const expectedStages = new Map([
    ...binaryStages.map(stage => [stage, corpus.assets]),
    ...staticStages.map(stage => [stage, corpus.staticAssets])
  ]);
  if (summary.operations.length !== expectedStages.size
    || operations.size !== summary.operations.length) {
    fail("oracle-inventory");
  }
  for (const [stage, expected] of expectedStages) {
    const operation = operations.get(stage);
    if (!operation || operation.attempted !== expected) {
      fail("oracle-inventory");
    }
    if (operation.failed !== 0 || operation.passed !== expected) {
      fail("oracle-failure");
    }
  }
  if (summary.failures?.total !== 0 || (summary.failures?.categories?.length ?? 0) !== 0) {
    fail("oracle-failure");
  }

  if (!Array.isArray(summary.diagnostics) || !Array.isArray(profile.diagnostics)) {
    fail("evidence-contract");
  }
  const diagnostics = canonicalDiagnostics(summary.diagnostics);
  if (diagnostics.some(item => item.severity === "Error")) {
    fail("operation-diagnostic");
  }
  if (JSON.stringify(diagnostics) !== JSON.stringify(canonicalDiagnostics(profile.diagnostics ?? []))) {
    fail("diagnostic-drift");
  }

  const khronos = summary.validators?.khronos;
  if (!khronos
    || khronos.packages !== corpus.staticAssets * 4
    || !khronos.validatorVersion && !khronos.version
    || khronos.errors !== 0
    || khronos.warnings !== 0) {
    fail("validator-issue");
  }
  const expectedKhronos = profile.validators?.khronos;
  if (!expectedKhronos
    || !Array.isArray(khronos.codes)
    || !Array.isArray(expectedKhronos.codes)
    || khronos.infos !== expectedKhronos.infos
    || khronos.hints !== expectedKhronos.hints
    || JSON.stringify(canonicalValidatorCodes(khronos.codes ?? []))
      !== JSON.stringify(canonicalValidatorCodes(expectedKhronos.codes ?? []))) {
    fail("validator-drift");
  }
  return summary;
}

export function buildEvidence({
  summary,
  profile,
  platform,
  os,
  earthToolCommit,
  dotnetVersion,
  sharpGltfVersion,
  failureCategory
}) {
  const effectiveFailure = failureCategory
    ?? (!summary || !profile || !earthToolCommit || !dotnetVersion || !sharpGltfVersion
      ? "evidence-contract"
      : null);
  return {
    format: "earthtool.official-msh-qualification-evidence",
    version: 1,
    profile: {
      format: profile?.format ?? "earthtool.official-msh-corpus-profile",
      version: profile?.version ?? 1
    },
    outcome: effectiveFailure ? "failed" : "passed",
    failureCategory: effectiveFailure,
    platform,
    os,
    earthToolCommit: earthToolCommit ?? null,
    tools: {
      node: process.version,
      dotnet: dotnetVersion ?? null,
      sharpGltf: sharpGltfVersion ?? null,
      khronosValidator: summary?.validators?.khronos?.validatorVersion
        ?? summary?.validators?.khronos?.version
        ?? null
    },
    policies: [
      "public MSH read, validate, write, semantic equivalence, and canonical idempotence",
      "SharpGLTF strict validation",
      "Khronos glTF Validator: zero errors and zero warnings; all findings recorded",
      "unchanged import reproduces the canonical MSH baseline",
      "exact aggregate diagnostic profile"
    ],
    corpus: summary?.corpus ?? null,
    profiles: summary?.profiles ?? null,
    bytes: summary?.bytes ?? null,
    operations: summary?.operations ?? [],
    diagnostics: summary?.diagnostics ?? [],
    validators: summary?.validators ?? null,
    passFail: summary ? {
      passedOperations: summary.operations.reduce((total, item) => total + item.passed, 0),
      failedOperations: summary.operations.reduce((total, item) => total + item.failed, 0),
      aggregateFailures: summary.failures.total
    } : { passedOperations: 0, failedOperations: 0, aggregateFailures: 1 }
  };
}

export function assertPrivacySafe(evidence, forbiddenValues = []) {
  const forbiddenKeys = /^(source|path|uri|name|texKey|textureKey|inputPath|destinationPath|relativePath|assetName)$/i;
  function visit(value, key = "") {
    if (forbiddenKeys.test(key)) {
      fail("privacy-violation");
    }
    if (typeof value === "string") {
      if (/[\\/]|\.msh\b|textures?[\\]/i.test(value)
        || forbiddenValues.some(item => item && value.localeCompare(item, undefined, { sensitivity: "accent" }) === 0)) {
        fail("privacy-violation");
      }
      return;
    }
    if (Array.isArray(value)) {
      value.forEach(item => visit(item));
      return;
    }
    if (value && typeof value === "object") {
      Object.entries(value).forEach(([childKey, child]) => visit(child, childKey));
    }
  }
  visit(evidence);
}

export function renderProgress(progress, width = 30) {
  const ratio = progress.total === 0 ? 0 : Math.min(1, progress.completed / progress.total);
  const filled = Math.round(ratio * width);
  const percent = Math.floor(ratio * 100);
  return `Official MSH corpus [${"#".repeat(filled)}${"-".repeat(width - filled)}] `
    + `${percent}% ${progress.completed}/${progress.total} `
    + `static=${progress.staticAssets} dynamic=${progress.dynamicAssets} failures=${progress.failures}`;
}

export function shouldReportProgress(progress, lastReported, isTTY, force = false) {
  return force
    || isTTY
    || progress.completed === progress.total
    || progress.completed - lastReported >= 100;
}

function watchProgress(filePath) {
  let lastObserved = -1;
  let lastReported = 0;
  let lastLength = 0;
  let polling;
  function poll(force = false) {
    if (polling) {
      return polling;
    }
    polling = (async () => {
      try {
        const progress = await readJson(filePath);
        if (progress.completed === lastObserved
          && !(force && progress.completed !== lastReported)) {
          return;
        }
        const line = renderProgress(progress);
        if (shouldReportProgress(progress, lastReported, process.stdout.isTTY, force)) {
          process.stdout.write(process.stdout.isTTY ? `\r${line.padEnd(lastLength)}` : line + "\n");
          lastLength = Math.max(lastLength, line.length);
          lastReported = progress.completed;
        }
        lastObserved = progress.completed;
      } catch {
        // The producer may be between file replacement operations.
      }
    })().finally(() => polling = null);
    return polling;
  }
  const timer = setInterval(poll, 500);
  return async () => {
    clearInterval(timer);
    await poll();
    await poll(true);
    if (process.stdout.isTTY && lastObserved >= 0) {
      process.stdout.write("\n");
    }
  };
}

export function run(executable, arguments_, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(executable, arguments_, {
      cwd: options.cwd,
      env: options.env,
      shell: false,
      windowsHide: true
    });
    let stdout = "";
    let stderr = "";
    let timedOut = false;
    const timeout = setTimeout(() => {
      timedOut = true;
      child.kill("SIGKILL");
    }, options.timeoutMs ?? 10 * 60 * 1000);
    child.stdout?.on("data", data => { stdout += data; });
    child.stderr?.on("data", data => { stderr += data; });
    child.on("error", error => {
      clearTimeout(timeout);
      reject(error);
    });
    child.on("close", code => {
      clearTimeout(timeout);
      resolve({ code, stdout, stderr, timedOut });
    });
  });
}

function readTestCount(trx) {
  const counters = trx.match(/<Counters\s+([^>]+)\/?\s*>/i)?.[1];
  if (!counters) {
    fail("test-discovery");
  }
  const values = Object.fromEntries(
    [...counters.matchAll(/(\w+)="(\d+)"/g)].map(match => [match[1], Number(match[2])]));
  if (values.total !== 1
    || values.passed !== 1
    || values.failed !== 0
    || values.error !== 0
    || values.timeout !== 0
    || values.aborted !== 0
    || values.notExecuted !== 0) {
    fail("test-execution");
  }
}

function parseArguments(arguments_) {
  const options = {};
  for (let index = 0; index < arguments_.length; index += 2) {
    const key = arguments_[index];
    if (!key?.startsWith("--") || arguments_[index + 1] === undefined) {
      fail("invalid-arguments");
    }
    options[key.slice(2)] = arguments_[index + 1];
  }
  return options;
}

function currentPlatform() {
  const platform = hostPlatform();
  if (platform === "linux" && process.arch === "x64") {
    return "linux-x64";
  }
  if (platform === "win32" && process.arch === "x64") {
    return "windows-x64";
  }
  return `${platform}-${process.arch}`;
}

async function readJson(filePath) {
  return JSON.parse((await readFile(filePath, "utf8")).replace(/^\uFEFF/, ""));
}

async function collectPrivateNames(directory) {
  const names = [];
  async function visit(current) {
    for (const entry of await readdir(current, { withFileTypes: true })) {
      if (entry.isDirectory()) {
        await visit(path.join(current, entry.name));
      } else if (entry.isFile()) {
        names.push(entry.name);
      }
    }
  }
  await visit(directory);
  return names;
}

function readSharpGltfVersion(packages) {
  return packages.match(/<PackageVersion Include="SharpGLTF\.Core" Version="([^"]+)"\s*\/>/)?.[1]
    ?? null;
}

async function main() {
  const options = parseArguments(process.argv.slice(2));
  const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
  const corpus = path.resolve(options.corpus
    ?? process.env.EARTHTOOL_OFFICIAL_MSH_CORPUS
    ?? "");
  const profilePath = path.resolve(options.profile
    ?? path.join(root, "test-tools", "official-corpus-profile.v1.json"));
  const evidencePath = path.resolve(options.evidence
    ?? path.join(root, "artifacts", "official-msh-corpus-qualification.json"));
  const resultsDirectory = path.join(root, "artifacts", "official-corpus-results");
  const eventsPath = path.join(resultsDirectory, "aggregate-event.json");
  const progressPath = path.join(resultsDirectory, "aggregate-progress.json");
  const trxPath = path.join(resultsDirectory, "official-corpus.trx");
  let profile;
  let summary;
  let failureCategory;
  let earthToolCommit;
  let dotnetVersion;
  let sharpGltfVersion;
  let forbiddenValues = [];
  try {
    if (!options.corpus && !process.env.EARTHTOOL_OFFICIAL_MSH_CORPUS) {
      fail("corpus-unavailable");
    }
    profile = await readJson(profilePath);
    forbiddenValues = await collectPrivateNames(corpus);
    await readFile(path.join(root, "test-tools", "node_modules", "gltf-validator", "package.json"));
    await rm(resultsDirectory, { recursive: true, force: true });
    await mkdir(resultsDirectory, { recursive: true });
    const dotnet = await run("dotnet", ["--version"], { cwd: root, timeoutMs: 30_000 });
    const git = await run("git", ["rev-parse", "HEAD"], { cwd: root, timeoutMs: 30_000 });
    if (dotnet.timedOut || git.timedOut) {
      fail("tool-timeout");
    }
    dotnetVersion = dotnet.stdout.trim();
    earthToolCommit = git.stdout.trim();
    sharpGltfVersion = readSharpGltfVersion(
      await readFile(path.join(root, "Directory.Packages.props"), "utf8"));
    if (!dotnetVersion || !earthToolCommit || !sharpGltfVersion) {
      fail("tool-provenance");
    }
    const stopProgress = watchProgress(progressPath);
    let testResult;
    try {
      testResult = await run("dotnet", [
        "test",
        "EarthTool.MSH.Tests/EarthTool.MSH.Tests.csproj",
        "--configuration", "Release",
        "--no-build",
        "--filter", "Category=OfficialCorpusQualification",
        "--results-directory", resultsDirectory,
        "--logger", `trx;LogFileName=${path.basename(trxPath)}`
      ], {
        cwd: root,
        timeoutMs: 2 * 60 * 60 * 1000,
        env: {
          ...process.env,
          EARTHTOOL_OFFICIAL_MSH_CORPUS: corpus,
          EARTHTOOL_OFFICIAL_MSH_EVIDENCE_EVENT: eventsPath,
          EARTHTOOL_OFFICIAL_MSH_PROGRESS_EVENT: progressPath
        }
      });
    } finally {
      await stopProgress();
    }
    summary = await readJson(eventsPath);
    validateQualificationSummary(summary, profile);
    readTestCount(await readFile(trxPath, "utf8"));
    if (testResult.timedOut) {
      fail("tool-timeout");
    }
    if (testResult.code !== 0) {
      fail("test-execution");
    }
  } catch (error) {
    failureCategory = error instanceof QualificationError
      ? error.category
      : "infrastructure-failure";
  }

  const evidence = buildEvidence({
    summary,
    profile,
    platform: currentPlatform(),
    os: `${hostPlatform()} ${osRelease()}`,
    earthToolCommit,
    dotnetVersion,
    sharpGltfVersion,
    failureCategory
  });
  try {
    assertPrivacySafe(evidence, forbiddenValues);
  } catch {
    evidence.outcome = "failed";
    evidence.failureCategory = "privacy-violation";
    evidence.corpus = null;
    evidence.profiles = null;
    evidence.bytes = null;
    evidence.operations = [];
    evidence.diagnostics = [];
    evidence.validators = null;
    evidence.passFail = { passedOperations: 0, failedOperations: 0, aggregateFailures: 1 };
    failureCategory = "privacy-violation";
  }
  await mkdir(path.dirname(evidencePath), { recursive: true });
  await writeFile(evidencePath, JSON.stringify(evidence, null, 2) + "\n");
  await rm(resultsDirectory, { recursive: true, force: true });
  if (failureCategory) {
    throw new QualificationError(failureCategory);
  }
  process.stdout.write("Official MSH corpus qualification passed.\n");
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => {
    const category = error instanceof QualificationError ? error.category : "infrastructure-failure";
    console.error(`Official MSH corpus qualification failed: ${category}.`);
    process.exitCode = 1;
  });
}
