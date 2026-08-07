# Migrating to canonical glTF creation

Every public glTF creation route now regenerates a static or dynamic canonical
MSH from the current GLB or separate glTF package. It never restores or falls
back to an embedded source MSH.

## Creation results

Replace preservation-oriented creation results with `OperationResult<MeshAsset>`:

```csharp
var result = await interchange.CreateMeshAsync(glbStream);
if (result.Succeeded)
{
  MeshAsset asset = result.Value!;
}
```

Use `CreateMeshFileAsync` for a separate `.gltf` package. Planned callers use
`CreateMeshWithPlanAsync` or `CreateMeshFileWithPlanAsync` and receive the same
result shape.

Remove code that supplies a source asset, expected baseline, lineage, document or
scope identity, projection fingerprint, guard, restoration policy, preservation
policy, conflict inventory, or conflict action. There are no forwarding
overloads or compatibility interpretation for those contracts.

## Export results

Export methods now return `OperationResult`. Read `Status`, `Succeeded`, and
`Diagnostics`; do not expect an export receipt, baseline, projection fingerprint,
or preservation report.

Export can succeed with `ETG1030` warnings. Each warning identifies a source-only
serialized representation that the glTF projection cannot carry and later
canonical creation will not retain.

## CLI reports

The `earthtool.msh.cli-report` schema is version 2. Export, canonical import, and
validation operations contain status and diagnostics. Successful import
operations also contain `assetKind` and `identities.meshCreationGuid`. Reports
have no edit mode, lineage, fingerprint, conflict-action, restoration-path,
preservation, or receipt fields.

## Package authoring

EarthTool exports only `extras.earthtoolAuthoring` string envelopes with format
`earthtool.msh.authoring`, version 1. Envelopes contain typed authoring values and
are read only from strict case-sensitive canonical named owners such as
`ET_Static_{n}`, `ET_Turret_{n}`, canonical static-light names, and
`ET_Dynamic_{n}_{Effect}`.

Legacy `extras.earthtool`, source MSH payloads, serialized representations,
identities, fingerprints, guards, inventories, and preservation data are ignored.
Do not migrate them into the new envelope.

TEX and MSH resource bindings are embedded by export as material and
dynamic-node custom properties and consumed by a returning import as defaults.
Explicit `GltfNewModelImportOptions` or version-3 import-plan values override
the embedded keys. Identities are never inferred from names, URIs, image bytes,
or preview geometry. Successful creation assigns a new creation GUID and may
produce different bytes even when authored semantics are unchanged.

## Plans and limits

Version-3 plans carry typed options for the imported package. Old edit-mode
plans and version-2 source bindings are rejected. Plans are no longer bound to a
source-package SHA-256 digest.

`GltfOperationProfile` now bounds canonical envelope count, bytes per envelope,
aggregate bytes, JSON depth and elements, unknown members, and emitted warning
diagnostics. Metadata limit failures report `ETG2005`.
