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
  internal static class CanonicalStaticRenderObjectSequenceEncoder
  {
    private const int VertexBlockSize = 0xA0;
    private const int FixedRecordSize = 53;

    internal static IReadOnlyList<CanonicalStaticRenderObject> GetCanonicalSequence(
      CanonicalStaticSourceObject root
    )
    {
      if (root is null)
      {
        throw new ArgumentNullException(nameof(root));
      }
      return Array.AsReadOnly(Flatten(root).Select(record => record.RenderObject).ToArray());
    }

    internal static int GetSerializedLength(
      CanonicalStaticSourceObject root,
      IReadOnlyDictionary<int, StaticAnimationReplacement>? animations = null,
      IReadOnlyDictionary<int, string?>? textureResourceBindings = null
    )
    {
      animations ??= new Dictionary<int, StaticAnimationReplacement>();
      return GetSerializedLength(Flatten(root), animations, textureResourceBindings);
    }

    internal static long GetMinimumSerializedLength(
      IEnumerable<(int VertexCount, int TriangleCount)> geometry
    )
    {
      var length = (long)sizeof(uint);
      foreach (var record in geometry)
      {
        var blocks = checked((record.VertexCount + 3L) / 4L);
        length = checked(
          length + FixedRecordSize + (blocks * VertexBlockSize) + (record.TriangleCount * 8L)
        );
      }
      return length;
    }

    internal static byte[] Encode(
      CanonicalStaticSourceObject root,
      IReadOnlyDictionary<int, Vector3>? pivots = null,
      IReadOnlyDictionary<int, StaticAnimationReplacement>? animations = null,
      IReadOnlyDictionary<int, string?>? textureResourceBindings = null
    )
    {
      pivots ??= new Dictionary<int, Vector3>();
      animations ??= new Dictionary<int, StaticAnimationReplacement>();
      var records = Flatten(root);
      var result = new byte[GetSerializedLength(records, animations, textureResourceBindings)];
      var cursor = 0;
      WriteUInt32(result, cursor, checked((uint)records[^1].Depth + 1));
      cursor += sizeof(uint);
      for (var index = 0; index < records.Count; index++)
      {
        var record = records[index];
        var textureResourceKey = GetTextureResourceKey(
          record.RenderObject,
          index,
          textureResourceBindings
        );
        var texturePathBytes = textureResourceKey is null
          ? Array.Empty<byte>()
          : Encoding.ASCII.GetBytes(textureResourceKey);
        WriteRecord(
          result,
          ref cursor,
          record.RenderObject.RenderVertices,
          record.RenderObject.Triangles,
          texturePathBytes,
          record.ObjectFlags,
          ReferenceEquals(record.RenderObject, record.Source.RenderObjects[0])
            ? record.Source.Role?.BarrelMaximumAngle ?? 0
            : (byte)0,
          index == records.Count - 1 ? 0u : 1u,
          pivots.TryGetValue(index, out var pivot) ? pivot : Vector3.Zero,
          animations.TryGetValue(index, out var animation) ? animation : null
        );
      }
      return result;
    }

    internal static byte[] EncodeRecord(
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles,
      IReadOnlyList<byte> texturePathBytes,
      uint objectFlags,
      byte barrelMaximumAngle,
      uint nextRecordMarker,
      Vector3 pivot,
      StaticAnimationReplacement? animation
    )
    {
      var result = new byte[GetRecordLength(
        vertices.Count,
        triangles.Count,
        texturePathBytes.Count,
        animation
      )];
      var cursor = 0;
      WriteRecord(
        result,
        ref cursor,
        vertices,
        triangles,
        texturePathBytes,
        objectFlags,
        barrelMaximumAngle,
        nextRecordMarker,
        pivot,
        animation
      );
      return result;
    }

    private static int GetSerializedLength(
      IReadOnlyList<CanonicalStaticRecord> records,
      IReadOnlyDictionary<int, StaticAnimationReplacement> animations,
      IReadOnlyDictionary<int, string?>? textureResourceBindings
    )
    {
      var length = sizeof(uint);
      for (var index = 0; index < records.Count; index++)
      {
        var record = records[index];
        var blocks = (record.RenderObject.RenderVertices.Count + 3) / 4;
        var textureResourceKey = GetTextureResourceKey(
          record.RenderObject,
          index,
          textureResourceBindings
        );
        var texturePathLength = textureResourceKey is null
          ? 0
          : Encoding.ASCII.GetByteCount(textureResourceKey);
        animations.TryGetValue(index, out var animation);
        length = checked(length + GetRecordLength(
          record.RenderObject.RenderVertices.Count,
          record.RenderObject.Triangles.Count,
          texturePathLength,
          animation
        ));
      }
      return length;
    }

    private static string? GetTextureResourceKey(
      CanonicalStaticRenderObject renderObject,
      int ordinal,
      IReadOnlyDictionary<int, string?>? textureResourceBindings
    )
    {
      return textureResourceBindings is null
        ? renderObject.TextureResourceKey
        : textureResourceBindings.TryGetValue(ordinal, out var binding)
          ? binding
          : null;
    }

    private static int GetRecordLength(
      int vertexCount,
      int triangleCount,
      int texturePathLength,
      StaticAnimationReplacement? animation
    )
    {
      var blocks = (vertexCount + 3) / 4;
      return checked(
        FixedRecordSize
        + blocks * VertexBlockSize
        + texturePathLength
        + triangleCount * 8
        + (animation?.Tracks.ScaleFrames.Count ?? 0) * 12
        + (animation?.Tracks.TranslationFrames.Count ?? 0) * 12
        + (animation?.Tracks.Matrices.Count ?? 0) * 64
      );
    }

    private static IReadOnlyList<CanonicalStaticRecord> Flatten(
      CanonicalStaticSourceObject root
    )
    {
      var records = new List<CanonicalStaticRecord>();
      Flatten(root, 0, records);
      if (records.Count == 0)
      {
        return records.AsReadOnly();
      }
      var encounteredSources = new HashSet<CanonicalStaticSourceObject> { records[0].Source };
      for (var index = 1; index < records.Count; index++)
      {
        var current = records[index];
        if (ReferenceEquals(current.Source, records[index - 1].Source))
        {
          continue;
        }

        // A new source descends from the previous record; an established source is an unwind target.
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
      return records.AsReadOnly();
    }

    private static void Flatten(
      CanonicalStaticSourceObject source,
      int depth,
      ICollection<CanonicalStaticRecord> records
    )
    {
      if (source.RenderObjects.Count == 0)
      {
        return;
      }
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
      foreach (var renderObject in source.RenderObjects.Skip(1))
      {
        records.Add(new CanonicalStaticRecord(source, renderObject, depth));
      }
    }

    private static void WriteRecord(
      byte[] data,
      ref int cursor,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles,
      IReadOnlyList<byte> texturePathBytes,
      uint objectFlags,
      byte barrelMaximumAngle,
      uint nextRecordMarker,
      Vector3 pivot,
      StaticAnimationReplacement? animation
    )
    {
      var recordOffset = cursor;
      WriteUInt32(data, recordOffset, checked((uint)vertices.Count));
      var blockCount = (vertices.Count + 3) / 4;
      WriteUInt32(data, recordOffset + sizeof(uint), checked((uint)blockCount));

      // The structure-of-arrays block is zero-initialized, including padding lanes and W values.
      var vertexOffset = recordOffset + (2 * sizeof(uint));
      for (var lane = 0; lane < vertices.Count; lane++)
      {
        var vertex = vertices[lane];
        var blockOffset = vertexOffset + lane / 4 * VertexBlockSize;
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

      cursor = vertexOffset + blockCount * VertexBlockSize;
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

      WriteAnimationTail(
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

    private static void WriteAnimationTail(
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

    private static void WriteVector3(byte[] data, int offset, Vector3 value, bool invertY)
    {
      WriteSingle(data, offset, value.X);
      WriteSingle(data, offset + 4, invertY ? -value.Y : value.Y);
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
        WriteSingle(data, offset + index * sizeof(float), values[index]);
      }
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
      BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteSingle(byte[] data, int offset, float value)
    {
      WriteUInt32(
        data,
        offset,
        unchecked((uint)BitConverter.SingleToInt32Bits(value == 0 ? 0 : value))
      );
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
  }
}
