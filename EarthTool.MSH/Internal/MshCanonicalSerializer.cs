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
    private static readonly Encoding _dynamicStringEncoding = CreateDynamicStringEncoding();

    internal static byte[] CreateStatic(
      Guid creationGuid,
      AnimationClassBytes animationLengths,
      CanonicalStaticSourceObject rootSourceObject)
    {
      var framing = new MeshArchiveFraming(0x20D0A1FF, null, creationGuid);
      var records = FlattenStaticTree(rootSourceObject);
      var vertices = records.SelectMany(record => record.RenderObject.RenderVertices).ToArray();
      var commonHeader = CreateCanonicalCommonHeader(0, animationLengths, vertices);
      return CreateStatic(framing, commonHeader, records, Array.Empty<byte>());
    }

    internal static byte[] RewriteStatic(
      StaticMeshAsset source,
      IReadOnlyDictionary<StaticRenderObjectId, IReadOnlyList<CanonicalStaticVertex>> vertices,
      IReadOnlyDictionary<StaticRenderObjectId, IReadOnlyList<CanonicalTriangle>> triangles)
    {
      var archiveHeader = CreateArchiveHeader(source.ArchiveFraming);
      var records = source.StaticRenderObjectSequence.Select(record =>
        vertices.TryGetValue(record.Id, out var replacementVertices)
          ? RewriteStaticRecord(record, replacementVertices, triangles[record.Id])
          : record.GetSerializedRepresentation()).ToArray();
      var length = archiveHeader.Length + BaseHeaderSize + sizeof(uint)
        + records.Sum(record => record.Length) + source.RootTrailingBytes.Count;
      var result = new byte[length];
      archiveHeader.CopyTo(result, 0);
      source.CommonBaseHeader.SerializedRepresentation.CopyTo(result, archiveHeader.Length);
      var cursor = archiveHeader.Length + BaseHeaderSize;
      WriteUInt32(result, cursor, source.StoredTrailingHierarchyUnwindCount);
      cursor += sizeof(uint);
      foreach (var record in records)
      {
        record.CopyTo(result, cursor);
        cursor += record.Length;
      }

      source.RootTrailingBytes.CopyTo(result, cursor);
      return result;
    }

    private static byte[] RewriteStaticRecord(
      StaticRenderObject source,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles)
    {
      var blockCount = (vertices.Count + 3) / 4;
      var tracks = source.AnimationTracks;
      var length = checked(53 + blockCount * 0xA0 + source.TexturePathBytes.Count
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
      WriteUInt32(data, cursor, checked((uint)source.TexturePathBytes.Count));
      cursor += 4;
      source.TexturePathBytes.CopyTo(data, cursor);
      cursor += source.TexturePathBytes.Count;
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
      WriteVector3(data, cursor, source.Pivot, invertY: true);
      cursor += 12;
      data[cursor++] = source.BarrelMaximumAngle;
      WriteUInt32(data, cursor, source.NextRecordMarker);
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
      for (var index = 0; index < records.Count; index++)
      {
        var current = records[index];
        if (index == 0 || ReferenceEquals(current.Source, records[index - 1].Source))
        {
          continue;
        }

        var previousDepth = records[index - 1].Depth;
        var unwind = previousDepth - (current.Depth - 1);
        current.ObjectFlags = (uint)StaticRenderObjectFlags.BeginsNestedSourceObject
          | checked((byte)unwind);
      }

      return records;
    }

    private static void Flatten(
      CanonicalStaticSourceObject source,
      int depth,
      List<CanonicalStaticRecord> records)
    {
      records.AddRange(source.RenderObjects.Select(renderObject =>
        new CanonicalStaticRecord(source, renderObject, depth)));
      foreach (var child in source.Children)
      {
        Flatten(child, depth + 1, records);
      }
    }

    private static int GetStaticRecordLength(CanonicalStaticRecord record)
    {
      var blocks = (record.RenderObject.RenderVertices.Count + 3) / 4;
      return checked(53 + blocks * 0xA0 + record.RenderObject.Triangles.Count * 8);
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
      WriteUInt32(data, cursor, 0);
      cursor += sizeof(uint);
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
