import fs from "node:fs";
import pathModule from "node:path";
import readline from "node:readline";
import { fileURLToPath } from "node:url";
import validator from "gltf-validator";

export function summarizeValidatorReport(report, validatorVersion = validator.version()) {
  const histogram = new Map();
  for (const issue of report.issues?.messages ?? []) {
    const code = issue.code ?? "UNSPECIFIED";
    histogram.set(code, (histogram.get(code) ?? 0) + 1);
  }

  return {
    errors: report.issues?.numErrors ?? 0,
    warnings: report.issues?.numWarnings ?? 0,
    infos: report.issues?.numInfos ?? 0,
    hints: report.issues?.numHints ?? 0,
    codes: [...histogram]
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([code, count]) => ({ code, count })),
    validatorVersion
  };
}

export async function validateFile(filePath, { summaryOnly = false } = {}) {
  const format = pathModule.extname(filePath).toLowerCase() === ".glb" ? "glb" : "gltf";
  const report = await validator.validateBytes(
    new Uint8Array(fs.readFileSync(filePath)),
    {
      uri: summaryOnly ? `asset.${format}` : filePath,
      format,
      maxIssues: 0,
      writeTimestamp: false,
      externalResourceFunction: async uri => new Uint8Array(
        fs.readFileSync(pathModule.resolve(pathModule.dirname(filePath), decodeURIComponent(uri))))
    });
  return { report, summary: summarizeValidatorReport(report) };
}

export function parseOptions(arguments_) {
  if (arguments_.length === 0) {
    return {};
  }
  const hasPath = !arguments_[0].startsWith("--");
  const options = hasPath ? { path: arguments_[0] } : {};
  for (let index = hasPath ? 1 : 0; index < arguments_.length; index += 2) {
    const key = arguments_[index];
    if (!key?.startsWith("--") || arguments_[index + 1] === undefined) {
      throw new Error("Invalid validator argument.");
    }
    options[key.slice(2)] = arguments_[index + 1];
  }
  return options;
}

export function hasIssues(summary, failOn) {
  if (failOn === "any") {
    return summary.errors + summary.warnings + summary.infos + summary.hints !== 0;
  }
  return summary.errors + summary.warnings !== 0;
}

async function runServer(failOn) {
  const input = readline.createInterface({ input: process.stdin, crlfDelay: Infinity });
  for await (const line of input) {
    if (!line) {
      continue;
    }
    try {
      const request = JSON.parse(line);
      if (typeof request.path !== "string" || request.path.length === 0) {
        throw new Error("A validator path is required.");
      }
      const { summary } = await validateFile(request.path, { summaryOnly: true });
      process.stdout.write(JSON.stringify({
        ...summary,
        passed: !hasIssues(summary, failOn)
      }) + "\n");
    } catch {
      process.stdout.write(JSON.stringify({
        errors: 0,
        warnings: 0,
        infos: 0,
        hints: 0,
        codes: [],
        validatorVersion: validator.version(),
        passed: false,
        failure: "validator-execution"
      }) + "\n");
    }
  }
}

async function main() {
  const options = parseOptions(process.argv.slice(2));
  const failOn = options["fail-on"] ?? "errors-and-warnings";
  if (options.server === "true") {
    await runServer(failOn);
    return;
  }
  if (!options.path) {
    console.error("Usage: node validate-glb.mjs <file.glb|file.gltf> [--fail-on any] [--summary-only true]");
    process.exitCode = 2;
    return;
  }

  const summaryOnly = options["summary-only"] === "true";
  try {
    const { report, summary } = await validateFile(options.path, { summaryOnly });
    console.log(JSON.stringify(summary));
    if (hasIssues(summary, failOn)) {
      if (!summaryOnly) {
        console.error(JSON.stringify(report, null, 2));
      }
      process.exitCode = 1;
    }
  } catch (error) {
    console.error(summaryOnly
      ? "glTF validation could not complete."
      : error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}

if (process.argv[1] && pathModule.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  await main();
}
