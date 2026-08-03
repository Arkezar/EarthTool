import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { once } from "node:events";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { hasIssues, parseOptions, summarizeValidatorReport, validateFile } from "./validate-glb.mjs";

test("validator summary counts every severity and sorts the code histogram", () => {
  const summary = summarizeValidatorReport({
    issues: {
      numErrors: 1,
      numWarnings: 2,
      numInfos: 3,
      numHints: 4,
      messages: [
        { code: "Z_CODE" },
        { code: "A_CODE" },
        { code: "Z_CODE" }
      ]
    }
  }, "2.0.0-dev.3.10");

  assert.deepEqual(summary, {
    errors: 1,
    warnings: 2,
    infos: 3,
    hints: 4,
    codes: [
      { code: "A_CODE", count: 1 },
      { code: "Z_CODE", count: 2 }
    ],
    validatorVersion: "2.0.0-dev.3.10"
  });
});

test("validator options support one-shot and private server forms", () => {
  assert.deepEqual(parseOptions(["asset.glb", "--fail-on", "any"]), {
    path: "asset.glb",
    "fail-on": "any"
  });
  assert.deepEqual(parseOptions(["--server", "true", "--summary-only", "true"]), {
    server: "true",
    "summary-only": "true"
  });
  assert.throws(() => parseOptions(["--server"]), /invalid validator argument/i);
});

test("validator policy fails errors and warnings while retaining lower severities", () => {
  assert.equal(hasIssues({ errors: 0, warnings: 0, infos: 2, hints: 1 }, "errors-and-warnings"), false);
  assert.equal(hasIssues({ errors: 0, warnings: 1, infos: 0, hints: 0 }, "errors-and-warnings"), true);
  assert.equal(hasIssues({ errors: 0, warnings: 0, infos: 1, hints: 0 }, "any"), true);
});

test("validator file and private server failures remain path-free", async () => {
  await assert.rejects(validateFile("missing-private-package.glb", { summaryOnly: true }));

  const child = spawn(process.execPath, [
    fileURLToPath(new URL("./validate-glb.mjs", import.meta.url)),
    "--server", "true",
    "--summary-only", "true"
  ]);
  let stdout = "";
  let stderr = "";
  child.stdout.on("data", chunk => { stdout += chunk; });
  child.stderr.on("data", chunk => { stderr += chunk; });
  child.stdin.end(JSON.stringify({ path: "missing-private-package.glb" }) + "\n");
  await once(child, "close");

  const response = JSON.parse(stdout.trim());
  assert.equal(response.passed, false);
  assert.equal(response.failure, "validator-execution");
  assert.ok(!stdout.includes("missing-private-package"));
  assert.equal(stderr, "");
});
