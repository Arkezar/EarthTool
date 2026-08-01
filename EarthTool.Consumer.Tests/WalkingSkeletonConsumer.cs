using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Expert;
using EarthTool.MSH.Operations;
using System.Numerics;

namespace EarthTool.Consumer.Tests;

public static class WalkingSkeletonConsumer
{
  public static MshBuildResult<StaticMeshAsset> BuildStatic(
    Guid creationGuid,
    MeshAssetLineageId lineageId)
  {
    return StaticMeshBuilder.Create(creationGuid, lineageId)
      .SetRenderObject(
        new[]
        {
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY)
        },
        new[] { new CanonicalTriangle(0, 1, 2) })
      .Build();
  }

  public static MshBuildResult<StaticMeshAsset> ConstructExactStatic(
    IEnumerable<byte> serializedRepresentation,
    MeshAssetLineageId lineageId)
  {
    return MshExpert.CreateStatic(serializedRepresentation, lineageId);
  }

  public static MshEditResult<StaticMeshAsset> EditStatic(
    StaticMeshAsset asset,
    IEnumerable<CanonicalStaticVertex> vertices,
    IEnumerable<CanonicalTriangle> triangles)
  {
    return asset.Edit()
      .ReplaceGeometry(asset.StaticRenderObjectSequence[0].Id, vertices, triangles)
      .Commit();
  }

  public static MeshAssetKind GetKind(MeshAsset asset)
  {
    return asset.Match(_ => MeshAssetKind.Static, _ => MeshAssetKind.Dynamic);
  }

  public static async Task<OperationResult> RoundTripAsync(
    IMshReader reader,
    IMshWriter writer,
    GltfInterchange interchange,
    Stream source,
    Stream glb,
    Stream destination,
    CancellationToken cancellationToken)
  {
    var read = await reader.ReadAsync(source, cancellationToken: cancellationToken);
    if (read.Value is not StaticMeshAsset asset)
    {
      return read;
    }

    _ = asset.CommonBaseHeader.AnimationLengths.A;
    _ = asset.CommonBaseHeader.AttachmentTable.Count;
    _ = asset.RootTrailingBytes.Count;

    var export = await interchange.ExportGlbAsync(asset, glb, cancellationToken: cancellationToken);
    if (export.Value is null)
    {
      return export;
    }

    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(
      glb,
      export.Value.Baseline,
      cancellationToken: cancellationToken);
    return import.Value is null
      ? import
      : await writer.WriteAsync(import.Value.Asset, destination, cancellationToken: cancellationToken);
  }
}
