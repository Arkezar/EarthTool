import { createHash } from "node:crypto";
import { createWriteStream } from "node:fs";
import {
  access,
  mkdir,
  readFile,
  readdir,
  rm,
  writeFile
} from "node:fs/promises";
import { homedir, platform as hostPlatform, release as osRelease } from "node:os";
import path from "node:path";
import { pipeline } from "node:stream/promises";
import { fileURLToPath } from "node:url";
import { spawn } from "node:child_process";

const baseUrl = "https://download.blender.org/release";
const requiredOwnershipOracles = [
  "no-edit-glb",
  "no-edit-separate-gltf",
  "hierarchy",
  "geometry",
  "material",
  "animation",
  "attachment",
  "light",
  "metadata-loss",
  "branch",
  "stale",
  "ambiguity"
];
const expectedOwnershipOutcomes = new Map([
  ["no-edit-glb", { count: 2, package: "glb", outcome: "Succeeded" }],
  ["no-edit-separate-gltf", { count: 2, package: "gltf", outcome: "Succeeded" }],
  ["hierarchy", { count: 1, package: "glb", outcome: "Succeeded" }],
  ["geometry", { count: 1, package: "glb", outcome: "Succeeded" }],
  ["material", { count: 1, package: "glb", outcome: "Succeeded" }],
  ["animation", { count: 1, package: "glb", outcome: "Succeeded" }],
  ["attachment", { count: 1, package: "glb", outcome: "Succeeded" }],
  ["light", { count: 1, package: "glb", outcome: "Succeeded" }],
  ["metadata-loss", { count: 1, package: "glb", outcome: "ETG2000" }],
  ["branch", { count: 1, package: "glb", outcome: "ETG2007" }],
  ["stale", { count: 1, package: "glb", outcome: "ETG2016" }],
  ["ambiguity", { count: 1, package: "glb", outcome: "ETG2009" }]
]);

function archiveSuffix(platform) {
  switch (platform) {
    case "linux-x64":
      return "linux-x64.tar.xz";
    case "windows-x64":
      return "windows-x64.zip";
    default:
      throw new Error(`Unsupported qualification platform: ${platform}`);
  }
}

function compareVersions(left, right) {
  const leftParts = left.split(".").map(Number);
  const rightParts = right.split(".").map(Number);
  for (let index = 0; index < Math.max(leftParts.length, rightParts.length); index++) {
    const difference = (leftParts[index] ?? 0) - (rightParts[index] ?? 0);
    if (difference !== 0) {
      return difference;
    }
  }
  return 0;
}

async function defaultFetchText(url) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`GET ${url} failed with HTTP ${response.status}`);
  }
  return response.text();
}

export async function resolveMatrix(matrix, platform, fetchText = defaultFetchText) {
  if (matrix.format !== "earthtool.blender-qualification-matrix" || matrix.version !== 1) {
    throw new Error("Unsupported Blender qualification matrix format or version.");
  }
  if (!Array.isArray(matrix.requestedLanes) || matrix.requestedLanes.length === 0) {
    throw new Error("The Blender qualification matrix has no requested lanes.");
  }

  const suffix = archiveSuffix(platform);
  const resolved = [];
  for (const request of matrix.requestedLanes) {
    if (!request.name || !/^\d+\.\d+$/.test(request.series ?? "")) {
      throw new Error("Every requested Blender lane needs a name and major.minor series.");
    }
    const directoryUrl = `${baseUrl}/Blender${request.series}/`;
    const listing = await fetchText(directoryUrl);
    const versions = [...listing.matchAll(
      new RegExp(`blender-(${request.series.replace(".", "\\.")}\\.\\d+)-${suffix.replaceAll(".", "\\.")}`, "g"))]
      .map(match => match[1])
      .sort(compareVersions);
    const version = versions.at(-1);
    if (!version) {
      throw new Error(`No official ${platform} archive found for Blender ${request.series}.`);
    }

    const archiveName = `blender-${version}-${suffix}`;
    const checksumSource = `${directoryUrl}blender-${version}.sha256`;
    const checksums = await fetchText(checksumSource);
    const checksumEntry = checksums.split(/\r?\n/)
      .map(line => line.trim().split(/\s+/))
      .find(parts => parts.length === 2 && parts[1] === archiveName && /^[a-fA-F0-9]{64}$/.test(parts[0]));
    if (!checksumEntry) {
      throw new Error(`Official checksum missing for ${archiveName}.`);
    }

    resolved.push({
      name: request.name,
      series: request.series,
      version,
      archiveName,
      archiveSha256: checksumEntry[0].toLowerCase(),
      source: `${directoryUrl}${archiveName}`,
      checksumSource
    });
  }
  return resolved;
}

export function deduplicateBuilds(lanes) {
  const builds = new Map();
  for (const lane of lanes) {
    const key = lane.archiveSha256;
    const existing = builds.get(key);
    if (existing) {
      existing.requestedLanes.push(lane.name);
    } else {
      builds.set(key, { ...lane, requestedLanes: [lane.name] });
    }
  }
  return [...builds.values()];
}

export function buildEvidence({
  matrix,
  platform,
  os,
  builds,
  earthToolCommit,
  dotnetVersion,
  failure
}) {
  const outputs = builds.flatMap(build => build.outputs ?? []);
  const ownershipOracles = [...new Set(builds.flatMap(build =>
    (build.outputs ?? []).map(output => output.domain)))].sort();
  return {
    format: "earthtool.blender-qualification-evidence",
    version: 1,
    requestedLanes: matrix.requestedLanes,
    platform,
    os,
    outcome: failure ? "failed" : "passed",
    failure: failure ?? null,
    earthToolCommit: earthToolCommit ?? null,
    dotnetVersion: dotnetVersion ?? null,
    addon: "stock io_scene_gltf2",
    blenderOptions: outputs.length === 0 ? null : {
      import: [...new Set(outputs.flatMap(output => output.options.import))].sort(),
      export: [...new Set(outputs.flatMap(output => output.options.export))].sort()
    },
    validatorPolicies: [
      "SharpGLTF strict validation",
      "Khronos glTF Validator 2.0.0-dev.3.10: zero errors and zero warnings"
    ],
    ownershipOracles,
    builds
  };
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
    child.stdout?.on("data", data => { stdout += data; process.stdout.write(data); });
    child.stderr?.on("data", data => { stderr += data; process.stderr.write(data); });
    child.on("error", reject);
    child.on("close", code => {
      if (code === 0 || options.allowFailure) {
        resolve({ code, stdout, stderr });
      } else {
        reject(new Error(`${executable} exited with ${code}.`));
      }
    });
  });
}

async function sha256(filePath) {
  const hash = createHash("sha256");
  const file = await import("node:fs").then(module => module.createReadStream(filePath));
  for await (const chunk of file) {
    hash.update(chunk);
  }
  return hash.digest("hex");
}

async function download(url, destination) {
  try {
    await access(destination);
    return;
  } catch {
    // Download below.
  }
  const response = await fetch(url);
  if (!response.ok || !response.body) {
    throw new Error(`GET ${url} failed with HTTP ${response.status}`);
  }
  await pipeline(response.body, createWriteStream(destination));
}

async function findExecutable(directory, fileName) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const candidate = path.join(directory, entry.name);
    if (entry.isFile() && entry.name.toLowerCase() === fileName.toLowerCase()) {
      return candidate;
    }
    if (entry.isDirectory()) {
      const nested = await findExecutable(candidate, fileName);
      if (nested) {
        return nested;
      }
    }
  }
  return null;
}

async function installBuild(build, cacheDirectory, platform) {
  await mkdir(cacheDirectory, { recursive: true });
  const archivePath = path.join(cacheDirectory, build.archiveName);
  await download(build.source, archivePath);
  const actualChecksum = await sha256(archivePath);
  if (actualChecksum !== build.archiveSha256) {
    throw new Error(
      `Checksum mismatch for ${build.archiveName}: expected ${build.archiveSha256}, got ${actualChecksum}.`);
  }

  const extractDirectory = path.join(cacheDirectory, build.archiveSha256.slice(0, 16));
  await rm(extractDirectory, { recursive: true, force: true });
  await mkdir(extractDirectory, { recursive: true });
  await run("tar", ["-xf", archivePath, "-C", extractDirectory]);
  const executable = await findExecutable(
    extractDirectory,
    platform === "windows-x64" ? "blender.exe" : "blender");
  if (!executable) {
    throw new Error(`Blender executable missing from ${build.archiveName}.`);
  }
  return executable;
}

async function readBlenderProvenance(executable, expectedVersion) {
  const versionResult = await run(executable, ["--version"]);
  const version = versionResult.stdout.match(/^Blender\s+(\d+\.\d+\.\d+)/m)?.[1];
  const buildHash = versionResult.stdout.match(/^\s*build hash:\s*(\S+)/m)?.[1];
  if (version !== expectedVersion || !/^[a-f0-9]{7,40}$/i.test(buildHash ?? "")) {
    throw new Error(
      `Blender provenance mismatch: expected ${expectedVersion}, got ${version ?? "unknown"}.`);
  }

  const addonResult = await run(executable, [
    "--background",
    "--factory-startup",
    "--python-expr",
    "import io_scene_gltf2; print('EARTHTOOL_ADDON_VERSION=' + '.'.join(str(v) for v in io_scene_gltf2.bl_info['version']))"
  ]);
  const addonVersion = addonResult.stdout.match(/EARTHTOOL_ADDON_VERSION=([^\s]+)/)?.[1];
  if (!addonVersion) {
    throw new Error("Stock io_scene_gltf2 version was not reported.");
  }
  return { buildHash, addonVersion };
}

function readTestCount(trx) {
  const counters = trx.match(/<Counters\s+([^>]+)\/?\s*>/i)?.[1];
  if (!counters) {
    throw new Error("Test result counters are missing from the TRX report.");
  }
  const values = Object.fromEntries(
    [...counters.matchAll(/(\w+)="(\d+)"/g)].map(match => [match[1], Number(match[2])]));
  if (values.failed !== 0
    || values.error !== 0
    || values.timeout !== 0
    || values.aborted !== 0
    || values.notExecuted !== 0) {
    throw new Error("The Blender qualification test report contains non-passing outcomes.");
  }
  return values.passed;
}

export function validateOwnershipEvidence(events) {
  const domains = new Set(events.map(event => event.domain));
  const missing = requiredOwnershipOracles.filter(domain => !domains.has(domain));
  if (missing.length > 0) {
    throw new Error(`Blender ownership evidence is missing: ${missing.join(", ")}.`);
  }
  for (const [domain, expected] of expectedOwnershipOutcomes) {
    const matching = events.filter(event => event.domain === domain);
    if (matching.length !== expected.count
      || matching.some(event => event.package !== expected.package
        || event.earthToolOutcome !== expected.outcome)) {
      throw new Error(`Unexpected Blender ownership evidence for ${domain}.`);
    }
  }
  const optionSets = new Set(events.map(event => JSON.stringify({
    import: event.options.import,
    export: event.options.export.filter(option => !option.startsWith("export_format="))
  })));
  if (optionSets.size !== 1) {
    throw new Error("Blender outputs reported inconsistent import/export options.");
  }
  for (const event of events) {
    if (!/^[a-f0-9]{64}$/.test(event.outputSha256)
      || event.sharpGltfValidation !== "passed"
      || event.khronosValidation !== "passed"
      || !event.earthToolOutcome
      || !Array.isArray(event.options?.import)
      || !Array.isArray(event.options?.export)) {
      throw new Error(`Incomplete Blender ownership evidence for ${event.domain}.`);
    }
  }
  return events.sort((left, right) =>
    left.domain.localeCompare(right.domain)
    || left.package.localeCompare(right.package)
    || left.outputSha256.localeCompare(right.outputSha256));
}

async function readOwnershipEvidence(filePath) {
  const events = (await readFile(filePath, "utf8")).split(/\r?\n/)
    .filter(Boolean)
    .map(line => JSON.parse(line));
  return validateOwnershipEvidence(events);
}

function parseArguments(arguments_) {
  const options = {};
  for (let index = 0; index < arguments_.length; index += 2) {
    const key = arguments_[index];
    if (!key?.startsWith("--") || !arguments_[index + 1]) {
      throw new Error(`Invalid argument: ${key ?? "<missing>"}`);
    }
    options[key.slice(2)] = arguments_[index + 1];
  }
  return options;
}

function currentPlatform() {
  if (hostPlatform() === "linux" && process.arch === "x64") {
    return "linux-x64";
  }
  if (hostPlatform() === "win32" && process.arch === "x64") {
    return "windows-x64";
  }
  throw new Error(`Unsupported host: ${hostPlatform()}-${process.arch}`);
}

async function main() {
  const options = parseArguments(process.argv.slice(2));
  const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
  const matrixPath = path.resolve(options.matrix ?? path.join(root, "test-tools", "blender-matrix.v1.json"));
  const evidencePath = path.resolve(options.evidence ?? path.join(root, "artifacts", "blender-qualification.json"));
  const cacheDirectory = path.resolve(options.cache ?? path.join(homedir(), ".cache", "earthtool-blender"));
  const matrix = JSON.parse((await readFile(matrixPath, "utf8")).replace(/^\uFEFF/, ""));
  const platform = options.platform ?? currentPlatform();
  const completed = [];
  let failure;
  let dotnetVersion;
  let earthToolCommit;
  try {
    const resolved = await resolveMatrix(matrix, platform);
    const builds = deduplicateBuilds(resolved);
    if (options["resolve-only"] === "true") {
      process.stdout.write(JSON.stringify({ requestedLanes: resolved, builds }, null, 2) + "\n");
      return;
    }

    await access(path.join(root, "test-tools", "node_modules", "gltf-validator"));
    dotnetVersion = (await run("dotnet", ["--version"], { cwd: root })).stdout.trim();
    earthToolCommit = (await run("git", ["rev-parse", "HEAD"], { cwd: root })).stdout.trim();
    for (const build of builds) {
      const result = { ...build, outcome: "failed" };
      completed.push(result);
      const executable = await installBuild(build, cacheDirectory, platform);
      const provenance = await readBlenderProvenance(executable, build.version);
      Object.assign(result, provenance);

      const resultsDirectory = path.join(root, "artifacts", "blender-results", build.version);
      await rm(resultsDirectory, { recursive: true, force: true });
      await mkdir(resultsDirectory, { recursive: true });
      const trxName = `blender-${build.version}.trx`;
      const eventsPath = path.join(resultsDirectory, "ownership-events.jsonl");
      const testResult = await run("dotnet", [
        "test",
        "EarthTool.MSH.Tests/EarthTool.MSH.Tests.csproj",
        "--configuration", "Release",
        "--no-build",
        "--filter", "Category=BlenderQualification",
        "--results-directory", resultsDirectory,
        "--logger", `trx;LogFileName=${trxName}`
      ], {
        cwd: root,
        env: {
          ...process.env,
          EARTHTOOL_BLENDER_EXECUTABLE: executable,
          EARTHTOOL_BLENDER_EVIDENCE_EVENTS: eventsPath,
          EARTHTOOL_RUN_KHRONOS_VALIDATOR: "1"
        },
        allowFailure: true
      });
      result.testCount = readTestCount(await readFile(path.join(resultsDirectory, trxName), "utf8"));
      result.outputs = await readOwnershipEvidence(eventsPath);
      if (testResult.code !== 0) {
        throw new Error(`Blender ${build.version} qualification tests failed.`);
      }
      result.outcome = "passed";
    }
  } catch (error) {
    failure = error;
  }

  const evidence = buildEvidence({
    matrix,
    platform,
    os: `${hostPlatform()} ${osRelease()}`,
    builds: completed,
    earthToolCommit,
    dotnetVersion,
    failure: failure instanceof Error ? failure.message : failure
  });
  await mkdir(path.dirname(evidencePath), { recursive: true });
  await writeFile(evidencePath, JSON.stringify(evidence, null, 2) + "\n");
  if (failure) {
    throw failure;
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  });
}
