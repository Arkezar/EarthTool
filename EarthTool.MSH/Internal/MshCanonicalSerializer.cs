#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Internal
{
  internal sealed class StaticSourceObjectSerializationPlan
  {
    internal int SourceObjectOrdinal { get; }

    internal IReadOnlyList<int> RenderObjectOrdinals { get; }

    internal IReadOnlyList<StaticSourceObjectSerializationPlan> Children { get; }

    internal StaticSourceObjectSerializationPlan(
      int sourceObjectOrdinal,
      IEnumerable<int> renderObjectOrdinals,
      IEnumerable<StaticSourceObjectSerializationPlan> children
    )
    {
      SourceObjectOrdinal = sourceObjectOrdinal;
      RenderObjectOrdinals = Array.AsReadOnly(renderObjectOrdinals.ToArray());
      Children = Array.AsReadOnly(children.ToArray());
    }
  }

  internal static class MshCanonicalSerializer
  {
    internal const int StaticRecordSize = 0xDD;
    internal const int DynamicRecordSize = 0x410;
    private const int StaticRecordPivotOffsetFromEnd = 17;
    private static readonly Encoding _dynamicStringEncoding = CreateDynamicStringEncoding();

    internal static byte[] CreateStatic(
      Guid creationGuid,
      AnimationClassBytes animationLengths,
      CanonicalStaticSourceObject rootSourceObject,
      CanonicalStaticFootprint? footprint = null,
      CanonicalHorizontalExtents? horizontalExtents = null,
      IReadOnlyDictionary<int, Vector3>? pivots = null,
      IReadOnlyDictionary<int, StaticAnimationReplacement>? animations = null,
      AnimationClassBytes? animationFrameIndices = null,
      IReadOnlyDictionary<int, byte[]>? attachmentRecords = null,
      IReadOnlyDictionary<int, byte[]>? cannonRenderPositions = null,
      IReadOnlyDictionary<int, byte[]>? staticSpotLights = null,
      IReadOnlyDictionary<int, byte[]>? staticOmniLights = null
    )
    {
      var framing = new MeshArchiveFraming(0x20D0A1FF, null, creationGuid);
      var records = FlattenStaticTree(rootSourceObject);
      var vertices = rootSourceObject
        .RenderObjects.SelectMany(record => record.RenderVertices)
        .ToArray();
      var commonHeader = CommonMeshBaseHeader
        .CreateCanonicalStatic(animationLengths)
        .SerializedRepresentation.ToArray();
      WriteCanonicalStaticHeaderRegions(commonHeader, vertices, footprint, horizontalExtents);
      if (animationFrameIndices.HasValue)
      {
        WriteAnimationClassBytes(commonHeader, 0x14, animationFrameIndices.Value);
      }
      WriteStaticHeaderRecords(
        commonHeader,
        attachmentRecords,
        cannonRenderPositions,
        staticSpotLights,
        staticOmniLights
      );
      return CreateStatic(
        framing,
        commonHeader,
        records,
        Array.Empty<byte>(),
        pivots,
        animations
      );
    }

    internal static long GetCanonicalStaticSerializedLength(
      IEnumerable<(int VertexCount, int TriangleCount)> geometry
    )
    {
      var length = sizeof(uint) + 16L + CommonMeshBaseHeader.SerializedSize + sizeof(uint);
      foreach (var record in geometry)
      {
        var blocks = checked((record.VertexCount + 3L) / 4L);
        length = checked(length + 53L + (blocks * 0xA0L) + (record.TriangleCount * 8L));
      }
      return length;
    }

    private static void WriteStaticHeaderRecords(
      byte[] commonHeader,
      IReadOnlyDictionary<int, byte[]>? attachmentRecords,
      IReadOnlyDictionary<int, byte[]>? cannonRenderPositions,
      IReadOnlyDictionary<int, byte[]>? staticSpotLights,
      IReadOnlyDictionary<int, byte[]>? staticOmniLights
    )
    {
      foreach (var replacement in attachmentRecords ?? new Dictionary<int, byte[]>())
      {
        replacement.Value.CopyTo(commonHeader, 0x1D8 + ((replacement.Key - 1) * 8));
      }
      foreach (var replacement in cannonRenderPositions ?? new Dictionary<int, byte[]>())
      {
        replacement.Value.CopyTo(commonHeader, 0x018 + ((replacement.Key - 1) * 12));
      }
      foreach (var replacement in staticSpotLights ?? new Dictionary<int, byte[]>())
      {
        replacement.Value.CopyTo(commonHeader, 0x048 + ((replacement.Key - 1) * 0x30));
      }
      foreach (var replacement in staticOmniLights ?? new Dictionary<int, byte[]>())
      {
        replacement.Value.CopyTo(commonHeader, 0x108 + ((replacement.Key - 1) * 0x1C));
      }
    }

    internal static byte[] RewriteStatic(
      StaticMeshAsset source,
      IReadOnlyDictionary<int, IReadOnlyList<CanonicalStaticVertex>> vertices,
      IReadOnlyDictionary<int, IReadOnlyList<CanonicalTriangle>> triangles,
      IEnumerable<int>? removedRenderObjects = null,
      IReadOnlyList<StaticRenderObjectAddition>? additions = null,
      StaticSourceObjectSerializationPlan? rootSourceObject = null,
      IReadOnlyList<int>? explicitSequence = null,
      IReadOnlyDictionary<int, Vector3>? pivots = null,
      IReadOnlyDictionary<int, byte[]>? texturePathBytes = null,
      bool canonicalizeNextRecordMarkers = false,
      IReadOnlyDictionary<int, StaticRenderObjectFlags>? markerFlags = null,
      IReadOnlyDictionary<int, StaticAnimationReplacement>? animations = null,
      AnimationClassBytes? animationLengths = null,
      AnimationClassBytes? animationFrameIndices = null,
      IReadOnlyDictionary<int, byte[]>? attachmentRecords = null,
      IReadOnlyDictionary<int, byte[]>? cannonRenderPositions = null,
      IReadOnlyDictionary<int, byte[]>? staticSpotLights = null,
      IReadOnlyDictionary<int, byte[]>? staticOmniLights = null,
      CanonicalHorizontalExtents? horizontalExtents = null
    )
    {
      var archiveHeader = CreateArchiveHeader(source.ArchiveFraming);
      var removed = new HashSet<int>(removedRenderObjects ?? Array.Empty<int>());
      additions ??= Array.Empty<StaticRenderObjectAddition>();
      rootSourceObject ??= CreateOutputAssembly(source);
      pivots ??= new Dictionary<int, Vector3>();
      texturePathBytes ??= new Dictionary<int, byte[]>();
      markerFlags ??= new Dictionary<int, StaticRenderObjectFlags>();
      animations ??= new Dictionary<int, StaticAnimationReplacement>();
      attachmentRecords ??= new Dictionary<int, byte[]>();
      cannonRenderPositions ??= new Dictionary<int, byte[]>();
      staticSpotLights ??= new Dictionary<int, byte[]>();
      staticOmniLights ??= new Dictionary<int, byte[]>();
      var sourceRecords = source.StaticRenderObjectSequence
        .Select((record, ordinal) => (record, ordinal))
        .ToDictionary(item => item.ordinal, item => item.record);
      var addedRecords = additions.ToDictionary(addition => addition.Ordinal);
      var finalRecordSources = new Dictionary<int, int>();
      AddRecordSources(rootSourceObject, finalRecordSources);
      var recordList = new List<RewrittenStaticRecord>();
      var plan = explicitSequence ?? PlanStaticRenderObjectOrdinals(source, removed, additions);
      foreach (var ordinal in plan)
      {
        if (sourceRecords.TryGetValue(ordinal, out var record))
        {
          var pivot = pivots.TryGetValue(ordinal, out var replacementPivot)
            ? replacementPivot
            : record.Pivot;
          var hasReplacementTexturePath = texturePathBytes.TryGetValue(
            ordinal,
            out var replacementTexturePath
          );
          var hasReplacementAnimation = animations.TryGetValue(
            ordinal,
            out var replacementAnimation
          );
          recordList.Add(
            new RewrittenStaticRecord(
              vertices.TryGetValue(ordinal, out var replacementVertices)
                ? RewriteStaticRecord(
                  record,
                  replacementVertices,
                  triangles[ordinal],
                  pivot,
                  hasReplacementTexturePath ? replacementTexturePath : record.TexturePathBytes,
                  hasReplacementAnimation ? replacementAnimation!.Tracks : record.AnimationTracks,
                  hasReplacementAnimation
                    ? replacementAnimation!.ClassValue
                    : record.AnimationClassValue
                )
                : RewriteStaticRecordRepresentations(
                  record,
                  pivot,
                  pivots.ContainsKey(ordinal),
                  hasReplacementTexturePath ? replacementTexturePath : null,
                  hasReplacementAnimation ? replacementAnimation : null
                ),
              finalRecordSources[ordinal]
            )
          );
          continue;
        }

        var addition = addedRecords[ordinal];
        recordList.Add(
          new RewrittenStaticRecord(
            CreateStaticRecord(
              addition.Vertices,
              addition.Triangles,
              addition.TexturePathBytes,
              pivots.TryGetValue(addition.Ordinal, out var additionPivot)
                ? additionPivot
                : Vector3.Zero
            ),
            addition.SourceObjectOrdinal
          )
        );
      }

      var records = recordList.ToArray();
      RewriteSharingLinks(source, records, plan, vertices, removed, additions);
      for (var index = 0; index < plan.Count; index++)
      {
        if (!markerFlags.TryGetValue(plan[index], out var replacement))
        {
          continue;
        }
        var offset = GetObjectFlagsOffset(records[index].Bytes);
        var flags =
          ReadUInt32(records[index].Bytes, offset)
          & ~(uint)StaticRenderObjectFlagMasks.MarkerAttachments;
        WriteUInt32(records[index].Bytes, offset, flags | (uint)replacement);
      }
      var trailingHierarchyUnwindCount = RewriteHierarchyFlags(rootSourceObject, records);
      for (var index = 0; index < records.Length; index++)
      {
        var markerOffset = records[index].Bytes.Length - sizeof(uint);
        var marker = ReadUInt32(records[index].Bytes, markerOffset);
        if (index == records.Length - 1)
        {
          WriteUInt32(records[index].Bytes, markerOffset, 0);
        }
        else if (canonicalizeNextRecordMarkers || marker == 0)
        {
          WriteUInt32(records[index].Bytes, markerOffset, 1);
        }
      }
      var length =
        archiveHeader.Length
        + CommonMeshBaseHeader.SerializedSize
        + sizeof(uint)
        + records.Sum(record => record.Bytes.Length)
        + source.RootTrailingBytes.Count;
      var result = new byte[length];
      archiveHeader.CopyTo(result, 0);
      var commonHeader = source.CommonBaseHeader.SerializedRepresentation.ToArray();
      if (animationLengths.HasValue)
      {
        WriteAnimationClassBytes(commonHeader, 0x10, animationLengths.Value);
      }
      if (animationFrameIndices.HasValue)
      {
        WriteAnimationClassBytes(commonHeader, 0x14, animationFrameIndices.Value);
      }
      foreach (var replacement in attachmentRecords)
      {
        replacement.Value.CopyTo(commonHeader, 0x1D8 + ((replacement.Key - 1) * 8));
      }
      foreach (var replacement in cannonRenderPositions)
      {
        replacement.Value.CopyTo(commonHeader, 0x018 + ((replacement.Key - 1) * 12));
      }
      foreach (var replacement in staticSpotLights)
      {
        replacement.Value.CopyTo(commonHeader, 0x048 + ((replacement.Key - 1) * 0x30));
      }
      foreach (var replacement in staticOmniLights)
      {
        replacement.Value.CopyTo(commonHeader, 0x108 + ((replacement.Key - 1) * 0x1C));
      }
      if (horizontalExtents is not null)
      {
        WriteUInt16(commonHeader, 0x360, ToUnsignedFixedPoint(horizontalExtents.PositiveY));
        WriteUInt16(commonHeader, 0x362, ToUnsignedFixedPoint(horizontalExtents.NegativeY));
        WriteUInt16(commonHeader, 0x364, ToUnsignedFixedPoint(horizontalExtents.PositiveX));
        WriteUInt16(commonHeader, 0x366, ToUnsignedFixedPoint(horizontalExtents.NegativeX));
      }
      commonHeader.CopyTo(result, archiveHeader.Length);
      var cursor = archiveHeader.Length + CommonMeshBaseHeader.SerializedSize;
      WriteUInt32(result, cursor, trailingHierarchyUnwindCount);
      cursor += sizeof(uint);
      foreach (var record in records)
      {
        record.Bytes.CopyTo(result, cursor);
        cursor += record.Bytes.Length;
      }

      source.RootTrailingBytes.CopyTo(result, cursor);
      return result;
    }

    private static void AddRecordSources(
      StaticSourceObjectSerializationPlan source,
      IDictionary<int, int> recordSources
    )
    {
      foreach (var ordinal in source.RenderObjectOrdinals)
      {
        recordSources.Add(ordinal, source.SourceObjectOrdinal);
      }
      foreach (var child in source.Children)
      {
        AddRecordSources(child, recordSources);
      }
    }

    private static StaticSourceObjectSerializationPlan CreateOutputAssembly(StaticMeshAsset asset)
    {
      var renderObjectOrdinals = asset.StaticRenderObjectSequence
        .Select((renderObject, ordinal) => (renderObject, ordinal))
        .ToDictionary(item => item.renderObject, item => item.ordinal);
      var sourceObjectOrdinals = EnumerateSourceObjects(asset.RootSourceObject)
        .Select((sourceObject, ordinal) => (sourceObject, ordinal))
        .ToDictionary(item => item.sourceObject, item => item.ordinal);
      return CreateOutputAssembly(
        asset.RootSourceObject,
        sourceObjectOrdinals,
        renderObjectOrdinals
      );
    }

    private static StaticSourceObjectSerializationPlan CreateOutputAssembly(
      StaticSourceObject source,
      IReadOnlyDictionary<StaticSourceObject, int> sourceOrdinals,
      IReadOnlyDictionary<StaticRenderObject, int> renderObjectOrdinals
    )
    {
      return new StaticSourceObjectSerializationPlan(
        sourceOrdinals[source],
        source.StaticRenderObjects.Select(renderObject => renderObjectOrdinals[renderObject]),
        source.Children.Select(child =>
          CreateOutputAssembly(child, sourceOrdinals, renderObjectOrdinals)
        )
      );
    }

    private static void RewriteSharingLinks(
      StaticMeshAsset source,
      IReadOnlyList<RewrittenStaticRecord> records,
      IReadOnlyList<int> plan,
      IReadOnlyDictionary<int, IReadOnlyList<CanonicalStaticVertex>> replacements,
      ISet<int> removed,
      IReadOnlyList<StaticRenderObjectAddition> additions
    )
    {
      var oldStarts = new Dictionary<int, int>();
      var oldCursor = 0;
      for (var ordinal = 0; ordinal < source.StaticRenderObjectSequence.Count; ordinal++)
      {
        var record = source.StaticRenderObjectSequence[ordinal];
        oldStarts.Add(ordinal, oldCursor);
        oldCursor = checked(oldCursor + record.RenderVertices.Count);
      }

      var sourceRecords = source.StaticRenderObjectSequence
        .Select((record, ordinal) => (record, ordinal))
        .ToDictionary(item => item.ordinal, item => item.record);
      var addedRecords = additions.ToDictionary(addition => addition.Ordinal);
      var newStarts = new Dictionary<int, int>();
      var newCursor = 0;
      foreach (var ordinal in plan)
      {
        newStarts.Add(ordinal, newCursor);
        var count = sourceRecords.TryGetValue(ordinal, out var sourceRecord)
          ? replacements.TryGetValue(ordinal, out var replacement)
            ? replacement.Count
            : sourceRecord.RenderVertices.Count
          : addedRecords[ordinal].Vertices.Count;
        newCursor = checked(newCursor + count);
      }

      var targetMap = new Dictionary<int, int?>();
      for (var ordinal = 0; ordinal < source.StaticRenderObjectSequence.Count; ordinal++)
      {
        var sourceRecord = source.StaticRenderObjectSequence[ordinal];
        var invalidated = removed.Contains(ordinal) || replacements.ContainsKey(ordinal);
        for (var localIndex = 0; localIndex < sourceRecord.RenderVertices.Count; localIndex++)
        {
          targetMap.Add(
            oldStarts[ordinal] + localIndex,
            invalidated ? null : newStarts[ordinal] + localIndex
          );
        }
      }

      for (var recordIndex = 0; recordIndex < plan.Count; recordIndex++)
      {
        var ordinal = plan[recordIndex];
        if (
          !sourceRecords.TryGetValue(ordinal, out var sourceRecord)
          || replacements.ContainsKey(ordinal)
        )
        {
          continue;
        }

        for (var localIndex = 0; localIndex < sourceRecord.RenderVertices.Count; localIndex++)
        {
          var blockOffset = 8 + localIndex / 4 * 0xA0;
          var laneOffset = localIndex % 4 * sizeof(ushort);
          RewriteSharingLink(
            records[recordIndex].Bytes,
            blockOffset + 0x90 + laneOffset,
            sourceRecord.RenderVertices[localIndex].NormalSharingIndex,
            targetMap
          );
          RewriteSharingLink(
            records[recordIndex].Bytes,
            blockOffset + 0x98 + laneOffset,
            sourceRecord.RenderVertices[localIndex].PositionSharingIndex,
            targetMap
          );
        }
      }
    }

    private static void RewriteSharingLink(
      byte[] record,
      int offset,
      ushort sourceTarget,
      IReadOnlyDictionary<int, int?> targetMap
    )
    {
      if (sourceTarget == ushort.MaxValue)
      {
        return;
      }

      var target = targetMap.TryGetValue(sourceTarget, out var mapped) ? mapped : null;
      WriteUInt16(
        record,
        offset,
        target.HasValue && target.Value < ushort.MaxValue ? (ushort)target.Value : ushort.MaxValue
      );
    }

    internal static IReadOnlyList<int> PlanStaticRenderObjectOrdinals(
      StaticMeshAsset source,
      IEnumerable<int> removedRenderObjects,
      IReadOnlyList<StaticRenderObjectAddition> additions
    )
    {
      var removed = new HashSet<int>(removedRenderObjects);
      var additionsBySource = additions
        .GroupBy(item => item.SourceObjectOrdinal)
        .ToDictionary(group => group.Key, group => group.ToArray());
      var sourceOrdinals = EnumerateSourceObjects(source.RootSourceObject)
        .Select((sourceObject, ordinal) => (sourceObject, ordinal))
        .ToDictionary(item => item.sourceObject, item => item.ordinal);
      var sourceByRecord = sourceOrdinals
        .SelectMany(item =>
          item.Key.StaticRenderObjects.Select(record => (record, sourceOrdinal: item.Value))
        )
        .ToDictionary(item => item.record, item => item.sourceOrdinal);
      var records = source.StaticRenderObjectSequence
        .Select((record, ordinal) => (record, ordinal))
        .ToArray();
      var lastRetainedBySource = records
        .Where(item => !removed.Contains(item.ordinal))
        .GroupBy(item => sourceByRecord[item.record])
        .ToDictionary(group => group.Key, group => group.Last().ordinal);
      var result = new List<int>();
      foreach (var item in records)
      {
        if (removed.Contains(item.ordinal))
        {
          continue;
        }

        result.Add(item.ordinal);
        var sourceOrdinal = sourceByRecord[item.record];
        if (
          lastRetainedBySource[sourceOrdinal] == item.ordinal
          && additionsBySource.TryGetValue(sourceOrdinal, out var sourceAdditions)
        )
        {
          result.AddRange(sourceAdditions.Select(addition => addition.Ordinal));
        }
      }

      return result.AsReadOnly();
    }

    private static uint RewriteHierarchyFlags(
      StaticSourceObjectSerializationPlan root,
      IReadOnlyList<RewrittenStaticRecord> records
    )
    {
      var parents = new Dictionary<int, int?>();
      var depths = new Dictionary<int, int>();
      AddSourceHierarchy(root, null, 0, parents, depths);
      var established = new HashSet<int> { root.SourceObjectOrdinal };
      var current = root.SourceObjectOrdinal;
      for (var index = 0; index < records.Count; index++)
      {
        var target = records[index].SourceObjectOrdinal;
        var unwind = 0;
        var beginsNested = false;
        if (index == 0)
        {
          if (target != root.SourceObjectOrdinal)
          {
            throw new InvalidOperationException(
              "The first retained partition must belong to the root source object."
            );
          }
        }
        else if (target != current)
        {
          var ancestor = current;
          while (
            target != ancestor
            && (
              !parents.TryGetValue(target, out var targetParent)
              || targetParent is null
              || targetParent.Value != ancestor
            )
          )
          {
            ancestor =
              parents[ancestor]
              ?? throw new InvalidOperationException(
                "The edited source sequence cannot be represented."
              );
            unwind++;
          }

          if (target == ancestor)
          {
            current = target;
          }
          else if (established.Add(target))
          {
            beginsNested = true;
            current = target;
          }
          else
          {
            throw new InvalidOperationException(
              "The edited source sequence revisits a completed source object."
            );
          }
        }

        if (unwind > byte.MaxValue)
        {
          throw new InvalidOperationException(
            "The edited hierarchy unwind exceeds its serialized range."
          );
        }

        var objectFlagsOffset = GetObjectFlagsOffset(records[index].Bytes);
        var objectFlags = ReadUInt32(records[index].Bytes, objectFlagsOffset);
        objectFlags &= ~0x000008FFu;
        objectFlags |= (uint)unwind;
        if (beginsNested)
        {
          objectFlags |= (uint)StaticRenderObjectFlags.BeginsNestedSourceObject;
        }
        WriteUInt32(records[index].Bytes, objectFlagsOffset, objectFlags);
      }

      return checked((uint)depths[current] + 1);
    }

    private static void AddSourceHierarchy(
      StaticSourceObjectSerializationPlan source,
      int? parent,
      int depth,
      IDictionary<int, int?> parents,
      IDictionary<int, int> depths
    )
    {
      parents.Add(source.SourceObjectOrdinal, parent);
      depths.Add(source.SourceObjectOrdinal, depth);
      foreach (var child in source.Children)
      {
        AddSourceHierarchy(child, source.SourceObjectOrdinal, depth + 1, parents, depths);
      }
    }

    private static int GetObjectFlagsOffset(byte[] record)
    {
      var blockCount = ReadUInt32(record, sizeof(uint));
      return checked(8 + (int)blockCount * 0xA0);
    }

    private static byte[] CreateStaticRecord(
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles,
      IReadOnlyList<byte> texturePathBytes,
      Vector3 pivot
    )
    {
      var blocks = (vertices.Count + 3) / 4;
      var result = new byte[
        checked(53 + blocks * 0xA0 + texturePathBytes.Count + triangles.Count * 8)
      ];
      var cursor = 0;
      WriteStaticRecord(result, ref cursor, vertices, triangles, texturePathBytes, 0, 0, 1);
      WriteVector3(result, result.Length - StaticRecordPivotOffsetFromEnd, pivot, invertY: true);
      return result;
    }

    private static byte[] RewriteStaticRecord(
      StaticRenderObject source,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles,
      Vector3 pivot,
      IReadOnlyList<byte> texturePathBytes,
      StaticAnimationTracks tracks,
      uint animationClassValue
    )
    {
      var blockCount = (vertices.Count + 3) / 4;
      var length = checked(
        53
        + blockCount * 0xA0
        + texturePathBytes.Count
        + triangles.Count * 8
        + tracks.ScaleFrames.Count * 12
        + tracks.TranslationFrames.Count * 12
        + tracks.Matrices.Count * 64
      );
      var data = new byte[length];
      WriteUInt32(data, 0, checked((uint)vertices.Count));
      WriteUInt32(data, 4, checked((uint)blockCount));
      for (var index = 0; index < vertices.Count; index++)
      {
        var vertex = vertices[index];
        var blockOffset = 8 + index / 4 * 0xA0;
        var laneOffset = index % 4 * 4;
        WriteSingle(data, blockOffset + laneOffset, vertex.Position.X);
        WriteSingle(data, blockOffset + 0x10 + laneOffset, -vertex.Position.Y);
        WriteSingle(data, blockOffset + 0x20 + laneOffset, vertex.Position.Z);
        WriteSingle(data, blockOffset + 0x30 + laneOffset, vertex.Normal.X);
        WriteSingle(data, blockOffset + 0x40 + laneOffset, -vertex.Normal.Y);
        WriteSingle(data, blockOffset + 0x50 + laneOffset, vertex.Normal.Z);
        WriteSingle(data, blockOffset + 0x60 + laneOffset, vertex.TextureCoordinate.X);
        WriteSingle(data, blockOffset + 0x70 + laneOffset, vertex.TextureCoordinate.Y);
        WriteUInt16(data, blockOffset + 0x90 + index % 4 * 2, ushort.MaxValue);
        WriteUInt16(data, blockOffset + 0x98 + index % 4 * 2, ushort.MaxValue);
      }

      var cursor = 8 + blockCount * 0xA0;
      WriteUInt32(data, cursor, source.ObjectFlags);
      cursor += 4;
      WriteUInt32(data, cursor, checked((uint)texturePathBytes.Count));
      cursor += 4;
      texturePathBytes.CopyTo(data, cursor);
      cursor += texturePathBytes.Count;
      WriteUInt32(data, cursor, checked((uint)triangles.Count));
      cursor += 4;
      foreach (var triangle in triangles)
      {
        WriteUInt16(data, cursor, triangle.Vertex0);
        WriteUInt16(data, cursor + 2, triangle.Vertex1);
        WriteUInt16(data, cursor + 4, triangle.Vertex2);
        WriteUInt16(data, cursor + 6, CalculateTriangleFlags(vertices, triangle));
        cursor += 8;
      }

      WriteStaticAnimationTail(
        data,
        ref cursor,
        tracks,
        animationClassValue,
        pivot,
        source.BarrelMaximumAngle,
        source.NextRecordMarker
      );
      return data;
    }

    private static byte[] RewriteStaticRecordRepresentations(
      StaticRenderObject source,
      Vector3 pivot,
      bool replacePivot,
      IReadOnlyList<byte>? texturePathBytes,
      StaticAnimationReplacement? animation
    )
    {
      var data = texturePathBytes is null
        ? source.GetSerializedRepresentation()
        : RewriteStaticTexturePath(source, texturePathBytes);
      if (animation is not null)
      {
        return RewriteStaticAnimation(source, data, animation.Tracks, animation.ClassValue, pivot);
      }
      if (replacePivot)
      {
        WriteVector3(data, data.Length - StaticRecordPivotOffsetFromEnd, pivot, invertY: true);
      }
      return data;
    }

    private static byte[] RewriteStaticAnimation(
      StaticRenderObject source,
      byte[] record,
      StaticAnimationTracks tracks,
      uint animationClassValue,
      Vector3 pivot
    )
    {
      var objectFlagsOffset = GetObjectFlagsOffset(record);
      var textureLength = checked((int)ReadUInt32(record, objectFlagsOffset + sizeof(uint)));
      var triangleCountOffset = checked(objectFlagsOffset + (2 * sizeof(uint)) + textureLength);
      var triangleCount = checked((int)ReadUInt32(record, triangleCountOffset));
      var animationOffset = checked(triangleCountOffset + sizeof(uint) + (triangleCount * 8));
      var result = new byte[
        checked(
          animationOffset
          + (3 * sizeof(uint))
          + (tracks.ScaleFrames.Count * 12)
          + (tracks.TranslationFrames.Count * 12)
          + (tracks.Matrices.Count * 64)
          + sizeof(uint)
          + 12
          + sizeof(byte)
          + sizeof(uint)
        )
      ];
      record.AsSpan(0, animationOffset).CopyTo(result);
      var cursor = animationOffset;
      WriteStaticAnimationTail(
        result,
        ref cursor,
        tracks,
        animationClassValue,
        pivot,
        source.BarrelMaximumAngle,
        source.NextRecordMarker
      );
      return result;
    }

    private static void WriteStaticAnimationTail(
      byte[] data,
      ref int cursor,
      StaticAnimationTracks tracks,
      uint animationClassValue,
      Vector3 pivot,
      byte barrelMaximumAngle,
      uint nextRecordMarker
    )
    {
      WriteUInt32(data, cursor, checked((uint)tracks.ScaleFrames.Count));
      cursor += sizeof(uint);
      foreach (var frame in tracks.ScaleFrames)
      {
        WriteVector3(data, cursor, frame, invertY: false);
        cursor += 12;
      }
      WriteUInt32(data, cursor, checked((uint)tracks.TranslationFrames.Count));
      cursor += sizeof(uint);
      foreach (var frame in tracks.TranslationFrames)
      {
        WriteVector3(data, cursor, frame, invertY: true);
        cursor += 12;
      }
      WriteUInt32(data, cursor, checked((uint)tracks.Matrices.Count));
      cursor += sizeof(uint);
      foreach (var matrix in tracks.Matrices)
      {
        WriteMatrix(data, cursor, matrix);
        cursor += 64;
      }
      WriteUInt32(data, cursor, animationClassValue);
      cursor += sizeof(uint);
      WriteVector3(data, cursor, pivot, invertY: true);
      cursor += 12;
      data[cursor++] = barrelMaximumAngle;
      WriteUInt32(data, cursor, nextRecordMarker);
    }

    private static byte[] RewriteStaticTexturePath(
      StaticRenderObject source,
      IReadOnlyList<byte> texturePathBytes
    )
    {
      var original = source.GetSerializedRepresentation();
      var lengthOffset = GetObjectFlagsOffset(original) + sizeof(uint);
      var originalPathOffset = lengthOffset + sizeof(uint);
      var result = new byte[
        checked(original.Length - source.TexturePathBytes.Count + texturePathBytes.Count)
      ];
      original.AsSpan(0, lengthOffset).CopyTo(result);
      WriteUInt32(result, lengthOffset, checked((uint)texturePathBytes.Count));
      texturePathBytes.CopyTo(result, originalPathOffset);
      original
        .AsSpan(originalPathOffset + source.TexturePathBytes.Count)
        .CopyTo(result.AsSpan(originalPathOffset + texturePathBytes.Count));
      return result;
    }

    private static byte[] RewriteStaticPivot(StaticRenderObject source, Vector3 pivot)
    {
      var data = source.GetSerializedRepresentation();
      WriteVector3(data, data.Length - StaticRecordPivotOffsetFromEnd, pivot, invertY: true);
      return data;
    }

    internal static byte[] CreateDynamic(
      Guid creationGuid,
      CanonicalDynamicObject root,
      int serializedLength
    )
    {
      var framing = new MeshArchiveFraming(0x30D0A1FF, 1, creationGuid);
      var archiveHeader = CreateArchiveHeader(framing);
      var result = new byte[archiveHeader.Length + serializedLength];
      archiveHeader.CopyTo(result, 0);
      var cursor = archiveHeader.Length;
      WriteCanonicalDynamicRecord(result, ref cursor, root);
      return result;
    }

    internal static int GetDynamicSerializedLength(CanonicalDynamicObject root)
    {
      var meshNameLength = EncodeDynamicString(root.Recipe.MeshResourceKey).Length;
      var texturePathLength = EncodeDynamicString(root.Recipe.TextureResourceKey).Length;
      var length = checked(DynamicRecordSize + meshNameLength + texturePathLength);
      foreach (var child in root.Children)
      {
        length = checked(length + GetDynamicSerializedLength(child));
      }

      return length;
    }

    internal static byte[] EncodeDynamicString(string value)
    {
      return _dynamicStringEncoding.GetBytes(value);
    }

    internal static byte[] CreateCanonicalDynamicRecord()
    {
      var record = new byte[DynamicRecordSize];
      CommonMeshBaseHeader.CanonicalDynamic.SerializedRepresentation.CopyTo(record, 0);
      return record;
    }

    private static void WriteCanonicalDynamicRecord(
      byte[] destination,
      ref int cursor,
      CanonicalDynamicObject source
    )
    {
      var recordOffset = cursor;
      var record = CreateCanonicalDynamicRecord();
      var recipe = source.Recipe;
      WriteUInt32(record, 0x368, (uint)recipe.EffectType);
      WriteUInt32(record, 0x36C, (uint)recipe.LightType);
      WriteInt32(record, 0x370, recipe.FirstSourceFrame);
      WriteInt32(record, 0x374, recipe.FrameCount);
      WriteInt32(record, 0x378, recipe.SpriteSheetColumnCount);
      WriteInt32(record, 0x37C, recipe.SpriteSheetRowCount);
      WriteInt32(record, 0x380, recipe.FramePeriodTicks);
      WriteSingle(
        record,
        0x384,
        recipe.SpriteSheetColumnCount == 0 ? 0 : 1f / recipe.SpriteSheetColumnCount
      );
      WriteSingle(
        record,
        0x388,
        recipe.SpriteSheetRowCount == 0 ? 0 : 1f / recipe.SpriteSheetRowCount
      );
      WriteRectangle(record, 0x38C, recipe.StartEffectRectangle);
      WriteRectangle(record, 0x39C, recipe.EndEffectRectangle);
      WriteSingle(record, 0x3AC, recipe.EffectDepthOffset);
      WriteSingle(record, 0x3B0, recipe.RibbonHalfWidth);
      WriteUInt32(record, 0x3B8, recipe.Additive ? 1u : 0u);
      WriteVector3(record, 0x3BC, recipe.TerrainLightColor, invertY: false);
      WriteVector3(record, 0x3C8, recipe.VisibleEffectColor, invertY: false);
      WriteSingle(record, 0x3D4, recipe.VisibleTerrainLightGain);
      WriteInt32(record, 0x3D8, (int)recipe.AlphaTiming);
      WriteSingle(record, 0x3DC, recipe.EndAlpha);
      WriteSingle(record, 0x3E0, recipe.StartAlpha);
      WriteSingle(record, 0x3E4, recipe.EndModelScale);
      WriteSingle(record, 0x3E8, recipe.StartModelScale);
      WriteVector3(record, 0x3EC, recipe.ChildStartTranslation, invertY: true);
      WriteVector3(record, 0x3F8, recipe.ChildEndTranslation, invertY: true);
      record.AsSpan(0, 0x404).CopyTo(destination.AsSpan(recordOffset));
      cursor += 0x404;
      var meshName = EncodeDynamicString(recipe.MeshResourceKey);
      var texturePath = EncodeDynamicString(recipe.TextureResourceKey);
      WriteUInt32(destination, cursor, checked((uint)meshName.Length));
      cursor += sizeof(uint);
      meshName.CopyTo(destination, cursor);
      cursor += meshName.Length;
      WriteUInt32(destination, cursor, checked((uint)texturePath.Length));
      cursor += sizeof(uint);
      texturePath.CopyTo(destination, cursor);
      cursor += texturePath.Length;
      WriteUInt32(destination, cursor, checked((uint)source.Children.Count));
      cursor += sizeof(uint);
      foreach (var child in source.Children)
      {
        WriteCanonicalDynamicRecord(destination, ref cursor, child);
      }
    }

    private static byte[] CreateStatic(
      MeshArchiveFraming framing,
      IReadOnlyList<byte> commonHeader,
      IReadOnlyList<CanonicalStaticRecord> records,
      IReadOnlyList<byte> rootTrailingBytes,
      IReadOnlyDictionary<int, Vector3>? pivots,
      IReadOnlyDictionary<int, StaticAnimationReplacement>? animations
    )
    {
      var archiveHeader = CreateArchiveHeader(framing);
      pivots ??= new Dictionary<int, Vector3>();
      animations ??= new Dictionary<int, StaticAnimationReplacement>();
      var recordLength = records.Select(
        (record, ordinal) => GetStaticRecordLength(
          record,
          animations.TryGetValue(ordinal, out var animation) ? animation : null
        )
      ).Sum();
      var result = new byte[
        archiveHeader.Length
          + CommonMeshBaseHeader.SerializedSize
          + sizeof(uint)
          + recordLength
          + rootTrailingBytes.Count
      ];
      archiveHeader.CopyTo(result, 0);
      commonHeader.CopyTo(result, archiveHeader.Length);
      var cursor = archiveHeader.Length + CommonMeshBaseHeader.SerializedSize;
      WriteUInt32(result, cursor, checked((uint)records[^1].Depth + 1));
      cursor += sizeof(uint);
      for (var index = 0; index < records.Count; index++)
      {
        var record = records[index];
        WriteStaticRecord(
          result,
          ref cursor,
          record.RenderObject.RenderVertices,
          record.RenderObject.Triangles,
          record.RenderObject.TextureResourceKey is null
            ? Array.Empty<byte>()
            : Encoding.ASCII.GetBytes(record.RenderObject.TextureResourceKey),
          record.ObjectFlags,
          ReferenceEquals(record.RenderObject, record.Source.RenderObjects[0])
            ? record.Source.Role?.BarrelMaximumAngle ?? 0
            : (byte)0,
          index == records.Count - 1 ? 0u : 1u,
          pivots.TryGetValue(index, out var pivot) ? pivot : Vector3.Zero,
          animations.TryGetValue(index, out var animation) ? animation : null
        );
      }

      rootTrailingBytes.CopyTo(result, cursor);
      return result;
    }

    private static IReadOnlyList<CanonicalStaticRecord> FlattenStaticTree(
      CanonicalStaticSourceObject root
    )
    {
      var records = new List<CanonicalStaticRecord>();
      Flatten(root, 0, records);
      var encounteredSources = new HashSet<CanonicalStaticSourceObject> { records[0].Source };
      for (var index = 0; index < records.Count; index++)
      {
        var current = records[index];
        if (index == 0 || ReferenceEquals(current.Source, records[index - 1].Source))
        {
          continue;
        }

        var previousDepth = records[index - 1].Depth;
        var beginsNested = encounteredSources.Add(current.Source);
        var unwind = beginsNested
          ? previousDepth - (current.Depth - 1)
          : previousDepth - current.Depth;
        current.ObjectFlags = (current.ObjectFlags & 0xFFFFFF00u) | checked((byte)unwind);
        if (beginsNested)
        {
          current.ObjectFlags |= (uint)StaticRenderObjectFlags.BeginsNestedSourceObject;
        }
      }

      return records;
    }

    private static void Flatten(
      CanonicalStaticSourceObject source,
      int depth,
      List<CanonicalStaticRecord> records
    )
    {
      records.Add(
        new CanonicalStaticRecord(source, source.RenderObjects[0], depth)
        {
          ObjectFlags = (uint)(source.Role?.Flags ?? StaticRenderObjectFlags.None),
        }
      );
      foreach (var child in source.Children)
      {
        Flatten(child, depth + 1, records);
      }
      records.AddRange(
        source
          .RenderObjects.Skip(1)
          .Select(renderObject => new CanonicalStaticRecord(source, renderObject, depth))
      );
    }

    private static int GetStaticRecordLength(
      CanonicalStaticRecord record,
      StaticAnimationReplacement? animation
    )
    {
      var blocks = (record.RenderObject.RenderVertices.Count + 3) / 4;
      var texturePathLength = record.RenderObject.TextureResourceKey is null
        ? 0
        : Encoding.ASCII.GetByteCount(record.RenderObject.TextureResourceKey);
      return checked(
        53
        + blocks * 0xA0
        + texturePathLength
        + record.RenderObject.Triangles.Count * 8
        + (animation?.Tracks.ScaleFrames.Count ?? 0) * 12
        + (animation?.Tracks.TranslationFrames.Count ?? 0) * 12
        + (animation?.Tracks.Matrices.Count ?? 0) * 64
      );
    }

    private static byte[] CreateArchiveHeader(MeshArchiveFraming framing)
    {
      var length =
        sizeof(uint)
        + (framing.ArchiveType.HasValue ? sizeof(uint) : 0)
        + (framing.CreationGuid.HasValue ? 16 : 0);
      var result = new byte[length];
      WriteUInt32(result, 0, framing.Declaration);
      var cursor = sizeof(uint);
      if (framing.ArchiveType.HasValue)
      {
        WriteUInt32(result, cursor, framing.ArchiveType.Value);
        cursor += sizeof(uint);
      }

      if (framing.CreationGuid.HasValue)
      {
        framing.CreationGuid.Value.ToByteArray().CopyTo(result, cursor);
      }

      return result;
    }

    private static void WriteCanonicalStaticHeaderRegions(
      byte[] header,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      CanonicalStaticFootprint? footprint,
      CanonicalHorizontalExtents? horizontalExtents
    )
    {
      var resolvedFootprint =
        footprint
        ?? new CanonicalStaticFootprint(
          0x8000,
          Enumerable
            .Range(0, 16)
            .Select(index => index == 15 ? vertices.Max(vertex => vertex.Position.Z) : 0),
          new byte[16]
        );
      WriteUInt32(header, 0x0C, resolvedFootprint.PresenceMask);
      for (var logicalIndex = 0; logicalIndex < 16; logicalIndex++)
      {
        WriteUInt16(
          header,
          0x196 - (logicalIndex * sizeof(ushort)),
          ToUnsignedFixedPoint(resolvedFootprint.TopElevations[logicalIndex])
        );
        header[0x1A7 - logicalIndex] = resolvedFootprint.CornerPassageFlags[logicalIndex];
      }
      WriteCanonicalRotatedFootprint(header, resolvedFootprint);

      var resolvedExtents =
        horizontalExtents
        ?? new CanonicalHorizontalExtents(
          Math.Max(0, vertices.Max(vertex => vertex.Position.Y)),
          -Math.Min(0, vertices.Min(vertex => vertex.Position.Y)),
          Math.Max(0, vertices.Max(vertex => vertex.Position.X)),
          -Math.Min(0, vertices.Min(vertex => vertex.Position.X))
        );
      WriteUInt16(header, 0x360, ToUnsignedFixedPoint(resolvedExtents.PositiveY));
      WriteUInt16(header, 0x362, ToUnsignedFixedPoint(resolvedExtents.NegativeY));
      WriteUInt16(header, 0x364, ToUnsignedFixedPoint(resolvedExtents.PositiveX));
      WriteUInt16(header, 0x366, ToUnsignedFixedPoint(resolvedExtents.NegativeX));
    }

    private static void WriteCanonicalRotatedFootprint(
      byte[] header,
      CanonicalStaticFootprint footprint
    )
    {
      var anchors = new[] { (X: 0, Y: 3), (X: 0, Y: 0), (X: 3, Y: 0), (X: 3, Y: 3) };
      var flagMaps = new[]
      {
        new[] { 1, 0, 3, 2 },
        new[] { 0, 3, 2, 1 },
        new[] { 3, 2, 1, 0 },
        new[] { 2, 1, 0, 3 },
      };
      for (var quarterTurn = 0; quarterTurn < 4; quarterTurn++)
      {
        ushort rotatedMask = 0;
        ulong rotatedFlags = footprint.PresenceMask == 0 ? 0 : ulong.MaxValue;
        var occupiedPhysicalSlots = new List<int>();
        for (var logicalIndex = 0; logicalIndex < 16; logicalIndex++)
        {
          if ((footprint.PresenceMask & (1 << logicalIndex)) == 0)
          {
            continue;
          }
          var physicalSlot = 15 - logicalIndex;
          var row = physicalSlot / 4;
          var column = physicalSlot % 4;
          var rotatedPhysicalSlot = quarterTurn switch
          {
            0 => 4 * (3 - column) + row,
            1 => physicalSlot,
            2 => 4 * column + (3 - row),
            _ => 15 - physicalSlot,
          };
          occupiedPhysicalSlots.Add(rotatedPhysicalSlot);
          var rotatedLogicalIndex = 15 - rotatedPhysicalSlot;
          rotatedMask |= checked((ushort)(1 << rotatedLogicalIndex));
          byte rotatedNibble = 0;
          for (var bit = 0; bit < 4; bit++)
          {
            if (
              (footprint.CornerPassageFlags[logicalIndex] & (1 << flagMaps[quarterTurn][bit])) != 0
            )
            {
              rotatedNibble |= checked((byte)(1 << bit));
            }
          }
          var shift = rotatedLogicalIndex * 4;
          rotatedFlags = (rotatedFlags & ~(0xFul << shift)) | ((ulong)rotatedNibble << shift);
        }

        uint descriptor = rotatedMask;
        if (occupiedPhysicalSlots.Count != 0)
        {
          var minimumRow = occupiedPhysicalSlots.Min(slot => slot / 4);
          var maximumRow = occupiedPhysicalSlots.Max(slot => slot / 4);
          var minimumColumn = occupiedPhysicalSlots.Min(slot => slot % 4);
          var maximumColumn = occupiedPhysicalSlots.Max(slot => slot % 4);
          var biasA = minimumRow + (int)Math.Truncate((maximumColumn + 1 - minimumRow) / 2d);
          var biasB = minimumColumn + (int)Math.Truncate((maximumRow + 1 - minimumColumn) / 2d);
          descriptor |= (uint)anchors[quarterTurn].X << 30;
          descriptor |= (uint)anchors[quarterTurn].Y << 28;
          descriptor |= (uint)biasA << 26;
          descriptor |= (uint)biasB << 24;
        }
        WriteUInt32(header, 0x1A8 + (quarterTurn * sizeof(uint)), descriptor);
        WriteUInt64(header, 0x1B8 + (quarterTurn * sizeof(ulong)), rotatedFlags);
      }
    }

    private static ushort ToUnsignedFixedPoint(float value)
    {
      return checked((ushort)Math.Truncate(value * 256d));
    }

    private static void WriteStaticRecord(
      byte[] data,
      ref int cursor,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles,
      IReadOnlyList<byte> texturePathBytes,
      uint objectFlags,
      byte barrelMaximumAngle,
      uint nextRecordMarker,
      Vector3 pivot = default,
      StaticAnimationReplacement? animation = null
    )
    {
      var recordOffset = cursor;
      WriteUInt32(data, recordOffset, checked((uint)vertices.Count));
      var blockCount = (vertices.Count + 3) / 4;
      WriteUInt32(data, recordOffset + 4, checked((uint)blockCount));
      var vertexOffset = recordOffset + 8;
      for (var lane = 0; lane < vertices.Count; lane++)
      {
        var vertex = vertices[lane];
        var blockOffset = vertexOffset + lane / 4 * 0xA0;
        var laneOffset = lane % 4 * sizeof(float);
        WriteSingle(data, blockOffset + laneOffset, vertex.Position.X);
        WriteSingle(data, blockOffset + 0x10 + laneOffset, -vertex.Position.Y);
        WriteSingle(data, blockOffset + 0x20 + laneOffset, vertex.Position.Z);
        WriteSingle(data, blockOffset + 0x30 + laneOffset, vertex.Normal.X);
        WriteSingle(data, blockOffset + 0x40 + laneOffset, -vertex.Normal.Y);
        WriteSingle(data, blockOffset + 0x50 + laneOffset, vertex.Normal.Z);
        WriteSingle(data, blockOffset + 0x60 + laneOffset, vertex.TextureCoordinate.X);
        WriteSingle(data, blockOffset + 0x70 + laneOffset, vertex.TextureCoordinate.Y);
        WriteUInt16(data, blockOffset + 0x90 + lane % 4 * sizeof(ushort), ushort.MaxValue);
        WriteUInt16(data, blockOffset + 0x98 + lane % 4 * sizeof(ushort), ushort.MaxValue);
      }

      cursor = vertexOffset + blockCount * 0xA0;
      WriteUInt32(data, cursor, objectFlags);
      cursor += sizeof(uint);
      WriteUInt32(data, cursor, checked((uint)texturePathBytes.Count));
      cursor += sizeof(uint);
      texturePathBytes.CopyTo(data, cursor);
      cursor += texturePathBytes.Count;
      WriteUInt32(data, cursor, checked((uint)triangles.Count));
      cursor += sizeof(uint);
      foreach (var triangle in triangles)
      {
        WriteUInt16(data, cursor, triangle.Vertex0);
        WriteUInt16(data, cursor + 2, triangle.Vertex1);
        WriteUInt16(data, cursor + 4, triangle.Vertex2);
        WriteUInt16(data, cursor + 6, CalculateTriangleFlags(vertices, triangle));
        cursor += 8;
      }

      WriteStaticAnimationTail(
        data,
        ref cursor,
        animation?.Tracks ?? new StaticAnimationTracks(
          Array.Empty<Vector3>(),
          Array.Empty<Vector3>(),
          Array.Empty<Matrix4x4>()
        ),
        animation?.ClassValue ?? 0,
        pivot,
        barrelMaximumAngle,
        nextRecordMarker
      );
      cursor += sizeof(uint);
    }

    private static ushort CalculateTriangleFlags(
      IReadOnlyList<CanonicalStaticVertex> vertices,
      CanonicalTriangle triangle
    )
    {
      var edge1 = vertices[triangle.Vertex1].Position - vertices[triangle.Vertex0].Position;
      var edge2 = vertices[triangle.Vertex2].Position - vertices[triangle.Vertex0].Position;
      var cross = Vector3.Cross(edge1, edge2);
      if (cross.LengthSquared() == 0)
      {
        return 1;
      }

      return Vector3.Normalize(cross).Z > 0.5f ? (ushort)3 : (ushort)1;
    }

    private static IEnumerable<StaticSourceObject> EnumerateSourceObjects(
      StaticSourceObject source
    )
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

    private sealed class CanonicalStaticRecord
    {
      internal CanonicalStaticSourceObject Source { get; }
      internal CanonicalStaticRenderObject RenderObject { get; }
      internal int Depth { get; }
      internal uint ObjectFlags { get; set; }

      internal CanonicalStaticRecord(
        CanonicalStaticSourceObject source,
        CanonicalStaticRenderObject renderObject,
        int depth
      )
      {
        Source = source;
        RenderObject = renderObject;
        Depth = depth;
      }
    }

    private sealed class RewrittenStaticRecord
    {
      internal byte[] Bytes { get; }

      internal int SourceObjectOrdinal { get; }

      internal RewrittenStaticRecord(byte[] bytes, int sourceObjectOrdinal)
      {
        Bytes = bytes;
        SourceObjectOrdinal = sourceObjectOrdinal;
      }
    }

    private static void WriteRectangle(byte[] data, int offset, EffectRectangle rectangle)
    {
      WriteSingle(data, offset, rectangle.X0);
      WriteSingle(data, offset + 4, rectangle.Y1);
      WriteSingle(data, offset + 8, rectangle.X1);
      WriteSingle(data, offset + 12, rectangle.Y0);
    }

    private static void WriteVector3(byte[] data, int offset, Vector3 value, bool invertY)
    {
      WriteSingle(data, offset, value.X);
      WriteSingle(data, offset + 4, invertY && value.Y != 0 ? -value.Y : value.Y);
      WriteSingle(data, offset + 8, value.Z);
    }

    private static void WriteMatrix(byte[] data, int offset, Matrix4x4 value)
    {
      var values = new[]
      {
        value.M11,
        value.M12,
        value.M13,
        value.M14,
        value.M21,
        value.M22,
        value.M23,
        value.M24,
        value.M31,
        value.M32,
        value.M33,
        value.M34,
        value.M41,
        value.M42,
        value.M43,
        value.M44,
      };
      for (var index = 0; index < values.Length; index++)
      {
        WriteSingle(data, offset + index * 4, values[index]);
      }
    }

    private static void WriteAnimationClassBytes(byte[] data, int offset, AnimationClassBytes value)
    {
      WriteUInt32(
        data,
        offset,
        ((uint)value.A << 24) | ((uint)value.B << 16) | ((uint)value.C << 8) | value.D
      );
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
      BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, sizeof(uint)));
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteInt32(byte[] data, int offset, int value)
    {
      BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteUInt64(byte[] data, int offset, ulong value)
    {
      BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteSingle(byte[] data, int offset, float value)
    {
      WriteUInt32(data, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
    }

    private static Encoding CreateDynamicStringEncoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      return Encoding.GetEncoding(
        28592,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback
      );
    }
  }

  internal static class ReadOnlyListCopyExtensions
  {
    internal static void CopyTo<T>(
      this IReadOnlyList<T> source,
      T[] destination,
      int destinationIndex
    )
    {
      for (var index = 0; index < source.Count; index++)
      {
        destination[destinationIndex + index] = source[index];
      }
    }
  }
}
