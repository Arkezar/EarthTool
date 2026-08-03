export const corpusBinaryStages = Object.freeze([
  "msh.read",
  "msh.validate",
  "msh.write",
  "msh.semantic-equivalence",
  "msh.canonical-idempotence"
]);

export const corpusInterchangeStages = Object.freeze([
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
]);

export const recognizedDynamicEffectTypes = Object.freeze([
  "Group",
  "Explosion",
  "Track",
  "ScalableObject",
  "MappedExplosion",
  "FlatExplosion",
  "Laser",
  "LaserWall",
  "Shockwave",
  "Line",
  "Sphere",
  "ElectricalCannon",
  "Lightning",
  "Smoke",
  "Keelwater"
]);
