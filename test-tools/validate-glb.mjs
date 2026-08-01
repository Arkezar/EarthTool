import fs from "node:fs";
import pathModule from "node:path";
import validator from "gltf-validator";

const path = process.argv[2];
if (!path) {
  console.error("Usage: node validate-glb.mjs <file.glb|file.gltf>");
  process.exit(2);
}

const format = pathModule.extname(path).toLowerCase() === ".glb" ? "glb" : "gltf";
const report = await validator.validateBytes(
  new Uint8Array(fs.readFileSync(path)),
  {
    uri: path,
    format,
    maxIssues: 0,
    writeTimestamp: false,
    externalResourceFunction: async (uri) => new Uint8Array(
      fs.readFileSync(pathModule.resolve(pathModule.dirname(path), decodeURIComponent(uri))))
  });

const errors = report.issues.numErrors;
const warnings = report.issues.numWarnings;
console.log(JSON.stringify({ errors, warnings, validatorVersion: validator.version() }));
if (errors !== 0 || warnings !== 0) {
  console.error(JSON.stringify(report, null, 2));
  process.exit(1);
}
