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
      IReadOnlyDictionary<int, CanonicalAttachmentRecord>? attachmentRecords = null,
      IReadOnlyDictionary<int, CanonicalCannonRenderPosition>? cannonRenderPositions = null,
      IReadOnlyDictionary<int, CanonicalSpotLight>? staticSpotLights = null,
      IReadOnlyDictionary<int, CanonicalOmniLight>? staticOmniLights = null
    )
    {
      var framing = new MeshArchiveFraming(0x20D0A1FF, null, creationGuid);
      var vertices = rootSourceObject
        .RenderObjects.SelectMany(record => record.RenderVertices)
        .ToArray();
      var commonHeader = CanonicalBaseHeaderEncoder
        .EncodeStatic(
          new CanonicalStaticBaseHeaderInput(
            animationLengths,
            vertices,
            footprint,
            horizontalExtents,
            animationFrameIndices,
            attachmentRecords,
            cannonRenderPositions,
            staticSpotLights,
            staticOmniLights
          )
        )
        .SerializedRepresentation;
      var sequence = CanonicalStaticRenderObjectSequenceEncoder.Encode(
        rootSourceObject,
        pivots,
        animations
      );
      return CreateStatic(framing, commonHeader, sequence);
    }

    internal static long GetCanonicalStaticSerializedLength(
      IEnumerable<(int VertexCount, int TriangleCount)> geometry
    )
    {
      return checked(
        sizeof(uint)
        + 16L
        + CommonMeshBaseHeader.SerializedSize
        + CanonicalStaticRenderObjectSequenceEncoder.GetMinimumSerializedLength(geometry)
      );
    }

    internal static byte[] CreateStatic(
      Guid creationGuid,
      IReadOnlyList<byte> commonHeader,
      IReadOnlyList<byte> sequence
    )
    {
      return CreateStatic(
        new MeshArchiveFraming(0x20D0A1FF, null, creationGuid),
        commonHeader,
        sequence
      );
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
      var commonHeader = CanonicalBaseHeaderEncoder.RewriteStatic(
        source.CommonBaseHeader,
        animationLengths,
        animationFrameIndices,
        attachmentRecords,
        cannonRenderPositions,
        staticSpotLights,
        staticOmniLights,
        horizontalExtents
      );
      commonHeader.SerializedRepresentation.CopyTo(result, archiveHeader.Length);
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
      return CanonicalStaticRenderObjectSequenceEncoder.EncodeRecord(
        vertices,
        triangles,
        texturePathBytes,
        0,
        0,
        1,
        pivot,
        null
      );
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
      return CanonicalStaticRenderObjectSequenceEncoder.EncodeRecord(
        vertices,
        triangles,
        texturePathBytes,
        source.ObjectFlags,
        source.BarrelMaximumAngle,
        source.NextRecordMarker,
        pivot,
        new StaticAnimationReplacement(tracks, animationClassValue)
      );
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
      CanonicalBaseHeaderEncoder.Dynamic.SerializedRepresentation.CopyTo(record, 0);
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
      IReadOnlyList<byte> sequence
    )
    {
      var archiveHeader = CreateArchiveHeader(framing);
      var result = new byte[
        archiveHeader.Length
          + CommonMeshBaseHeader.SerializedSize
          + sequence.Count
      ];
      archiveHeader.CopyTo(result, 0);
      commonHeader.CopyTo(result, archiveHeader.Length);
      sequence.CopyTo(result, archiveHeader.Length + CommonMeshBaseHeader.SerializedSize);
      return result;
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
