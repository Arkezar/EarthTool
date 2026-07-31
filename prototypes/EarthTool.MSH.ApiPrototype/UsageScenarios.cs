using EarthTool.MSH.Expert;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Immutable;
using System.Numerics;

namespace EarthTool.MSH.ApiPrototype;

internal static class UsageScenarios
{
  // This file is compiled but not executed. It is the primary review artifact.

  public static async Task ReadAndNavigateWithoutCasts(string path, CancellationToken cancellationToken)
  {
    var result = await new MshReader(NullLogger<MshReader>.Instance).ReadAsync(path, cancellationToken: cancellationToken);
    if (!result.TryGetAsset(out var asset))
    {
      foreach (var diagnostic in result.Diagnostics)
      {
        Console.WriteLine($"{diagnostic.Code.Value} {diagnostic.FieldPath}: {diagnostic.Message}");
      }

      return;
    }

    asset.Match(
      onStatic: asset =>
      {
        PrintSourceObject(asset, asset.SourceObjectTree.Root, 0);
      },
      onDynamic: asset =>
      {
        PrintDynamic(asset.RootDynamicObject, 0);
      });
  }

  public static async Task RewritePreservingLoadedRepresentations(
    Stream input,
    Stream output,
    CancellationToken cancellationToken)
  {
    var read = await new MshReader(NullLogger<MshReader>.Instance).ReadAsync(input, cancellationToken: cancellationToken);
    if (!read.TryGetAsset(out var asset))
    {
      return;
    }

    var write = await new MshWriter(NullLogger<MshWriter>.Instance).WriteAsync(output, asset, cancellationToken: cancellationToken);
    Console.WriteLine($"Wrote {write.BytesWritten} bytes with {write.Diagnostics.Length} diagnostics.");
  }

  public static MshBuildResult<StaticMeshAsset> CanonicallyAuthorStaticMesh()
  {
    var vertices = ImmutableArray.Create(
      new CanonicalStaticVertex(new Vector3(-1, 0, 0), Vector3.UnitY, Vector2.Zero, 1, 1),
      new CanonicalStaticVertex(new Vector3(1, 0, 0), Vector3.UnitY, Vector2.UnitX, 2, 1),
      new CanonicalStaticVertex(new Vector3(0, 0, 1), Vector3.UnitY, Vector2.UnitY, 3, 1));
    var triangles = ImmutableArray.Create(new CanonicalTriangle(0, 1, 2));

    var child = new SourceObjectDraft(ImmutableArray.Create<SourceObjectContent>(
      new RenderPartitionDraft("unit_child.tex", vertices, triangles)));
    var root = new SourceObjectDraft(ImmutableArray.Create<SourceObjectContent>(
      new RenderPartitionDraft("unit_base.tex", vertices, triangles),
      new ChildSourceObjectDraft(child),
      new RenderPartitionDraft("unit_detail.tex", vertices, triangles)));

    return StaticMeshBuilder.Create()
      .SetAnimationLengths(a: 1, b: 0, c: 0, d: 0)
      .SetRootSourceObject(root)
      .Build();
  }

  public static MshBuildResult<DynamicMeshAsset> CanonicallyAuthorDynamicMesh()
  {
    var effect = DynamicObjectRecipes.Group(
      DynamicObjectRecipes.ScalableObject("objects/flare.msh", startScale: 0.5f, endScale: 2f));

    return DynamicMeshBuilder.Create(effect).Build();
  }

  public static MshBuildResult<DynamicMeshAsset> ExpertAuthorUnknownDynamicEffect(
    ExpertArchiveFramingInput framing,
    BaseHeader dynamicHeader,
    DynamicExtension extension)
  {
    var unknownEffect = extension with
    {
      EffectType = new DynamicEffectType(unchecked((int)0x81234567)),
      AdditiveFlagRaw = -7
    };
    var root = new DynamicObject(dynamicHeader, unknownEffect, ImmutableArray<DynamicObject>.Empty);

    return MshExpert.CreateDynamic(new ExpertDynamicMeshInput(
      framing,
      root,
      ImmutableArray<byte>.Empty));
  }

  public static MshEditResult<StaticMeshAsset> EditGeometryAndInspectPreservation(
    StaticMeshAsset source,
    StaticRenderObjectId renderObjectId,
    ImmutableArray<CanonicalStaticVertex> vertices,
    ImmutableArray<CanonicalTriangle> triangles)
  {
    var result = source.Edit()
      .ReplaceGeometry(renderObjectId, vertices, triangles)
      .Commit();

    foreach (var change in result.Preservation.Changes)
    {
      Console.WriteLine($"{change.Disposition}: {change.FieldPath} ({change.Reason})");
    }

    return result;
  }

  // A future EarthTool.GLTF service should take StaticMeshAsset directly. No MSH type knows SharpGLTF.
  public static TDocument ExportStatic<TDocument>(StaticMeshAsset asset, IStaticMeshExporter<TDocument> exporter)
    => exporter.Export(asset);

  private static void PrintDynamic(DynamicObject current, int depth)
  {
    Console.WriteLine($"{new string(' ', depth * 2)}{current.Extension.EffectType.Known?.ToString() ?? $"raw:{current.Extension.EffectType.Raw}"}");
    foreach (var child in current.Children)
    {
      PrintDynamic(child, depth + 1);
    }
  }

  private static void PrintSourceObject(StaticMeshAsset asset, SourceObjectNode node, int depth)
  {
    foreach (var id in node.RenderObjects)
    {
      var renderObject = asset.GetRenderObject(id);
      Console.WriteLine($"{new string(' ', depth * 2)}{renderObject.Ordinal}: {renderObject.TexturePath} [{renderObject.Vertices.Length} vertices]");
    }

    foreach (var child in node.Children)
    {
      PrintSourceObject(asset, child, depth + 1);
    }
  }
}

internal interface IStaticMeshExporter<out TDocument>
{
  TDocument Export(StaticMeshAsset asset);
}
