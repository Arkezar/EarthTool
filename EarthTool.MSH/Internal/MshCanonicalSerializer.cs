#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace EarthTool.MSH.Internal
{
  internal static class MshCanonicalSerializer
  {
    internal const int BaseHeaderSize = 0x368;
    internal const int StaticRecordSize = 0xDD;
    internal const int DynamicRecordSize = 0x410;

    internal static byte[] CreateStatic(
      Guid creationGuid,
      AnimationClassBytes animationLengths,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles)
    {
      var framing = new MeshArchiveFraming(0x20D0A1FF, null, creationGuid);
      var commonHeader = CreateCanonicalCommonHeader(0, animationLengths, vertices);
      return CreateStatic(framing, commonHeader, vertices, triangles, Array.Empty<byte>());
    }

    internal static byte[] RewriteStatic(
      StaticMeshAsset source,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles)
    {
      return CreateStatic(
        source.ArchiveFraming,
        source.CommonBaseHeader.SerializedRepresentation.ToArray(),
        vertices,
        triangles,
        source.RootTrailingBytes.ToArray());
    }

    internal static byte[] CreateDynamic(
      Guid creationGuid,
      CanonicalDynamicObject root,
      int objectCount)
    {
      var framing = new MeshArchiveFraming(0x30D0A1FF, 1, creationGuid);
      var archiveHeader = CreateArchiveHeader(framing);
      var result = new byte[archiveHeader.Length + (objectCount * DynamicRecordSize)];
      archiveHeader.CopyTo(result, 0);
      var cursor = archiveHeader.Length;
      WriteCanonicalDynamicRecord(result, ref cursor, root);
      return result;
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
      WriteUInt32(record, 0x368, (uint)source.EffectType);
      WriteUInt32(record, 0x40C, checked((uint)source.Children.Count));
      record.CopyTo(destination, recordOffset);
      cursor += record.Length;
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
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles,
      IReadOnlyList<byte> rootTrailingBytes)
    {
      var archiveHeader = CreateArchiveHeader(framing);
      var result = new byte[
        archiveHeader.Length + BaseHeaderSize + sizeof(uint) + StaticRecordSize + rootTrailingBytes.Count];
      archiveHeader.CopyTo(result, 0);
      commonHeader.CopyTo(result, archiveHeader.Length);
      var cursor = archiveHeader.Length + BaseHeaderSize;
      WriteUInt32(result, cursor, 1);
      cursor += sizeof(uint);
      WriteStaticRecord(result, cursor, vertices, triangles);
      rootTrailingBytes.CopyTo(result, cursor + StaticRecordSize);
      return result;
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
      int recordOffset,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles)
    {
      WriteUInt32(data, recordOffset, checked((uint)vertices.Count));
      WriteUInt32(data, recordOffset + 4, 1);
      var vertexOffset = recordOffset + 8;
      for (var lane = 0; lane < vertices.Count; lane++)
      {
        var vertex = vertices[lane];
        var laneOffset = lane * sizeof(float);
        WriteSingle(data, vertexOffset + laneOffset, vertex.Position.X);
        WriteSingle(data, vertexOffset + 0x10 + laneOffset, -vertex.Position.Y);
        WriteSingle(data, vertexOffset + 0x20 + laneOffset, vertex.Position.Z);
        WriteSingle(data, vertexOffset + 0x30 + laneOffset, vertex.Normal.X);
        WriteSingle(data, vertexOffset + 0x40 + laneOffset, -vertex.Normal.Y);
        WriteSingle(data, vertexOffset + 0x50 + laneOffset, vertex.Normal.Z);
        WriteSingle(data, vertexOffset + 0x60 + laneOffset, vertex.TextureCoordinate.X);
        WriteSingle(data, vertexOffset + 0x70 + laneOffset, vertex.TextureCoordinate.Y);
        WriteUInt16(data, vertexOffset + 0x90 + (lane * sizeof(ushort)), ushort.MaxValue);
        WriteUInt16(data, vertexOffset + 0x98 + (lane * sizeof(ushort)), ushort.MaxValue);
      }

      var cursor = vertexOffset + 0xA0;
      WriteUInt32(data, cursor, 0);
      cursor += sizeof(uint);
      WriteUInt32(data, cursor, 0);
      cursor += sizeof(uint);
      WriteUInt32(data, cursor, checked((uint)triangles.Count));
      cursor += sizeof(uint);
      var triangle = triangles[0];
      WriteUInt16(data, cursor, triangle.Vertex0);
      WriteUInt16(data, cursor + 2, triangle.Vertex1);
      WriteUInt16(data, cursor + 4, triangle.Vertex2);
      WriteUInt16(data, cursor + 6, CalculateTriangleFlags(vertices, triangle));
      cursor += 8;
      cursor += 12;
      cursor += sizeof(uint) + 12;
      cursor++;
      WriteUInt32(data, cursor, 0);
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

    private static void WriteRectangle(byte[] data, int offset)
    {
      WriteSingle(data, offset, -0.25f);
      WriteSingle(data, offset + 4, 0.25f);
      WriteSingle(data, offset + 8, 0.25f);
      WriteSingle(data, offset + 12, -0.25f);
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

    private static void WriteUInt64(byte[] data, int offset, ulong value)
    {
      BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteSingle(byte[] data, int offset, float value)
    {
      WriteUInt32(data, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
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
