using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;

namespace EarthTool.Consumer.Tests;

public static class WalkingSkeletonConsumer
{
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
