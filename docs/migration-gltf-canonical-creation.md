# Migrating to canonical glTF creation

Public glTF creation now regenerates static and dynamic MSH assets from the
current GLB or separate glTF package. It no longer restores or falls back to an
embedded source MSH.

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

Remove code that supplies a source asset, expected baseline, lineage or document
identity, scope mapping, restoration policy, or preservation conflict action.
There are no forwarding overloads for those contracts.

## Export results

Export methods now return `OperationResult`. Read `Status`, `Succeeded`, and
`Diagnostics`; do not expect an export receipt, baseline, projection fingerprint,
or preservation report.

## CLI reports

The `earthtool.msh.cli-report` schema is version 2. Export, import, and validation
operations contain status and diagnostics. Successful import operations also
contain `assetKind` and `identities.meshCreationGuid`. Version 1 lineage,
fingerprint, conflict-action, restoration-path, and preservation fields were
removed.

## Package authoring

EarthTool exports canonical owner names (`ET_Static_{n}` and
`ET_Dynamic_{n}_{effect}`) and canonical authoring envelopes for typed values.
Third-party packages must use the canonical owner and helper naming contracts.
Values that have no portable glTF representation, including game resource keys,
still require `GltfNewModelImportOptions` or a typed plan. Successful creation
assigns a new creation GUID and may produce different bytes even when the
authored semantics are unchanged.
