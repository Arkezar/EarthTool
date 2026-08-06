#nullable enable

using EarthTool.MSH.Assets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.GLTF.Internal
{
  internal sealed class GltfStaticIdentityMap
  {
    private readonly IReadOnlyDictionary<StaticRenderObject, int> _renderObjectIds;
    private readonly IReadOnlyDictionary<StaticSourceObject, int> _sourceObjectIds;

    internal IReadOnlyList<int> RenderObjectIds { get; }

    internal IReadOnlyList<int> SourceObjectIds { get; }

    private GltfStaticIdentityMap(
      IReadOnlyDictionary<StaticRenderObject, int> renderObjectIds,
      IReadOnlyDictionary<StaticSourceObject, int> sourceObjectIds,
      IReadOnlyList<int> orderedRenderObjectIds,
      IReadOnlyList<int> orderedSourceObjectIds
    )
    {
      _renderObjectIds = renderObjectIds;
      _sourceObjectIds = sourceObjectIds;
      RenderObjectIds = Array.AsReadOnly(orderedRenderObjectIds.ToArray());
      SourceObjectIds = Array.AsReadOnly(orderedSourceObjectIds.ToArray());
    }

    internal int GetRenderObjectId(StaticRenderObject renderObject)
    {
      return _renderObjectIds.TryGetValue(renderObject, out var id)
        ? id
        : throw new ArgumentException(
          "The static render object does not belong to this glTF identity map.",
          nameof(renderObject)
        );
    }

    internal int GetSourceObjectId(StaticSourceObject sourceObject)
    {
      return _sourceObjectIds.TryGetValue(sourceObject, out var id)
        ? id
        : throw new ArgumentException(
          "The source object does not belong to this glTF identity map.",
          nameof(sourceObject)
        );
    }

    internal static GltfStaticIdentityMap CreateSequential(StaticMeshAsset asset)
    {
      return Create(
        asset,
        Enumerable.Range(1, asset.StaticRenderObjectSequence.Count),
        Enumerable.Range(1, StaticSourceObjectTraversal.Flatten(asset.RootSourceObject).Count())
      );
    }

    internal static GltfStaticIdentityMap Create(
      StaticMeshAsset asset,
      IEnumerable<int> renderObjectIds,
      IEnumerable<int> sourceObjectIds
    )
    {
      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }

      var renderIds = renderObjectIds?.ToArray()
        ?? throw new ArgumentNullException(nameof(renderObjectIds));
      var sourceIds = sourceObjectIds?.ToArray()
        ?? throw new ArgumentNullException(nameof(sourceObjectIds));
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject).ToArray();
      Validate(renderIds, asset.StaticRenderObjectSequence.Count, nameof(renderObjectIds));
      Validate(sourceIds, sources.Length, nameof(sourceObjectIds));
      return new GltfStaticIdentityMap(
        asset.StaticRenderObjectSequence
          .Select((renderObject, index) => (renderObject, id: renderIds[index]))
          .ToDictionary(item => item.renderObject, item => item.id),
        sources
          .Select((sourceObject, index) => (sourceObject, id: sourceIds[index]))
          .ToDictionary(item => item.sourceObject, item => item.id),
        renderIds,
        sourceIds
      );
    }

    private static void Validate(IReadOnlyList<int> ids, int count, string parameterName)
    {
      if (ids.Count != count || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
      {
        throw new ArgumentException(
          "glTF local IDs must be positive, unique, and match the immutable MSH view.",
          parameterName
        );
      }
    }
  }
}
