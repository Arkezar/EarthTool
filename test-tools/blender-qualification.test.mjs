import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

import {
  buildEvidence,
  deduplicateBuilds,
  resolveMatrix,
  validateOwnershipEvidence
} from "./blender-qualification.mjs";

const matrix = JSON.parse((await readFile(
  new URL("./blender-matrix.v1.json", import.meta.url),
  "utf8")).replace(/^\uFEFF/, ""));

const pages = new Map([
  ["https://download.blender.org/release/Blender4.5/", `
    blender-4.5.11-linux-x64.tar.xz
    blender-4.5.11.sha256
    blender-4.5.12-linux-x64.tar.xz
    blender-4.5.12.sha256`],
  ["https://download.blender.org/release/Blender5.2/", `
    blender-5.2.0-linux-x64.tar.xz
    blender-5.2.0.sha256`],
  ["https://download.blender.org/release/Blender4.5/blender-4.5.12.sha256", `
    aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa  blender-4.5.12-linux-x64.tar.xz`],
  ["https://download.blender.org/release/Blender5.2/blender-5.2.0.sha256", `
    bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb  blender-5.2.0-linux-x64.tar.xz`]
]);

async function fetchText(url) {
  assert.ok(pages.has(url), `unexpected URL ${url}`);
  return pages.get(url);
}

test("matrix resolves latest patches and deduplicates identical requested lanes", async () => {
  const resolved = await resolveMatrix(matrix, "linux-x64", fetchText);

  assert.deepEqual(resolved.map(lane => [lane.name, lane.version]), [
    ["latest-4.5-lts", "4.5.12"],
    ["latest-stable", "5.2.0"],
    ["latest-lts", "5.2.0"]
  ]);
  assert.equal(resolved[0].archiveSha256, "a".repeat(64));
  assert.equal(resolved[0].archiveName, "blender-4.5.12-linux-x64.tar.xz");

  const builds = deduplicateBuilds(resolved);
  assert.equal(builds.length, 2);
  assert.deepEqual(builds[1].requestedLanes, ["latest-stable", "latest-lts"]);
});

test("resolution fails when an official checksum is missing", async () => {
  const missingChecksum = new Map(pages);
  missingChecksum.set(
    "https://download.blender.org/release/Blender5.2/blender-5.2.0.sha256",
    "c".repeat(64) + "  blender-5.2.0-windows-x64.zip");

  await assert.rejects(
    resolveMatrix(matrix, "linux-x64", url => missingChecksum.get(url)),
    /checksum.*blender-5\.2\.0-linux-x64\.tar\.xz/i);
});

test("evidence records provenance, options, ownership oracles, and passed builds", () => {
  const evidence = buildEvidence({
    matrix,
    platform: "linux-x64",
    os: "Linux 6.8",
    builds: [{
      version: "5.2.0",
      archiveName: "blender-5.2.0-linux-x64.tar.xz",
      archiveSha256: "b".repeat(64),
      source: "https://download.blender.org/release/Blender5.2/blender-5.2.0-linux-x64.tar.xz",
      checksumSource: "https://download.blender.org/release/Blender5.2/blender-5.2.0.sha256",
      requestedLanes: ["latest-stable", "latest-lts"],
      buildHash: "fbe6228777e7",
      addonVersion: "5.2.17",
      testCount: 337,
      outputs: [{
        domain: "ambiguity",
        package: "glb",
        outputSha256: "c".repeat(64),
        sharpGltfValidation: "passed",
        khronosValidation: "passed",
        earthToolOutcome: "ETG2009",
        options: {
          import: ["import_scene_extras=true"],
          export: ["export_extras=true"]
        },
        preservation: []
      }],
      outcome: "passed"
    }]
  });

  assert.equal(evidence.format, "earthtool.blender-qualification-evidence");
  assert.equal(evidence.version, 1);
  assert.deepEqual(evidence.requestedLanes, matrix.requestedLanes);
  assert.equal(evidence.builds[0].requestedLanes.length, 2);
  assert.equal(evidence.builds[0].outcome, "passed");
  assert.ok(evidence.blenderOptions.import.includes("import_scene_extras=true"));
  assert.ok(evidence.blenderOptions.export.includes("export_extras=true"));
  assert.ok(evidence.ownershipOracles.includes("ambiguity"));
  assert.equal(evidence.addon, "stock io_scene_gltf2");
});

test("ownership evidence rejects a lane that did not execute every required scenario", () => {
  assert.throws(() => validateOwnershipEvidence([{
    domain: "geometry",
    package: "glb",
    outputSha256: "d".repeat(64),
    sharpGltfValidation: "passed",
    khronosValidation: "passed",
    earthToolOutcome: "Succeeded",
    options: { import: [], export: [] },
    preservation: []
  }]), /missing:.*no-edit-glb.*ambiguity/i);
});
