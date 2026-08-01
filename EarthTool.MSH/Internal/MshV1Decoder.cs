#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace EarthTool.MSH.Internal
{
  internal sealed class MshContentException : Exception
  {
    internal OperationDiagnostic Diagnostic { get; }

    internal MshContentException(OperationDiagnostic diagnostic)
      : base(diagnostic.Message)
    {
      Diagnostic = diagnostic;
    }
  }

  internal static class MshV1Decoder
  {
    private const int ArchiveHeaderSize = 0x14;
    private const int BaseHeaderSize = 0x368;
    private const int AttachmentOffset = 0x1D8;
    private const int AttachmentCount = 49;
    private const int AttachmentSize = 8;
    private const int RecordOffset = 0x380;
    private const int ExpectedLength = 0x45D;

    internal static StaticMeshAsset Decode(byte[] source, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (source.Length != ExpectedLength)
      {
        throw Structural("$", 0, $"The one-triangle profile requires exactly {ExpectedLength} bytes.");
      }

      var data = source.AsSpan();
      var declaration = ReadUInt32(data, 0);
      if (declaration != 0x20D0A1FF)
      {
        throw Failure(MshDiagnosticCodes.InvalidFraming, 1000, "ArchiveFraming", 0,
          "The one-triangle profile requires GUID-only framed MSH input.");
      }

      var creationGuid = new Guid(data.Slice(4, 16).ToArray());
      var baseHeader = data.Slice(ArchiveHeaderSize, BaseHeaderSize);
      if (!baseHeader.Slice(0, 4).SequenceEqual(new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'H' }))
      {
        throw Structural("BaseHeader.Magic", ArchiveHeaderSize, "Expected MESH.");
      }

      var version = ReadUInt32(baseHeader, 4);
      if (version != 1)
      {
        throw Failure(MshDiagnosticCodes.UnsupportedVersion, 1001, "BaseHeader.Version", ArchiveHeaderSize + 4,
          $"Unsupported MSH version {version}.");
      }

      var meshKind = ReadUInt32(baseHeader, 8);
      if (meshKind != 0)
      {
        throw Failure(MshDiagnosticCodes.UnsupportedMeshKind, 1002, "BaseHeader.MeshKind", ArchiveHeaderSize + 8,
          "Dynamic MSH transport is outside the walking-skeleton profile.");
      }

      ValidateBaseHeader(baseHeader);
      if (ReadUInt32(data, 0x37C) != 1)
      {
        throw Unsupported("Hierarchy", "StoredTrailingHierarchyUnwindCount", 0x37C);
      }

      cancellationToken.ThrowIfCancellationRequested();
      var renderObject = DecodeRenderObject(data);
      return new StaticMeshAsset(
        new MeshArchiveFraming(declaration, null, creationGuid),
        new[] { renderObject },
        source);
    }

    private static void ValidateBaseHeader(ReadOnlySpan<byte> baseHeader)
    {
      for (var attachment = 0; attachment < AttachmentCount; attachment++)
      {
        var offset = AttachmentOffset + (attachment * AttachmentSize);
        if (ReadInt16(baseHeader, offset) != short.MinValue
          || ReadInt16(baseHeader, offset + 2) != short.MinValue
          || ReadInt16(baseHeader, offset + 4) != short.MinValue
          || baseHeader[offset + 6] != 0
          || baseHeader[offset + 7] != 0)
        {
          throw Unsupported("Attachment", $"BaseHeader.Attachments[{attachment + 1}]", ArchiveHeaderSize + offset);
        }
      }

      for (var offset = 12; offset < baseHeader.Length; offset++)
      {
        if (offset >= AttachmentOffset && offset < AttachmentOffset + (AttachmentCount * AttachmentSize))
        {
          continue;
        }

        if (baseHeader[offset] != 0)
        {
          throw Unsupported("BaseHeader", "BaseHeader", ArchiveHeaderSize + offset);
        }
      }
    }

    private static StaticRenderObject DecodeRenderObject(ReadOnlySpan<byte> data)
    {
      if (ReadUInt32(data, RecordOffset) != 3 || ReadUInt32(data, RecordOffset + 4) != 1)
      {
        throw Unsupported("Geometry", "StaticRenderObject.Vertices", RecordOffset);
      }

      var vertices = new RenderVertex[3];
      var vertexDataOffset = RecordOffset + 8;
      for (var lane = 0; lane < vertices.Length; lane++)
      {
        var laneFloatOffset = lane * sizeof(float);
        var position = new Vector3(
          ReadSingle(data, vertexDataOffset + laneFloatOffset),
          -ReadSingle(data, vertexDataOffset + 0x10 + laneFloatOffset),
          ReadSingle(data, vertexDataOffset + 0x20 + laneFloatOffset));
        var normal = new Vector3(
          ReadSingle(data, vertexDataOffset + 0x30 + laneFloatOffset),
          -ReadSingle(data, vertexDataOffset + 0x40 + laneFloatOffset),
          ReadSingle(data, vertexDataOffset + 0x50 + laneFloatOffset));
        var textureCoordinate = new Vector2(
          ReadSingle(data, vertexDataOffset + 0x60 + laneFloatOffset),
          ReadSingle(data, vertexDataOffset + 0x70 + laneFloatOffset));
        var reserved = ReadSingle(data, vertexDataOffset + 0x80 + laneFloatOffset);
        var normalSharing = ReadUInt16(data, vertexDataOffset + 0x90 + (lane * sizeof(ushort)));
        var positionSharing = ReadUInt16(data, vertexDataOffset + 0x98 + (lane * sizeof(ushort)));

        if (!IsFinite(position) || !IsFinite(normal) || !IsFinite(textureCoordinate)
          || reserved != 0 || normalSharing != ushort.MaxValue || positionSharing != ushort.MaxValue)
        {
          throw Unsupported("Geometry", $"StaticRenderObject.RenderVertices[{lane}]", vertexDataOffset);
        }

        vertices[lane] = new RenderVertex(position, normal, textureCoordinate);
      }

      for (var offset = vertexDataOffset + 0x0C; offset < vertexDataOffset + 0xA0; offset++)
      {
        var inActiveChannel = IsActiveVertexByte(offset - vertexDataOffset);
        if (!inActiveChannel && data[offset] != 0)
        {
          throw Unsupported("VertexBlockPadding", "StaticRenderObject.VertexBlockPadding", offset);
        }
      }

      var cursor = vertexDataOffset + 0xA0;
      if (ReadUInt32(data, cursor) != 0)
      {
        throw Unsupported("Hierarchy", "StaticRenderObject.ObjectFlags", cursor);
      }

      cursor += 4;
      if (ReadUInt32(data, cursor) != 0)
      {
        throw Unsupported("Texture", "StaticRenderObject.Texture", cursor);
      }

      cursor += 4;
      if (ReadUInt32(data, cursor) != 1)
      {
        throw Unsupported("Geometry", "StaticRenderObject.Triangles", cursor);
      }

      cursor += 4;
      var triangle = new StaticTriangle(
        ReadUInt16(data, cursor),
        ReadUInt16(data, cursor + 2),
        ReadUInt16(data, cursor + 4),
        ReadUInt16(data, cursor + 6));
      if (triangle.Vertex0 >= 3 || triangle.Vertex1 >= 3 || triangle.Vertex2 >= 3
        || triangle.TriangleRenderPassFlags != 1)
      {
        throw Unsupported("Geometry", "StaticRenderObject.Triangles[0]", cursor);
      }

      cursor += 8;
      if (ReadUInt32(data, cursor) != 0
        || ReadUInt32(data, cursor + 4) != 0
        || ReadUInt32(data, cursor + 8) != 0)
      {
        throw Unsupported("Animation", "StaticRenderObject.AnimationTracks", cursor);
      }

      cursor += 12;
      if (ReadUInt32(data, cursor) != 0 || !data.Slice(cursor + 4, 13).SequenceEqual(new byte[13]))
      {
        throw Unsupported("Transform", "StaticRenderObject.Transform", cursor);
      }

      cursor += 17;
      if (ReadUInt32(data, cursor) != 0 || cursor + 4 != data.Length)
      {
        throw Unsupported("Hierarchy", "StaticRenderObject.NextRecordMarker", cursor);
      }

      return new StaticRenderObject(1, vertices, new[] { triangle });
    }

    private static bool IsActiveVertexByte(int offset)
    {
      var channelOffset = offset % 0x10;
      if (offset < 0x90)
      {
        return channelOffset < 0x0C;
      }

      var sharingOffset = offset % 8;
      return sharingOffset < 6;
    }

    private static bool IsFinite(Vector3 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }

    private static bool IsFinite(Vector2 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y);
    }

    private static bool IsFinite(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    }

    private static short ReadInt16(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset, 2));
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    }

    private static float ReadSingle(ReadOnlySpan<byte> data, int offset)
    {
      return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4)));
    }

    private static MshContentException Structural(string path, long offset, string message)
    {
      return Failure(MshDiagnosticCodes.StructuralHazard, 1003, path, offset, message);
    }

    private static MshContentException Unsupported(string domain, string path, long offset)
    {
      return new MshContentException(new OperationDiagnostic(
        MshDiagnosticCodes.UnsupportedDomain,
        1005,
        DiagnosticSeverity.Error,
        path,
        $"The {domain} domain is outside the one-triangle walking-skeleton profile.",
        offset,
        new Dictionary<string, string> { ["domain"] = domain }));
    }

    private static MshContentException Failure(string code, int eventId, string path, long offset, string message)
    {
      return new MshContentException(new OperationDiagnostic(
        code,
        eventId,
        DiagnosticSeverity.Error,
        path,
        message,
        offset));
    }
  }
}
