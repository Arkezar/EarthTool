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
  internal static class MshCanonicalSerializer
  {
    internal const int BaseHeaderSize = 0x368;
    internal const int StaticRecordSize = 0xDD;
    internal const int DynamicRecordSize = 0x410;
    private const int StaticRecordPivotOffsetFromEnd = 17;
    private static readonly Encoding _dynamicStringEncoding = CreateDynamicStringEncoding();

    internal static byte[] CreateStatic(
      Guid creationGuid,
      AnimationClassBytes animationLengths,
      CanonicalStaticSourceObject rootSourceObject)
    {
      var framing = new MeshArchiveFraming(0x20D0A1FF, null, creationGuid);
      var records = FlattenStaticTree(rootSourceObject);
      var vertices = rootSourceObject.RenderObjects
        .SelectMany(record => record.RenderVertices)
        .ToArray();
      var commonHeader = CreateCanonicalCommonHeader(0, animationLengths, vertices);
      return CreateStatic(framing, commonHeader, records, Array.Empty<byte>());
    }

    internal static long GetCanonicalStaticSerializedLength(
      IEnumerable<(int VertexCount, int TriangleCount)> geometry)
    {
      var length = sizeof(uint) + 16L + BaseHeaderSize + sizeof(uint);
      foreach (var record in geometry)
      {
        var blocks = checked((record.VertexCount + 3L) / 4L);
        length = checked(length + 53L + (blocks * 0xA0L) + (record.TriangleCount * 8L));
      }
      return length;
    }

    internal static byte[] RewriteStatic(
      StaticMeshAsset source,
      IReadOnlyDictionary<StaticRenderObjectId, IReadOnlyList<CanonicalStaticVertex>> vertices,
      IReadOnlyDictionary<StaticRenderObjectId, IReadOnlyList<CanonicalTriangle>> triangles,
      IEnumerable<StaticRenderObjectId>? removedRenderObjects = null,
      IReadOnlyList<StaticRenderObjectAddition>? additions = null,
      StaticSourceObject? rootSourceObject = null,
      IReadOnlyList<StaticRenderObjectId>? explicitSequence = null,
      IReadOnlyDictionary<StaticRenderObjectId, Vector3>? pivots = null,
      IReadOnlyDictionary<StaticRenderObjectId, byte[]>? texturePathBytes = null,
      bool canonicalizeNextRecordMarkers = false)
    {
      var archiveHeader = CreateArchiveHeader(source.ArchiveFraming);
      var removed = new HashSet<StaticRenderObjectId>(
        removedRenderObjects ?? Array.Empty<StaticRenderObjectId>());
      additions ??= Array.Empty<StaticRenderObjectAddition>();
      rootSourceObject ??= source.RootSourceObject;
      pivots ??= new Dictionary<StaticRenderObjectId, Vector3>();
      texturePathBytes ??= new Dictionary<StaticRenderObjectId, byte[]>();
      var sourceRecords = source.StaticRenderObjectSequence.ToDictionary(record => record.Id);
      var addedRecords = additions.ToDictionary(addition => addition.Id);
      var finalRecordSources = new Dictionary<StaticRenderObjectId, SourceObjectId>();
      AddRecordSources(rootSourceObject, finalRecordSources);
      var recordList = new List<RewrittenStaticRecord>();
      var plan = explicitSequence ?? PlanStaticRenderObjectIds(source, removed, additions);
      foreach (var id in plan)
      {
        if (sourceRecords.TryGetValue(id, out var record))
        {
          var pivot = pivots.TryGetValue(record.Id, out var replacementPivot)
            ? replacementPivot
            : record.Pivot;
          var hasReplacementTexturePath = texturePathBytes.TryGetValue(
            record.Id,
            out var replacementTexturePath);
          recordList.Add(new RewrittenStaticRecord(
            vertices.TryGetValue(record.Id, out var replacementVertices)
              ? RewriteStaticRecord(
                record,
                replacementVertices,
                triangles[record.Id],
                pivot,
                hasReplacementTexturePath
                  ? replacementTexturePath
                  : record.TexturePathBytes)
              : RewriteStaticRecordRepresentations(
                record,
                pivot,
                pivots.ContainsKey(record.Id),
                hasReplacementTexturePath ? replacementTexturePath : null),
            finalRecordSources[id]));
          continue;
        }

        var addition = addedRecords[id];
        recordList.Add(new RewrittenStaticRecord(
          CreateStaticRecord(
            addition.Vertices,
            addition.Triangles,
            addition.TexturePathBytes,
            pivots.TryGetValue(addition.Id, out var additionPivot)
              ? additionPivot
              : Vector3.Zero),
          addition.SourceObjectId));
      }

      var records = recordList.ToArray();
      RewriteSharingLinks(source, records, plan, vertices, removed, additions);
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
      var length = archiveHeader.Length + BaseHeaderSize + sizeof(uint)
        + records.Sum(record => record.Bytes.Length) + source.RootTrailingBytes.Count;
      var result = new byte[length];
      archiveHeader.CopyTo(result, 0);
      source.CommonBaseHeader.SerializedRepresentation.CopyTo(result, archiveHeader.Length);
      var cursor = archiveHeader.Length + BaseHeaderSize;
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
      StaticSourceObject source,
      IDictionary<StaticRenderObjectId, SourceObjectId> recordSources)
    {
      foreach (var id in source.StaticRenderObjectIds)
      {
        recordSources.Add(id, source.Id);
      }
      foreach (var child in source.Children)
      {
        AddRecordSources(child, recordSources);
      }
    }

    private static void RewriteSharingLinks(
      StaticMeshAsset source,
      IReadOnlyList<RewrittenStaticRecord> records,
      IReadOnlyList<StaticRenderObjectId> plan,
      IReadOnlyDictionary<StaticRenderObjectId, IReadOnlyList<CanonicalStaticVertex>> replacements,
      ISet<StaticRenderObjectId> removed,
      IReadOnlyList<StaticRenderObjectAddition> additions)
    {
      var oldStarts = new Dictionary<StaticRenderObjectId, int>();
      var oldCursor = 0;
      foreach (var record in source.StaticRenderObjectSequence)
      {
        oldStarts.Add(record.Id, oldCursor);
        oldCursor = checked(oldCursor + record.RenderVertices.Count);
      }

      var sourceRecords = source.StaticRenderObjectSequence.ToDictionary(record => record.Id);
      var addedRecords = additions.ToDictionary(addition => addition.Id);
      var newStarts = new Dictionary<StaticRenderObjectId, int>();
      var newCursor = 0;
      foreach (var id in plan)
      {
        newStarts.Add(id, newCursor);
        var count = sourceRecords.TryGetValue(id, out var sourceRecord)
          ? replacements.TryGetValue(id, out var replacement) ? replacement.Count : sourceRecord.RenderVertices.Count
          : addedRecords[id].Vertices.Count;
        newCursor = checked(newCursor + count);
      }

      var targetMap = new Dictionary<int, int?>();
      foreach (var sourceRecord in source.StaticRenderObjectSequence)
      {
        var invalidated = removed.Contains(sourceRecord.Id) || replacements.ContainsKey(sourceRecord.Id);
        for (var localIndex = 0; localIndex < sourceRecord.RenderVertices.Count; localIndex++)
        {
          targetMap.Add(
            oldStarts[sourceRecord.Id] + localIndex,
            invalidated ? null : newStarts[sourceRecord.Id] + localIndex);
        }
      }

      for (var recordIndex = 0; recordIndex < plan.Count; recordIndex++)
      {
        var id = plan[recordIndex];
        if (!sourceRecords.TryGetValue(id, out var sourceRecord) || replacements.ContainsKey(id))
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
            targetMap);
          RewriteSharingLink(
            records[recordIndex].Bytes,
            blockOffset + 0x98 + laneOffset,
            sourceRecord.RenderVertices[localIndex].PositionSharingIndex,
            targetMap);
        }
      }
    }

    private static void RewriteSharingLink(
      byte[] record,
      int offset,
      ushort sourceTarget,
      IReadOnlyDictionary<int, int?> targetMap)
    {
      if (sourceTarget == ushort.MaxValue)
      {
        return;
      }

      var target = targetMap.TryGetValue(sourceTarget, out var mapped) ? mapped : null;
      WriteUInt16(
        record,
        offset,
        target.HasValue && target.Value < ushort.MaxValue
          ? (ushort)target.Value
          : ushort.MaxValue);
    }

    internal static IReadOnlyList<StaticRenderObjectId> PlanStaticRenderObjectIds(
      StaticMeshAsset source,
      IEnumerable<StaticRenderObjectId> removedRenderObjects,
      IReadOnlyList<StaticRenderObjectAddition> additions)
    {
      var removed = new HashSet<StaticRenderObjectId>(removedRenderObjects);
      var additionsBySource = additions.GroupBy(item => item.SourceObjectId)
        .ToDictionary(group => group.Key, group => group.ToArray());
      var lastRetainedBySource = source.StaticRenderObjectSequence
        .Where(record => !removed.Contains(record.Id))
        .GroupBy(record => record.SourceObjectId)
        .ToDictionary(group => group.Key, group => group.Last().Id);
      var result = new List<StaticRenderObjectId>();
      foreach (var record in source.StaticRenderObjectSequence)
      {
        if (removed.Contains(record.Id))
        {
          continue;
        }

        result.Add(record.Id);
        if (lastRetainedBySource[record.SourceObjectId].Equals(record.Id)
          && additionsBySource.TryGetValue(record.SourceObjectId, out var sourceAdditions))
        {
          result.AddRange(sourceAdditions.Select(addition => addition.Id));
        }
      }

      return result.AsReadOnly();
    }

    private static uint RewriteHierarchyFlags(
      StaticSourceObject root,
      IReadOnlyList<RewrittenStaticRecord> records)
    {
      var parents = new Dictionary<SourceObjectId, SourceObjectId?>();
      var depths = new Dictionary<SourceObjectId, int>();
      AddSourceHierarchy(root, null, 0, parents, depths);
      var established = new HashSet<SourceObjectId> { root.Id };
      var current = root.Id;
      for (var index = 0; index < records.Count; index++)
      {
        var target = records[index].SourceObjectId;
        var unwind = 0;
        var beginsNested = false;
        if (index == 0)
        {
          if (!target.Equals(root.Id))
          {
            throw new InvalidOperationException("The first retained partition must belong to the root source object.");
          }
        }
        else if (!target.Equals(current))
        {
          var ancestor = current;
          while (!target.Equals(ancestor)
            && (!parents.TryGetValue(target, out var targetParent)
              || targetParent is null
              || !targetParent.Value.Equals(ancestor)))
          {
            ancestor = parents[ancestor]
              ?? throw new InvalidOperationException("The edited source sequence cannot be represented.");
            unwind++;
          }

          if (target.Equals(ancestor))
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
            throw new InvalidOperationException("The edited source sequence revisits a completed source object.");
          }
        }

        if (unwind > byte.MaxValue)
        {
          throw new InvalidOperationException("The edited hierarchy unwind exceeds its serialized range.");
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
      StaticSourceObject source,
      SourceObjectId? parent,
      int depth,
      IDictionary<SourceObjectId, SourceObjectId?> parents,
      IDictionary<SourceObjectId, int> depths)
    {
      parents.Add(source.Id, parent);
      depths.Add(source.Id, depth);
      foreach (var child in source.Children)
      {
        AddSourceHierarchy(child, source.Id, depth + 1, parents, depths);
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
      Vector3 pivot)
    {
      var blocks = (vertices.Count + 3) / 4;
      var result = new byte[checked(53 + blocks * 0xA0 + texturePathBytes.Count
        + triangles.Count * 8)];
      var cursor = 0;
      WriteStaticRecord(result, ref cursor, vertices, triangles, texturePathBytes, 0, 1);
      WriteVector3(result, result.Length - StaticRecordPivotOffsetFromEnd, pivot, invertY: true);
      return result;
    }

    private static byte[] RewriteStaticRecord(
      StaticRenderObject source,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles,
      Vector3 pivot,
      IReadOnlyList<byte> texturePathBytes)
    {
      var blockCount = (vertices.Count + 3) / 4;
      var tracks = source.AnimationTracks;
      var length = checked(53 + blockCount * 0xA0 + texturePathBytes.Count
        + triangles.Count * 8
        + tracks.ScaleFrames.Count * 12
        + tracks.TranslationFrames.Count * 12
        + tracks.Matrices.Count * 64);
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

      WriteUInt32(data, cursor, checked((uint)tracks.ScaleFrames.Count));
      cursor += 4;
      foreach (var frame in tracks.ScaleFrames)
      {
        WriteVector3(data, cursor, frame, invertY: false);
        cursor += 12;
      }

      WriteUInt32(data, cursor, checked((uint)tracks.TranslationFrames.Count));
      cursor += 4;
      foreach (var frame in tracks.TranslationFrames)
      {
        WriteVector3(data, cursor, frame, invertY: true);
        cursor += 12;
      }

      WriteUInt32(data, cursor, checked((uint)tracks.Matrices.Count));
      cursor += 4;
      foreach (var matrix in tracks.Matrices)
      {
        WriteMatrix(data, cursor, matrix);
        cursor += 64;
      }

      WriteUInt32(data, cursor, source.AnimationClassValue);
      cursor += 4;
      WriteVector3(data, cursor, pivot, invertY: true);
      cursor += 12;
      data[cursor++] = source.BarrelMaximumAngle;
      WriteUInt32(data, cursor, source.NextRecordMarker);
      return data;
    }

    private static byte[] RewriteStaticRecordRepresentations(
      StaticRenderObject source,
      Vector3 pivot,
      bool replacePivot,
      IReadOnlyList<byte>? texturePathBytes)
    {
      var data = texturePathBytes is null
        ? source.GetSerializedRepresentation()
        : RewriteStaticTexturePath(source, texturePathBytes);
      if (replacePivot)
      {
        WriteVector3(data, data.Length - StaticRecordPivotOffsetFromEnd, pivot, invertY: true);
      }
      return data;
    }

    private static byte[] RewriteStaticTexturePath(
      StaticRenderObject source,
      IReadOnlyList<byte> texturePathBytes)
    {
      var original = source.GetSerializedRepresentation();
      var lengthOffset = GetObjectFlagsOffset(original) + sizeof(uint);
      var originalPathOffset = lengthOffset + sizeof(uint);
      var result = new byte[checked(original.Length - source.TexturePathBytes.Count
        + texturePathBytes.Count)];
      original.AsSpan(0, lengthOffset).CopyTo(result);
      WriteUInt32(result, lengthOffset, checked((uint)texturePathBytes.Count));
      texturePathBytes.CopyTo(result, originalPathOffset);
      original.AsSpan(originalPathOffset + source.TexturePathBytes.Count).CopyTo(
        result.AsSpan(originalPathOffset + texturePathBytes.Count));
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
      int serializedLength)
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
      var commonHeader = CreateCanonicalCommonHeader(
        1,
        new AnimationClassBytes(),
        Array.Empty<CanonicalStaticVertex>());
      commonHeader.CopyTo(record, 0);

      WriteRectangle(record, 0x38C);
      WriteRectangle(record, 0x39C);
      WriteSingle(record, 0x3AC, 0.25f);
      WriteSingle(record, 0x3B0, 0.25f);
      WriteSingle(record, 0x3C8, 1f);
      WriteSingle(record, 0x3CC, 1f);
      WriteSingle(record, 0x3D0, 1f);
      WriteSingle(record, 0x3D4, 1f);
      WriteSingle(record, 0x3DC, 1f);
      WriteSingle(record, 0x3E0, 1f);
      return record;
    }

    private static void WriteCanonicalDynamicRecord(
      byte[] destination,
      ref int cursor,
      CanonicalDynamicObject source)
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
      WriteSingle(record, 0x384, recipe.SpriteSheetColumnCount == 0
        ? 0
        : 1f / recipe.SpriteSheetColumnCount);
      WriteSingle(record, 0x388, recipe.SpriteSheetRowCount == 0
        ? 0
        : 1f / recipe.SpriteSheetRowCount);
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

    internal static byte[] CreateCanonicalCommonHeader(
      uint meshKind,
      AnimationClassBytes animationLengths,
      IReadOnlyList<CanonicalStaticVertex> vertices)
    {
      var header = new byte[BaseHeaderSize];
      header[0] = (byte)'M';
      header[1] = (byte)'E';
      header[2] = (byte)'S';
      header[3] = (byte)'H';
      WriteUInt32(header, 0x04, 1);
      WriteUInt32(header, 0x08, meshKind);
      WriteAnimationClassBytes(header, 0x10, animationLengths);

      for (var attachment = 0; attachment < 49; attachment++)
      {
        var offset = 0x1D8 + (attachment * 8);
        WriteInt16(header, offset, short.MinValue);
        WriteInt16(header, offset + 2, short.MinValue);
        WriteInt16(header, offset + 4, short.MinValue);
      }

      if (meshKind == 0)
      {
        WriteCanonicalStaticHeaderRegions(header, vertices);
      }

      return header;
    }

    private static byte[] CreateStatic(
      MeshArchiveFraming framing,
      IReadOnlyList<byte> commonHeader,
      IReadOnlyList<CanonicalStaticRecord> records,
      IReadOnlyList<byte> rootTrailingBytes)
    {
      var archiveHeader = CreateArchiveHeader(framing);
      var recordLength = records.Sum(GetStaticRecordLength);
      var result = new byte[archiveHeader.Length + BaseHeaderSize + sizeof(uint) + recordLength
        + rootTrailingBytes.Count];
      archiveHeader.CopyTo(result, 0);
      commonHeader.CopyTo(result, archiveHeader.Length);
      var cursor = archiveHeader.Length + BaseHeaderSize;
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
          index == records.Count - 1 ? 0u : 1u);
      }

      rootTrailingBytes.CopyTo(result, cursor);
      return result;
    }

    private static IReadOnlyList<CanonicalStaticRecord> FlattenStaticTree(
      CanonicalStaticSourceObject root)
    {
      var records = new List<CanonicalStaticRecord>();
      Flatten(root, 0, records);
      var encounteredSources = new HashSet<CanonicalStaticSourceObject>
      {
        records[0].Source
      };
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
        current.ObjectFlags = checked((byte)unwind);
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
      List<CanonicalStaticRecord> records)
    {
      records.Add(new CanonicalStaticRecord(source, source.RenderObjects[0], depth));
      foreach (var child in source.Children)
      {
        Flatten(child, depth + 1, records);
      }
      records.AddRange(source.RenderObjects.Skip(1).Select(renderObject =>
        new CanonicalStaticRecord(source, renderObject, depth)));
    }

    private static int GetStaticRecordLength(CanonicalStaticRecord record)
    {
      var blocks = (record.RenderObject.RenderVertices.Count + 3) / 4;
      var texturePathLength = record.RenderObject.TextureResourceKey is null
        ? 0
        : Encoding.ASCII.GetByteCount(record.RenderObject.TextureResourceKey);
      return checked(53 + blocks * 0xA0 + texturePathLength
        + record.RenderObject.Triangles.Count * 8);
    }

    private static byte[] CreateArchiveHeader(MeshArchiveFraming framing)
    {
      var length = sizeof(uint)
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
      IReadOnlyList<CanonicalStaticVertex> vertices)
    {
      WriteUInt32(header, 0x0C, 0x00008000);
      var maximumZ = vertices.Max(vertex => vertex.Position.Z);
      WriteUInt16(header, 0x178, ToUnsignedFixedPoint(maximumZ));
      WriteUInt32(header, 0x1A8, 0x3A000008);
      WriteUInt32(header, 0x1AC, 0x00008000);
      WriteUInt32(header, 0x1B0, 0xCA001000);
      WriteUInt32(header, 0x1B4, 0xFF000001);
      WriteUInt64(header, 0x1B8, 0xFFFFFFFFFFFF0FFF);
      WriteUInt64(header, 0x1C0, 0x0FFFFFFFFFFFFFFF);
      WriteUInt64(header, 0x1C8, 0xFFF0FFFFFFFFFFFF);
      WriteUInt64(header, 0x1D0, 0xFFFFFFFFFFFFFFF0);

      var maximumX = Math.Max(0, vertices.Max(vertex => vertex.Position.X));
      var minimumX = Math.Min(0, vertices.Min(vertex => vertex.Position.X));
      var maximumY = Math.Max(0, vertices.Max(vertex => vertex.Position.Y));
      var minimumY = Math.Min(0, vertices.Min(vertex => vertex.Position.Y));
      WriteUInt16(header, 0x360, ToUnsignedFixedPoint(maximumY));
      WriteUInt16(header, 0x362, ToUnsignedFixedPoint(-minimumY));
      WriteUInt16(header, 0x364, ToUnsignedFixedPoint(maximumX));
      WriteUInt16(header, 0x366, ToUnsignedFixedPoint(-minimumX));
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
      uint nextRecordMarker)
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

      cursor += 12;
      cursor += sizeof(uint) + 12;
      cursor++;
      WriteUInt32(data, cursor, nextRecordMarker);
      cursor += sizeof(uint);
    }

    private static ushort CalculateTriangleFlags(
      IReadOnlyList<CanonicalStaticVertex> vertices,
      CanonicalTriangle triangle)
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

    private sealed class CanonicalStaticRecord
    {
      internal CanonicalStaticSourceObject Source { get; }
      internal CanonicalStaticRenderObject RenderObject { get; }
      internal int Depth { get; }
      internal uint ObjectFlags { get; set; }

      internal CanonicalStaticRecord(
        CanonicalStaticSourceObject source,
        CanonicalStaticRenderObject renderObject,
        int depth)
      {
        Source = source;
        RenderObject = renderObject;
        Depth = depth;
      }
    }

    private sealed class RewrittenStaticRecord
    {
      internal byte[] Bytes { get; }

      internal SourceObjectId SourceObjectId { get; }

      internal RewrittenStaticRecord(byte[] bytes, SourceObjectId sourceObjectId)
      {
        Bytes = bytes;
        SourceObjectId = sourceObjectId;
      }
    }

    private static void WriteRectangle(byte[] data, int offset)
    {
      WriteSingle(data, offset, -0.25f);
      WriteSingle(data, offset + 4, 0.25f);
      WriteSingle(data, offset + 8, 0.25f);
      WriteSingle(data, offset + 12, -0.25f);
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
        value.M11, value.M12, value.M13, value.M14,
        value.M21, value.M22, value.M23, value.M24,
        value.M31, value.M32, value.M33, value.M34,
        value.M41, value.M42, value.M43, value.M44
      };
      for (var index = 0; index < values.Length; index++)
      {
        WriteSingle(data, offset + index * 4, values[index]);
      }
    }

    private static void WriteAnimationClassBytes(byte[] data, int offset, AnimationClassBytes value)
    {
      WriteUInt32(data, offset,
        ((uint)value.A << 24) | ((uint)value.B << 16) | ((uint)value.C << 8) | value.D);
    }

    private static void WriteInt16(byte[] data, int offset, short value)
    {
      BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), value);
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
      return Encoding.GetEncoding(28592, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }
  }

  internal static class ReadOnlyListCopyExtensions
  {
    internal static void CopyTo<T>(this IReadOnlyList<T> source, T[] destination, int destinationIndex)
    {
      for (var index = 0; index < source.Count; index++)
      {
        destination[destinationIndex + index] = source[index];
      }
    }
  }
}
