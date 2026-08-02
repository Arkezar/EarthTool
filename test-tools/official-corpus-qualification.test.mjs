import assert from "node:assert/strict";
import test from "node:test";

import {
  assertPrivacySafe,
  buildEvidence,
  renderProgress,
  run,
  shouldReportProgress,
  validateQualificationSummary
} from "./official-corpus-qualification.mjs";

const fingerprint = "a".repeat(64);

function operation(stage, attempted) {
  return { stage, attempted, passed: attempted, failed: 0 };
}

function passingSummary() {
  return {
    format: "earthtool.official-msh-corpus-event",
    version: 1,
    corpus: {
      fingerprintAlgorithm: "sha256-content-multiset-v1",
      fingerprint,
      discoveredMshFiles: 3,
      excludedNonFramedOrUnsupported: 0,
      excludedByProfile: 0,
      assets: 3,
      staticAssets: 2,
      dynamicAssets: 1,
      inputBytes: 300
    },
    operations: [
      operation("msh.read", 3),
      operation("msh.validate", 3),
      operation("msh.write", 3),
      operation("msh.semantic-equivalence", 3),
      operation("msh.canonical-idempotence", 3),
      operation("glb.export", 2),
      operation("glb.sharp-gltf-validate", 2),
      operation("glb.khronos-validate", 2),
      operation("glb.unchanged-import", 2),
      operation("glb.canonical-baseline", 2),
      operation("gltf.export", 2),
      operation("gltf.sharp-gltf-validate", 2),
      operation("gltf.khronos-validate", 2),
      operation("gltf.unchanged-import", 2),
      operation("gltf.canonical-baseline", 2),
      operation("glb.cli-export", 2),
      operation("glb.cli-sharp-gltf-validate", 2),
      operation("glb.cli-khronos-validate", 2),
      operation("glb.cli-unchanged-import", 2),
      operation("gltf.cli-export", 2),
      operation("gltf.cli-sharp-gltf-validate", 2),
      operation("gltf.cli-khronos-validate", 2),
      operation("gltf.cli-unchanged-import", 2)
    ],
    bytes: {
      canonicalMsh: 300,
      glb: 500,
      gltfManifest: 200,
      gltfSidecars: 400,
      unchangedImportedMsh: 400,
      cliPackages: 1100,
      cliImportedMsh: 400
    },
    diagnostics: [],
    validators: {
      khronos: {
        version: "2.0.0-dev.3.10",
        packages: 8,
        errors: 0,
        warnings: 0,
        infos: 0,
        hints: 0,
        codes: []
      }
    },
    failures: { total: 0, categories: [] },
    profiles: {
      msh: { maxInputBytes: 1 },
      gltf: { maxInputBytes: 2 }
    }
  };
}

function profile() {
  return {
    format: "earthtool.official-msh-corpus-profile",
    version: 1,
    corpus: {
      fingerprint,
      discoveredMshFiles: 3,
      excludedNonFramedOrUnsupported: 0,
      excludedByProfile: 0,
      assets: 3,
      staticAssets: 2,
      dynamicAssets: 1
    },
    diagnostics: [],
    validators: {
      khronos: { infos: 0, hints: 0, codes: [] }
    }
  };
}

test("qualification summary requires every oracle with exact aggregate counts", () => {
  const summary = passingSummary();

  assert.equal(validateQualificationSummary(summary, profile()), summary);

  summary.operations = summary.operations.filter(item => item.stage !== "gltf.unchanged-import");
  assert.throws(
    () => validateQualificationSummary(summary, profile()),
    error => error.category === "oracle-inventory");
});

test("qualification summary rejects duplicate oracle stages", () => {
  const summary = passingSummary();
  summary.operations.push({ ...summary.operations[0] });

  assert.throws(
    () => validateQualificationSummary(summary, profile()),
    error => error.category === "oracle-inventory");
});

test("qualification summary rejects operation failures and validator issues", () => {
  const failedOperation = passingSummary();
  failedOperation.operations[0] = {
    stage: "msh.read",
    attempted: 3,
    passed: 2,
    failed: 1
  };
  assert.throws(
    () => validateQualificationSummary(failedOperation, profile()),
    error => error.category === "oracle-failure");

  const validatorIssue = passingSummary();
  validatorIssue.validators.khronos.warnings = 1;
  validatorIssue.validators.khronos.codes = [{ code: "UNEXPECTED", count: 1 }];
  assert.throws(
    () => validateQualificationSummary(validatorIssue, profile()),
    error => error.category === "validator-issue");
});

test("qualification summary requires an exact profile fingerprint", () => {
  const missingFingerprint = profile();
  delete missingFingerprint.corpus.fingerprint;
  assert.throws(
    () => validateQualificationSummary(passingSummary(), missingFingerprint),
    error => error.category === "corpus-mismatch");

  const changedFingerprint = profile();
  changedFingerprint.corpus.fingerprint = "b".repeat(64);
  assert.throws(
    () => validateQualificationSummary(passingSummary(), changedFingerprint),
    error => error.category === "corpus-mismatch");
});

test("qualification summary rejects lower-severity validator drift", () => {
  const summary = passingSummary();
  summary.validators.khronos.infos = 1;
  summary.validators.khronos.codes = [{ code: "INFORMATION", count: 1 }];

  assert.throws(
    () => validateQualificationSummary(summary, profile()),
    error => error.category === "validator-drift");
});

test("qualification summary requires explicit diagnostic and validator histograms", () => {
  const missingDiagnostics = profile();
  delete missingDiagnostics.diagnostics;
  assert.throws(
    () => validateQualificationSummary(passingSummary(), missingDiagnostics),
    error => error.category === "evidence-contract");

  const missingCodes = profile();
  delete missingCodes.validators.khronos.codes;
  assert.throws(
    () => validateQualificationSummary(passingSummary(), missingCodes),
    error => error.category === "validator-drift");
});

test("qualification summary rejects every error and aggregate diagnostic drift", () => {
  const errorSummary = passingSummary();
  errorSummary.diagnostics = [{
    stage: "msh.read",
    code: "ETM1003",
    eventId: 1003,
    severity: "Error",
    count: 1
  }];
  assert.throws(
    () => validateQualificationSummary(errorSummary, profile()),
    error => error.category === "operation-diagnostic");

  const warningSummary = passingSummary();
  warningSummary.diagnostics = [{
    stage: "msh.read",
    code: "ETM1009",
    eventId: 1009,
    severity: "Warning",
    count: 2
  }];
  assert.throws(
    () => validateQualificationSummary(warningSummary, profile()),
    error => error.category === "diagnostic-drift");
});

test("qualification summary rejects missing byte and profile evidence", () => {
  const missingBytes = passingSummary();
  delete missingBytes.bytes;
  assert.throws(
    () => validateQualificationSummary(missingBytes, profile()),
    error => error.category === "evidence-contract");

  const missingProfiles = passingSummary();
  delete missingProfiles.profiles;
  assert.throws(
    () => validateQualificationSummary(missingProfiles, profile()),
    error => error.category === "evidence-contract");
});

test("evidence is deterministic and contains only aggregate corpus data", () => {
  const input = {
    summary: passingSummary(),
    profile: profile(),
    platform: "linux-x64",
    os: "Linux 6.8",
    earthToolCommit: "b".repeat(40),
    dotnetVersion: "8.0.407",
    sharpGltfVersion: "1.0.6",
    failureCategory: null
  };

  const first = buildEvidence(input);
  const second = buildEvidence(input);

  assert.deepEqual(first, second);
  assert.equal(first.format, "earthtool.official-msh-qualification-evidence");
  assert.equal(first.outcome, "passed");
  assert.equal(first.corpus.fingerprint, fingerprint);
  assert.equal(first.corpus.assets, 3);
  assert.ok(!JSON.stringify(first).includes("inputPath"));
  assert.ok(!JSON.stringify(first).includes("texture"));
});

test("privacy gate rejects paths, asset names, texture keys, and forbidden values", () => {
  const safe = buildEvidence({
    summary: passingSummary(),
    profile: profile(),
    platform: "linux-x64",
    os: "Linux 6.8",
    earthToolCommit: null,
    dotnetVersion: "8.0.407",
    sharpGltfVersion: "1.0.6",
    failureCategory: null
  });
  assert.doesNotThrow(() => assertPrivacySafe(safe, ["secret-asset.msh"]));
  assert.throws(
    () => assertPrivacySafe({ ...safe, source: "/private/secret-asset.msh" }, ["secret-asset.msh"]),
    error => error.category === "privacy-violation");
  assert.throws(
    () => assertPrivacySafe({ ...safe, texKey: "Textures\\Secret.tex" }),
    error => error.category === "privacy-violation");
});

test("qualification subprocesses terminate at their configured deadline", async () => {
  const result = await run(
    process.execPath,
    ["-e", "setTimeout(() => {}, 1000)"],
    { timeoutMs: 10 });

  assert.equal(result.timedOut, true);
});

test("progress rendering contains only aggregate counts", () => {
  assert.equal(
    renderProgress({
      completed: 575,
      total: 1151,
      staticAssets: 500,
      dynamicAssets: 75,
      failures: 0
    }, 10),
    "Official MSH corpus [#####-----] 49% 575/1151 static=500 dynamic=75 failures=0");
});

test("non-TTY progress reports aggregate milestones and completion", () => {
  assert.equal(shouldReportProgress({ completed: 99, total: 1151 }, 0, false), false);
  assert.equal(shouldReportProgress({ completed: 100, total: 1151 }, 0, false), true);
  assert.equal(shouldReportProgress({ completed: 199, total: 1151 }, 100, false), false);
  assert.equal(shouldReportProgress({ completed: 1151, total: 1151 }, 1100, false), true);
  assert.equal(shouldReportProgress({ completed: 1, total: 1151 }, 0, true), true);
  assert.equal(shouldReportProgress({ completed: 99, total: 1151 }, 0, false, true), true);
});
