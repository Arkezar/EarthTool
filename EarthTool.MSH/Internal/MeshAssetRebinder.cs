#nullable enable

using EarthTool.MSH.Assets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.MSH.Internal
{
  internal sealed class StaticMeshIdentityState
  {
    internal MeshAssetLineageId LineageId { get; }
    internal IReadOnlyList<StaticRenderObjectId> StaticRenderObjectIds { get; }
    internal IReadOnlyList<SourceObjectId> SourceObjectIds { get; }
    internal int? NextStaticRenderObjectLocalId { get; }
    internal int? NextSourceObjectLocalId { get; }

    internal StaticMeshIdentityState(
      MeshAssetLineageId lineageId,
      IEnumerable<StaticRenderObjectId> staticRenderObjectIds,
      IEnumerable<SourceObjectId> sourceObjectIds,
      int? nextStaticRenderObjectLocalId,
      int? nextSourceObjectLocalId)
    {
      LineageId = lineageId;
      StaticRenderObjectIds = Array.AsReadOnly(
        (staticRenderObjectIds ?? throw new ArgumentNullException(nameof(staticRenderObjectIds)))
          .ToArray());
      SourceObjectIds = Array.AsReadOnly(
        (sourceObjectIds ?? throw new ArgumentNullException(nameof(sourceObjectIds))).ToArray());
      NextStaticRenderObjectLocalId = nextStaticRenderObjectLocalId;
      NextSourceObjectLocalId = nextSourceObjectLocalId;
    }

    internal static StaticMeshIdentityState ForLineage(
      StaticMeshAsset asset,
      MeshAssetLineageId lineageId)
    {
      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }

      return new StaticMeshIdentityState(
        lineageId,
        asset.StaticRenderObjectSequence.Select(item =>
          new StaticRenderObjectId(lineageId, item.LocalId)),
        MeshAssetRebinder.EnumerateSourceObjects(asset.RootSourceObject).Select(item =>
          new SourceObjectId(lineageId, item.Id.Value)),
        asset.NextStaticRenderObjectLocalId,
        asset.NextSourceObjectLocalId);
    }
  }

  internal static class MeshAssetRebinder
  {
    internal static StaticMeshAsset RebindStatic(
      StaticMeshAsset asset,
      MeshAssetOrigin origin,
      StaticMeshIdentityState identityState)
    {
      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }
      if (identityState is null)
      {
        throw new ArgumentNullException(nameof(identityState));
      }

      var sourceObjects = EnumerateSourceObjects(asset.RootSourceObject).ToArray();
      ValidateDecodedCorrespondence(asset, sourceObjects);
      ValidateIdentityState(asset, sourceObjects, identityState);

      var renderObjectIds = asset.StaticRenderObjectSequence
        .Select((item, index) => (item.Id, identityState.StaticRenderObjectIds[index]))
        .ToDictionary(item => item.Id, item => item.Item2);
      var sourceObjectIds = sourceObjects
        .Select((item, index) => (item.Id, identityState.SourceObjectIds[index]))
        .ToDictionary(item => item.Id, item => item.Item2);
      var renderObjects = asset.StaticRenderObjectSequence.Select(item =>
        new StaticRenderObject(
          renderObjectIds[item.Id],
          sourceObjectIds[item.SourceObjectId],
          item.RenderVertices,
          item.Triangles,
          item.VertexBlockCount,
          item.VertexBlockPadding,
          item.ObjectFlags,
          item.TexturePathBytes,
          item.AnimationTracks,
          item.AnimationClassValue,
          item.Pivot,
          item.BarrelMaximumAngle,
          item.NextRecordMarker,
          item.GetSerializedRepresentation())).ToArray();
      var rootSourceObject = RebindSourceObject(
        asset.RootSourceObject,
        sourceObjectIds,
        renderObjectIds);

      return new StaticMeshAsset(
        identityState.LineageId,
        asset.ArchiveFraming,
        asset.CommonBaseHeader,
        asset.RootTrailingBytes.ToArray(),
        renderObjects,
        asset.GetSerializedRepresentation(),
        origin,
        rootSourceObject,
        asset.StoredTrailingHierarchyUnwindCount,
        asset.ExpectedTrailingHierarchyUnwindCount,
        identityState.NextStaticRenderObjectLocalId,
        identityState.NextSourceObjectLocalId);
    }

    internal static DynamicMeshAsset RebindDynamic(
      DynamicMeshAsset asset,
      MeshAssetOrigin origin,
      MeshAssetLineageId lineageId)
    {
      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }

      return new DynamicMeshAsset(
        lineageId,
        asset.ArchiveFraming,
        asset.CommonBaseHeader,
        asset.RootDynamicObject,
        asset.RootTrailingBytes.ToArray(),
        asset.GetSerializedRepresentation(),
        origin);
    }

    private static void ValidateDecodedCorrespondence(
      StaticMeshAsset asset,
      IReadOnlyList<StaticSourceObject> sourceObjects)
    {
      var sequenceById = UniqueById(
        asset.StaticRenderObjectSequence,
        item => item.Id,
        "The decoded static render-object sequence contains duplicate identities.");
      var sourceById = UniqueById(
        sourceObjects,
        item => item.Id,
        "The decoded source-object tree contains duplicate identities.");
      var sequenceIndexes = asset.StaticRenderObjectSequence
        .Select((item, index) => (item.Id, Index: index))
        .ToDictionary(item => item.Id, item => item.Index);
      var firstSeenSourceIds = asset.StaticRenderObjectSequence
        .Select(item => item.SourceObjectId)
        .Distinct();
      if (!sourceObjects.Select(item => item.Id).SequenceEqual(firstSeenSourceIds))
      {
        throw InvalidState(
          "The decoded source-object tree does not correspond to serialized source-object order.");
      }

      var referencedRenderObjects = new HashSet<StaticRenderObjectId>();
      foreach (var sourceObject in sourceObjects)
      {
        if (!sourceObject.Id.Lineage.Equals(asset.LineageId))
        {
          throw InvalidState("The decoded source-object lineage is inconsistent.");
        }

        var previousSequenceIndex = -1;
        foreach (var renderObjectId in sourceObject.StaticRenderObjectIds)
        {
          if (!sequenceById.TryGetValue(renderObjectId, out var renderObject)
            || !sequenceIndexes.TryGetValue(renderObjectId, out var sequenceIndex)
            || sequenceIndex <= previousSequenceIndex
            || !renderObject.SourceObjectId.Equals(sourceObject.Id)
            || !referencedRenderObjects.Add(renderObjectId))
          {
            throw InvalidState(
              "The decoded source-object tree does not correspond to serialized render-object order.");
          }
          previousSequenceIndex = sequenceIndex;
        }
      }

      foreach (var renderObject in asset.StaticRenderObjectSequence)
      {
        if (!renderObject.Id.Lineage.Equals(asset.LineageId)
          || !renderObject.SourceObjectId.Lineage.Equals(asset.LineageId)
          || !sourceById.ContainsKey(renderObject.SourceObjectId))
        {
          throw InvalidState("The decoded static render-object lineage is inconsistent.");
        }
      }

      if (referencedRenderObjects.Count != asset.StaticRenderObjectSequence.Count)
      {
        throw InvalidState(
          "The decoded source-object tree does not correspond to serialized render-object order.");
      }
    }

    private static void ValidateIdentityState(
      StaticMeshAsset asset,
      IReadOnlyList<StaticSourceObject> sourceObjects,
      StaticMeshIdentityState identityState)
    {
      if (identityState.StaticRenderObjectIds.Count != asset.StaticRenderObjectSequence.Count)
      {
        throw InvalidState(
          "Static render-object identities must match the decoded sequence count.");
      }
      if (identityState.SourceObjectIds.Count != sourceObjects.Count)
      {
        throw InvalidState("Source-object identities must match the reconstructed tree count.");
      }

      ValidateIds(
        identityState.StaticRenderObjectIds,
        item => item.Lineage,
        item => item.Value,
        identityState.LineageId,
        "static render-object");
      ValidateIds(
        identityState.SourceObjectIds,
        item => item.Lineage,
        item => item.Value,
        identityState.LineageId,
        "source-object");
      ValidateNextId(
        identityState.StaticRenderObjectIds.Max(item => item.Value),
        identityState.NextStaticRenderObjectLocalId,
        "static render-object");
      ValidateNextId(
        identityState.SourceObjectIds.Max(item => item.Value),
        identityState.NextSourceObjectLocalId,
        "source-object");
    }

    private static void ValidateIds<T>(
      IEnumerable<T> ids,
      Func<T, MeshAssetLineageId> lineage,
      Func<T, int> value,
      MeshAssetLineageId expectedLineage,
      string name)
    {
      var localIds = new HashSet<int>();
      foreach (var id in ids)
      {
        if (!lineage(id).Equals(expectedLineage))
        {
          throw InvalidState($"Every {name} identity must use the selected lineage.");
        }
        if (value(id) <= 0)
        {
          throw InvalidState($"Every {name} identity must be positive.");
        }
        if (!localIds.Add(value(id)))
        {
          throw InvalidState($"Every {name} identity must be unique.");
        }
      }
    }

    private static void ValidateNextId(int maximum, int? next, string name)
    {
      if (next.HasValue)
      {
        if (next.Value <= maximum)
        {
          throw InvalidState(
            $"The next {name} identity must exceed every allocated identity.");
        }
        return;
      }

      if (maximum != int.MaxValue)
      {
        throw InvalidState(
          $"The next {name} identity can be exhausted only after allocating Int32.MaxValue.");
      }
    }

    private static StaticSourceObject RebindSourceObject(
      StaticSourceObject source,
      IReadOnlyDictionary<SourceObjectId, SourceObjectId> sourceObjectIds,
      IReadOnlyDictionary<StaticRenderObjectId, StaticRenderObjectId> renderObjectIds)
    {
      return new StaticSourceObject(
        sourceObjectIds[source.Id],
        source.StaticRenderObjectIds.Select(item => renderObjectIds[item]),
        source.Children.Select(child => RebindSourceObject(child, sourceObjectIds, renderObjectIds)));
    }

    private static Dictionary<TId, T> UniqueById<T, TId>(
      IEnumerable<T> values,
      Func<T, TId> id,
      string message)
      where TId : notnull
    {
      var result = new Dictionary<TId, T>();
      foreach (var value in values)
      {
        if (!result.TryAdd(id(value), value))
        {
          throw InvalidState(message);
        }
      }
      return result;
    }

    internal static IEnumerable<StaticSourceObject> EnumerateSourceObjects(StaticSourceObject source)
    {
      yield return source;
      foreach (var child in source.Children)
      {
        foreach (var descendant in EnumerateSourceObjects(child))
        {
          yield return descendant;
        }
      }
    }

    private static ArgumentException InvalidState(string message)
    {
      return new ArgumentException(message, "identityState");
    }
  }
}
