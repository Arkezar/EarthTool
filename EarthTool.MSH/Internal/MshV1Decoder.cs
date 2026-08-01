#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

  internal sealed class MshDecodeResult
  {
    internal MeshAsset Asset { get; }
    internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal MshDecodeResult(MeshAsset asset, IReadOnlyList<OperationDiagnostic> diagnostics)
    {
      Asset = asset;
      Diagnostics = diagnostics;
    }
  }

  internal static class MshV1Decoder
  {
    private const uint ArchiveSignature = 0x00D0A1FF;
    private const uint ArchiveTypeFlag = 0x10000000;
    private const uint CreationGuidFlag = 0x20000000;
    private const uint KnownDeclarationBits = 0x30FFFFFF;
    private const int BaseHeaderSize = 0x368;
    private const int StaticRecordSize = 0xDD;

    internal static MshDecodeResult Decode(
      byte[] source,
      MshOperationProfile profile,
      CancellationToken cancellationToken,
      MeshAssetLineageId? lineageId = null,
      MeshAssetOrigin origin = MeshAssetOrigin.Loaded,
      int staticRenderObjectLocalId = 1,
      int rootSourceObjectLocalId = 1)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var data = source.AsSpan();
      if (data.Length < sizeof(uint))
      {
        throw Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing declaration is truncated.");
      }

      var diagnostics = new List<OperationDiagnostic>();
      var declaration = ReadUInt32(data, 0);
      if ((declaration & 0x00FFFFFF) != ArchiveSignature)
      {
        throw Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing signature is invalid.");
      }

      var unknownDeclarationBits = declaration & ~KnownDeclarationBits;
      if (unknownDeclarationBits != 0)
      {
        diagnostics.Add(Compatibility(
          "ArchiveFraming.Declaration",
          0,
          "Unknown archive declaration bits were preserved.",
          new Dictionary<string, string>
          {
            ["unknownBits"] = $"0x{unknownDeclarationBits:X8}"
          }));
      }

      var cursor = sizeof(uint);
      uint? archiveType = null;
      if ((declaration & ArchiveTypeFlag) != 0)
      {
        Ensure(data, cursor, sizeof(uint), "ArchiveFraming.ArchiveType");
        archiveType = ReadUInt32(data, cursor);
        cursor += sizeof(uint);
      }

      Guid? creationGuid = null;
      if ((declaration & CreationGuidFlag) != 0)
      {
        Ensure(data, cursor, 16, "ArchiveFraming.CreationGuid");
        creationGuid = new Guid(data.Slice(cursor, 16).ToArray());
        cursor += 16;
      }

      var baseOffset = cursor;
      Ensure(data, baseOffset, BaseHeaderSize, "BaseHeader");
      var baseHeader = data.Slice(baseOffset, BaseHeaderSize);
      if (!baseHeader.Slice(0, 4).SequenceEqual(new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'H' }))
      {
        throw Structural("BaseHeader.Magic", baseOffset, "Expected MESH.");
      }

      var version = ReadUInt32(baseHeader, 4);
      if (version != 1)
      {
        throw Failure(
          MshDiagnosticCodes.UnsupportedVersion,
          1001,
          "BaseHeader.Version",
          baseOffset + 4,
          $"Unsupported MSH version {version}.");
      }

      var meshKind = ReadUInt32(baseHeader, 8);
      if (meshKind > 1)
      {
        throw Failure(
          MshDiagnosticCodes.UnsupportedMeshKind,
          1002,
          "BaseHeader.MeshKind",
          baseOffset + 8,
          $"Unsupported root mesh kind {meshKind}.");
      }

      var archiveSelectsDynamic = archiveType.GetValueOrDefault() != 0;
      var meshKindIsDynamic = meshKind == 1;
      if (archiveSelectsDynamic != meshKindIsDynamic)
      {
        diagnostics.Add(Compatibility(
          "ArchiveFraming.ArchiveType",
          sizeof(uint),
          "Archive type and root mesh kind select different payload shapes.",
          new Dictionary<string, string>
          {
            ["archiveType"] = archiveType.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
            ["meshKind"] = meshKind.ToString(CultureInfo.InvariantCulture)
          }));
      }

      var assetLineageId = lineageId ?? new MeshAssetLineageId(Guid.NewGuid());
      if (meshKindIsDynamic)
      {
        return DecodeDynamic(
          source,
          data,
          profile,
          cancellationToken,
          diagnostics,
          declaration,
          archiveType,
          creationGuid,
          baseOffset,
          baseHeader,
          assetLineageId,
          origin);
      }

      cursor = baseOffset + BaseHeaderSize;
      Ensure(data, cursor, sizeof(uint), "StoredTrailingHierarchyUnwindCount");
      var storedTrailingUnwind = ReadUInt32(data, cursor);
      if (storedTrailingUnwind != 1)
      {
        throw Unsupported("Hierarchy", "StoredTrailingHierarchyUnwindCount", cursor);
      }

      cursor += sizeof(uint);
      cancellationToken.ThrowIfCancellationRequested();
      var renderObject = DecodeRenderObject(
        data,
        cursor,
        new StaticRenderObjectId(assetLineageId, staticRenderObjectLocalId),
        out var payloadEnd);
      var trailingLength = data.Length - payloadEnd;
      if (trailingLength > profile.MaxRootTrailingBytes)
      {
        throw ResourceLimit(
          "RootTrailingBytes",
          payloadEnd,
          trailingLength,
          profile.MaxRootTrailingBytes);
      }

      var rootTrailingBytes = data.Slice(payloadEnd, trailingLength).ToArray();
      if (trailingLength != 0)
      {
        diagnostics.Add(Compatibility(
          "RootTrailingBytes",
          payloadEnd,
          "Opaque bytes after the complete root payload were preserved.",
          new Dictionary<string, string>
          {
            ["length"] = trailingLength.ToString(CultureInfo.InvariantCulture)
          }));
      }

      var asset = new StaticMeshAsset(
        assetLineageId,
        new MeshArchiveFraming(declaration, archiveType, creationGuid),
        new CommonMeshBaseHeader(baseHeader.ToArray()),
        rootTrailingBytes,
        new[] { renderObject },
        source,
        origin,
        new SourceObjectId(assetLineageId, rootSourceObjectLocalId));
      return new MshDecodeResult(asset, CapDiagnostics(diagnostics, profile.MaxDiagnostics));
    }

    private static MshDecodeResult DecodeDynamic(
      byte[] source,
      ReadOnlySpan<byte> data,
      MshOperationProfile profile,
      CancellationToken cancellationToken,
      List<OperationDiagnostic> diagnostics,
      uint declaration,
      uint? archiveType,
      Guid? creationGuid,
      int baseOffset,
      ReadOnlySpan<byte> baseHeader,
      MeshAssetLineageId lineageId,
      MeshAssetOrigin origin)
    {
      Ensure(data, baseOffset, MshCanonicalSerializer.DynamicRecordSize, "DynamicObject");
      var childCountOffset = baseOffset + 0x40C;
      if (ReadUInt32(data, childCountOffset) != 0)
      {
        throw Unsupported("DynamicChildren", "DynamicObject.Children", childCountOffset);
      }

      var canonicalRecord = MshCanonicalSerializer.CreateCanonicalDynamicRecord();
      if (!data.Slice(baseOffset, canonicalRecord.Length).SequenceEqual(canonicalRecord))
      {
        throw Unsupported("DynamicObject", "DynamicObject", baseOffset);
      }

      cancellationToken.ThrowIfCancellationRequested();
      var payloadEnd = baseOffset + canonicalRecord.Length;
      var trailingLength = data.Length - payloadEnd;
      if (trailingLength > profile.MaxRootTrailingBytes)
      {
        throw ResourceLimit(
          "RootTrailingBytes",
          payloadEnd,
          trailingLength,
          profile.MaxRootTrailingBytes);
      }

      var rootTrailingBytes = data.Slice(payloadEnd, trailingLength).ToArray();
      if (trailingLength != 0)
      {
        diagnostics.Add(Compatibility(
          "RootTrailingBytes",
          payloadEnd,
          "Opaque bytes after the complete root payload were preserved.",
          new Dictionary<string, string>
          {
            ["length"] = trailingLength.ToString(CultureInfo.InvariantCulture)
          }));
      }

      var asset = new DynamicMeshAsset(
        lineageId,
        new MeshArchiveFraming(declaration, archiveType, creationGuid),
        new CommonMeshBaseHeader(baseHeader.ToArray()),
        new DynamicObject(Array.Empty<DynamicObject>()),
        rootTrailingBytes,
        source,
        origin);
      return new MshDecodeResult(asset, CapDiagnostics(diagnostics, profile.MaxDiagnostics));
    }

    private static StaticRenderObject DecodeRenderObject(
      ReadOnlySpan<byte> data,
      int recordOffset,
      StaticRenderObjectId id,
      out int payloadEnd)
    {
      Ensure(data, recordOffset, StaticRecordSize, "StaticRenderObjectSequence[0]");
      if (ReadUInt32(data, recordOffset) != 3 || ReadUInt32(data, recordOffset + 4) != 1)
      {
        throw Unsupported("Geometry", "StaticRenderObjectSequence[0].RenderVertices", recordOffset);
      }

      var vertices = new RenderVertex[3];
      var vertexDataOffset = recordOffset + 8;
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
          throw Unsupported(
            "Geometry",
            $"StaticRenderObjectSequence[0].RenderVertices[{lane}]",
            vertexDataOffset);
        }

        vertices[lane] = new RenderVertex(position, normal, textureCoordinate);
      }

      for (var offset = vertexDataOffset + 0x0C; offset < vertexDataOffset + 0xA0; offset++)
      {
        if (!IsActiveVertexByte(offset - vertexDataOffset) && data[offset] != 0)
        {
          throw Unsupported(
            "VertexBlockPadding",
            "StaticRenderObjectSequence[0].VertexBlockPadding",
            offset);
        }
      }

      var cursor = vertexDataOffset + 0xA0;
      if (ReadUInt32(data, cursor) != 0)
      {
        throw Unsupported("Hierarchy", "StaticRenderObjectSequence[0].ObjectFlags", cursor);
      }

      cursor += 4;
      if (ReadUInt32(data, cursor) != 0)
      {
        throw Unsupported("Texture", "StaticRenderObjectSequence[0].Texture", cursor);
      }

      cursor += 4;
      if (ReadUInt32(data, cursor) != 1)
      {
        throw Unsupported("Geometry", "StaticRenderObjectSequence[0].Triangles", cursor);
      }

      cursor += 4;
      var triangle = new StaticTriangle(
        ReadUInt16(data, cursor),
        ReadUInt16(data, cursor + 2),
        ReadUInt16(data, cursor + 4),
        ReadUInt16(data, cursor + 6));
      if (triangle.Vertex0 >= 3 || triangle.Vertex1 >= 3 || triangle.Vertex2 >= 3
        || (triangle.TriangleRenderPassFlags & 1) == 0
        || (triangle.TriangleRenderPassFlags & ~3) != 0)
      {
        throw Unsupported("Geometry", "StaticRenderObjectSequence[0].Triangles[0]", cursor);
      }

      cursor += 8;
      if (ReadUInt32(data, cursor) != 0
        || ReadUInt32(data, cursor + 4) != 0
        || ReadUInt32(data, cursor + 8) != 0)
      {
        throw Unsupported("Animation", "StaticRenderObjectSequence[0].AnimationTracks", cursor);
      }

      cursor += 12;
      if (ReadUInt32(data, cursor) != 0 || !data.Slice(cursor + 4, 13).SequenceEqual(new byte[13]))
      {
        throw Unsupported("Transform", "StaticRenderObjectSequence[0].Transform", cursor);
      }

      cursor += 17;
      if (ReadUInt32(data, cursor) != 0)
      {
        throw Unsupported("Hierarchy", "StaticRenderObjectSequence[0].NextRecordMarker", cursor);
      }

      payloadEnd = cursor + sizeof(uint);
      return new StaticRenderObject(id, vertices, new[] { triangle });
    }

    private static IReadOnlyList<OperationDiagnostic> CapDiagnostics(
      IReadOnlyList<OperationDiagnostic> diagnostics,
      int maximum)
    {
      if (diagnostics.Count <= maximum)
      {
        return diagnostics;
      }

      var retainedDiagnosticCount = maximum - 1;
      var suppressedDiagnosticCount = diagnostics.Count - retainedDiagnosticCount;
      var retained = diagnostics.Take(retainedDiagnosticCount).ToList();
      retained.Add(new OperationDiagnostic(
        MshDiagnosticCodes.DiagnosticsTruncated,
        1010,
        DiagnosticSeverity.Warning,
        "$",
        "Additional diagnostics were suppressed by the operation profile.",
        data: new Dictionary<string, string>
        {
          ["suppressed"] = suppressedDiagnosticCount.ToString(CultureInfo.InvariantCulture)
        }));
      return retained;
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

    private static void Ensure(ReadOnlySpan<byte> data, int offset, int length, string path)
    {
      if (offset < 0 || length < 0 || offset > data.Length - length)
      {
        throw Structural(path, Math.Min(offset, data.Length), "The serialized representation is truncated.");
      }
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
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

    private static MshContentException ResourceLimit(
      string path,
      long offset,
      long actual,
      long maximum)
    {
      return new MshContentException(new OperationDiagnostic(
        MshDiagnosticCodes.ResourceLimitExceeded,
        1004,
        DiagnosticSeverity.Error,
        path,
        "The serialized representation exceeds the configured operation profile.",
        offset,
        new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(CultureInfo.InvariantCulture)
        }));
    }

    private static MshContentException Unsupported(string domain, string path, long offset)
    {
      return new MshContentException(new OperationDiagnostic(
        MshDiagnosticCodes.UnsupportedDomain,
        1005,
        DiagnosticSeverity.Error,
        path,
        $"The {domain} domain is outside the current safe MSH slice.",
        offset,
        new Dictionary<string, string> { ["domain"] = domain }));
    }

    private static OperationDiagnostic Compatibility(
      string path,
      long offset,
      string message,
      IReadOnlyDictionary<string, string> data)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.CompatibilityAnomaly,
        1009,
        DiagnosticSeverity.Warning,
        path,
        message,
        offset,
        data);
    }

    private static MshContentException Failure(
      string code,
      int eventId,
      string path,
      long offset,
      string message)
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
