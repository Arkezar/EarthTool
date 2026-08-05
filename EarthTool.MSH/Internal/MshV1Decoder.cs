#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using System;
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
      int rootSourceObjectLocalId = 1,
      IReadOnlyList<int>? staticRenderObjectLocalIds = null,
      IReadOnlyList<int>? sourceObjectLocalIds = null,
      int? nextStaticRenderObjectLocalId = null,
      int? nextSourceObjectLocalId = null
    )
    {
      var context = new MshDecodeContext(source, profile, cancellationToken);
      context.ThrowIfCancellationRequested();
      var data = context.Data;
      if (data.Length < sizeof(uint))
      {
        throw context.Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing declaration is truncated."
        );
      }

      var declaration = context.ReadUInt32(0);
      if ((declaration & 0x00FFFFFF) != ArchiveSignature)
      {
        throw context.Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing signature is invalid."
        );
      }

      var unknownDeclarationBits = declaration & ~KnownDeclarationBits;
      if (unknownDeclarationBits != 0)
      {
        context.AddDiagnostic(
          context.Compatibility(
          "ArchiveFraming.Declaration",
          0,
          "Unknown archive declaration bits were preserved.",
            new Dictionary<string, string> { ["unknownBits"] = $"0x{unknownDeclarationBits:X8}" }
          )
        );
      }

      var cursor = sizeof(uint);
      uint? archiveType = null;
      if ((declaration & ArchiveTypeFlag) != 0)
      {
        context.Ensure(cursor, sizeof(uint), "ArchiveFraming.ArchiveType");
        archiveType = context.ReadUInt32(cursor);
        cursor += sizeof(uint);
      }

      Guid? creationGuid = null;
      if ((declaration & CreationGuidFlag) != 0)
      {
        context.Ensure(cursor, 16, "ArchiveFraming.CreationGuid");
        creationGuid = new Guid(data.Slice(cursor, 16).ToArray());
        cursor += 16;
      }

      var baseOffset = cursor;
      context.Ensure(baseOffset, BaseHeaderSize, "BaseHeader");
      var baseHeader = data.Slice(baseOffset, BaseHeaderSize);
      if (
        !baseHeader
          .Slice(0, 4)
          .SequenceEqual(new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'H' })
      )
      {
        throw context.Structural("BaseHeader.Magic", baseOffset, "Expected MESH.");
      }

      var version = context.ReadUInt32(baseOffset + 4);
      if (version != 1)
      {
        throw context.Failure(
          MshDiagnosticCodes.UnsupportedVersion,
          1001,
          "BaseHeader.Version",
          baseOffset + 4,
          $"Unsupported MSH version {version}."
        );
      }

      var meshKind = context.ReadUInt32(baseOffset + 8);
      if (meshKind > 1)
      {
        throw context.Failure(
          MshDiagnosticCodes.UnsupportedMeshKind,
          1002,
          "BaseHeader.MeshKind",
          baseOffset + 8,
          $"Unsupported root mesh kind {meshKind}."
        );
      }

      var archiveSelectsDynamic = archiveType.GetValueOrDefault() != 0;
      var meshKindIsDynamic = meshKind == 1;
      if (archiveSelectsDynamic != meshKindIsDynamic)
      {
        context.AddDiagnostic(
          context.Compatibility(
          "ArchiveFraming.ArchiveType",
          sizeof(uint),
          "Archive type and root mesh kind select different payload shapes.",
          new Dictionary<string, string>
          {
              ["archiveType"] = archiveType
                .GetValueOrDefault()
                .ToString(CultureInfo.InvariantCulture),
              ["meshKind"] = meshKind.ToString(CultureInfo.InvariantCulture),
            }
          )
        );
      }

      var assetLineageId = lineageId ?? new MeshAssetLineageId(Guid.NewGuid());
      var framing = new MeshArchiveFraming(declaration, archiveType, creationGuid);
      if (meshKindIsDynamic)
      {
        return DynamicMeshDecoder.Decode(
          context,
          framing,
          baseOffset,
          assetLineageId,
          origin
        );
      }

      cursor = baseOffset + BaseHeaderSize;
      context.Ensure(cursor, sizeof(uint), "StoredTrailingHierarchyUnwindCount");
      var storedTrailingUnwind = context.ReadUInt32(cursor);
      cursor += sizeof(uint);
      var commonBaseHeader = new CommonMeshBaseHeader(baseHeader.ToArray());
      var decodedRecords = new List<DecodedStaticRecord>();
      var absoluteVertexCount = 0;
      while (true)
      {
        context.ThrowIfCancellationRequested();
        if (decodedRecords.Count == profile.MaxStaticRenderObjects)
        {
          throw context.ResourceLimit(
            "StaticRenderObjectSequence",
            cursor,
            (long)decodedRecords.Count + 1,
            profile.MaxStaticRenderObjects
          );
        }

        var record = DecodeRenderObject(
          context,
          cursor,
          decodedRecords.Count,
          absoluteVertexCount,
          commonBaseHeader,
          out cursor
        );
        decodedRecords.Add(record);
        absoluteVertexCount = checked(absoluteVertexCount + record.RenderVertices.Count);
        if (record.NextRecordMarker == 0)
        {
          break;
        }
      }

      var hierarchy = ReconstructHierarchy(
        decodedRecords,
        assetLineageId,
        rootSourceObjectLocalId,
        sourceObjectLocalIds,
        context
      );
      var expectedTrailingUnwind = checked((uint)hierarchy.FinalDepth + 1);
      if (storedTrailingUnwind != expectedTrailingUnwind)
      {
        throw context.Structural(
          "StoredTrailingHierarchyUnwindCount",
          baseOffset + BaseHeaderSize,
          $"Expected {expectedTrailingUnwind}, found {storedTrailingUnwind}."
        );
      }

      if (
        staticRenderObjectLocalIds is not null
        && staticRenderObjectLocalIds.Count != decodedRecords.Count
      )
      {
        throw new ArgumentException(
          "Static render-object identities must match the decoded sequence.",
          nameof(staticRenderObjectLocalIds)
        );
      }

      var renderObjects = decodedRecords
        .Select(
          (record, index) =>
            new StaticRenderObject(
        new StaticRenderObjectId(
          assetLineageId,
                staticRenderObjectLocalIds?[index] ?? checked(staticRenderObjectLocalId + index)
              ),
        hierarchy.RecordSourceIds[index],
        record.RenderVertices,
        record.Triangles,
        record.VertexBlockCount,
        record.VertexBlockPadding,
        record.ObjectFlags,
        record.TexturePathBytes,
        record.AnimationTracks,
        record.AnimationClassValue,
        record.Pivot,
        record.BarrelMaximumAngle,
        record.NextRecordMarker,
              record.SerializedRepresentation
            )
        )
        .ToArray();
      hierarchy.AssignRenderObjectIds(renderObjects);
      var nextRenderObjectId = ResolveNextLocalId(
        renderObjects.Select(record => record.LocalId),
        nextStaticRenderObjectLocalId,
        nameof(nextStaticRenderObjectLocalId)
      );
      var nextSourceId = ResolveNextLocalId(
        GetSourceObjectLocalIds(hierarchy.BuildRoot()),
        nextSourceObjectLocalId,
        nameof(nextSourceObjectLocalId)
      );
      var payloadEnd = cursor;
      var trailingLength = data.Length - payloadEnd;
      if (trailingLength > profile.MaxRootTrailingBytes)
      {
        throw context.ResourceLimit(
          "RootTrailingBytes",
          payloadEnd,
          trailingLength,
          profile.MaxRootTrailingBytes
        );
      }

      var rootTrailingBytes = data.Slice(payloadEnd, trailingLength).ToArray();
      if (trailingLength != 0)
      {
        context.AddDiagnostic(
          context.Compatibility(
          "RootTrailingBytes",
          payloadEnd,
          "Opaque bytes after the complete root payload were preserved.",
          new Dictionary<string, string>
          {
              ["length"] = trailingLength.ToString(CultureInfo.InvariantCulture),
            }
          )
        );
      }

      var asset = new StaticMeshAsset(
        assetLineageId,
        framing,
        commonBaseHeader,
        rootTrailingBytes,
        renderObjects,
        source,
        origin,
        hierarchy.BuildRoot(),
        storedTrailingUnwind,
        expectedTrailingUnwind,
        nextRenderObjectId,
        nextSourceId
      );
      return context.Complete(asset);
    }

    private static int? ResolveNextLocalId(
      IEnumerable<int> localIds,
      int? requested,
      string parameterName
    )
    {
      var maximum = localIds.Max();
      if (requested.HasValue && requested.Value <= maximum)
      {
        throw new ArgumentOutOfRangeException(
          parameterName,
          "The next lineage-local identity must exceed every allocated identity."
        );
      }
      return requested ?? (maximum == int.MaxValue ? null : maximum + 1);
    }

    private static IEnumerable<int> GetSourceObjectLocalIds(StaticSourceObject source)
    {
      yield return source.Id.Value;
      foreach (var child in source.Children)
      {
        foreach (var id in GetSourceObjectLocalIds(child))
        {
          yield return id;
        }
      }
    }

    private static DecodedStaticRecord DecodeRenderObject(
      MshDecodeContext context,
      int recordOffset,
      int recordIndex,
      int absoluteVertexStart,
      CommonMeshBaseHeader commonHeader,
      out int payloadEnd
    )
    {
      var data = context.Data;
      var profile = context.Profile;
      var path = $"StaticRenderObjectSequence[{recordIndex}]";
      context.Ensure(recordOffset, 8, path + ".RenderVertices");
      var vertexCount = context.ReadUInt32(recordOffset);
      var blockCount = context.ReadUInt32(recordOffset + 4);
      if (vertexCount > profile.MaxStaticVerticesPerObject)
      {
        throw context.ResourceLimit(
          path + ".RenderVertices",
          recordOffset,
          vertexCount,
          profile.MaxStaticVerticesPerObject
        );
      }

      if (blockCount > profile.MaxStaticVertexBlocksPerObject)
      {
        throw context.ResourceLimit(
          path + ".VertexBlockCount",
          recordOffset + 4,
          blockCount,
          profile.MaxStaticVertexBlocksPerObject
        );
      }

      var minimumBlockCount = (vertexCount + 3) / 4;
      if (blockCount < minimumBlockCount)
      {
        throw context.Structural(
          path + ".VertexBlockCount",
          recordOffset + 4,
          "The declared vertex blocks cannot contain all active render vertices."
        );
      }

      context.EnsureCounted(recordOffset + 8, blockCount, 0xA0, path + ".VertexBlocks");
      if (blockCount > minimumBlockCount)
      {
        context.AddDiagnosticBounded(
          context.Compatibility(
          path + ".VertexBlockCount",
          recordOffset + 4,
          "Excess physical vertex blocks were preserved as padding.",
            new Dictionary<string, string>()
          )
        );
      }

      var vertices = new RenderVertex[(int)vertexCount];
      var vertexDataOffset = recordOffset + 8;
      for (var lane = 0; lane < vertices.Length; lane++)
      {
        var blockOffset = vertexDataOffset + lane / 4 * 0xA0;
        var laneOffset = lane % 4;
        var laneFloatOffset = laneOffset * sizeof(float);
        var position = new Vector3(
          context.ReadSingle(blockOffset + laneFloatOffset),
          -context.ReadSingle(blockOffset + 0x10 + laneFloatOffset),
          context.ReadSingle(blockOffset + 0x20 + laneFloatOffset)
        );
        var normal = new Vector3(
          context.ReadSingle(blockOffset + 0x30 + laneFloatOffset),
          -context.ReadSingle(blockOffset + 0x40 + laneFloatOffset),
          context.ReadSingle(blockOffset + 0x50 + laneFloatOffset)
        );
        var textureCoordinate = new Vector2(
          context.ReadSingle(blockOffset + 0x60 + laneFloatOffset),
          context.ReadSingle(blockOffset + 0x70 + laneFloatOffset)
        );
        var reserved = context.ReadSingle(blockOffset + 0x80 + laneFloatOffset);
        var normalSharing = context.ReadUInt16(blockOffset + 0x90 + laneOffset * sizeof(ushort));
        var positionSharing = context.ReadUInt16(blockOffset + 0x98 + laneOffset * sizeof(ushort));
        ValidateSharingLink(
          context,
          normalSharing,
          absoluteVertexStart + lane,
          path,
          lane,
          "NormalSharingIndex",
          blockOffset
        );
        ValidateSharingLink(
          context,
          positionSharing,
          absoluteVertexStart + lane,
          path,
          lane,
          "PositionSharingIndex",
          blockOffset
        );
        vertices[lane] = new RenderVertex(
          position,
          normal,
          textureCoordinate,
          reserved,
          normalSharing,
          positionSharing
        );
      }

      var padding = new List<byte>();
      for (var lane = vertices.Length; lane < checked((int)blockCount * 4); lane++)
      {
        var blockOffset = vertexDataOffset + lane / 4 * 0xA0;
        var laneOffset = lane % 4;
        for (var channel = 0; channel < 9; channel++)
        {
          padding.AddRange(data.Slice(blockOffset + channel * 0x10 + laneOffset * 4, 4).ToArray());
        }

        padding.AddRange(data.Slice(blockOffset + 0x90 + laneOffset * 2, 2).ToArray());
        padding.AddRange(data.Slice(blockOffset + 0x98 + laneOffset * 2, 2).ToArray());
      }

      var cursor = checked(vertexDataOffset + (int)blockCount * 0xA0);
      context.Ensure(cursor, 8, path + ".ObjectFlags");
      var objectFlags = context.ReadUInt32(cursor);
      var unclassifiedFlags = objectFlags & 0xFFFF0000;
      if (unclassifiedFlags != 0)
      {
        context.AddDiagnosticBounded(
          context.Compatibility(
          path + ".UnclassifiedObjectFlagsHighWord",
          cursor + 2,
          "Unclassified object-flag bits were preserved.",
            new Dictionary<string, string> { ["actual"] = $"0x{unclassifiedFlags:X8}" }
          )
        );
      }

      cursor += 4;
      var textureLengthOffset = cursor;
      var textureLength = context.ReadUInt32(cursor);
      cursor += 4;
      if (textureLength > profile.MaxStaticTexturePathBytes)
      {
        throw context.ResourceLimit(
          path + ".TexturePathBytes",
          textureLengthOffset,
          textureLength,
          profile.MaxStaticTexturePathBytes
        );
      }

      context.Ensure(cursor, (int)textureLength, path + ".TexturePathBytes");
      var texturePathBytes = data.Slice(cursor, (int)textureLength).ToArray();
      cursor += (int)textureLength;
      context.Ensure(cursor, 4, path + ".Triangles");
      var triangleCountOffset = cursor;
      var triangleCount = context.ReadUInt32(cursor);
      cursor += 4;
      if (triangleCount > profile.MaxStaticTrianglesPerObject)
      {
        throw context.ResourceLimit(
          path + ".Triangles",
          triangleCountOffset,
          triangleCount,
          profile.MaxStaticTrianglesPerObject
        );
      }

      context.EnsureCounted(cursor, triangleCount, 8, path + ".Triangles");
      var triangles = new StaticTriangle[(int)triangleCount];
      for (var index = 0; index < triangles.Length; index++)
      {
        var triangleOffset = cursor + index * 8;
        var triangle = new StaticTriangle(
          context.ReadUInt16(triangleOffset),
          context.ReadUInt16(triangleOffset + 2),
          context.ReadUInt16(triangleOffset + 4),
          context.ReadUInt16(triangleOffset + 6)
        );
        if (
          triangle.Vertex0 >= vertexCount
          || triangle.Vertex1 >= vertexCount
          || triangle.Vertex2 >= vertexCount
        )
        {
          throw context.Structural(
            path + $".Triangles[{index}]",
            triangleOffset,
            "A triangle index is outside the active render-vertex range."
          );
        }

        triangles[index] = triangle;
      }

      cursor += checked((int)triangleCount * 8);
      var scaleFrames = ReadVectorTrack(
        context,
        ref cursor,
        path + ".AnimationTracks.ScaleFrames",
        false
      );
      var translationFrames = ReadVectorTrack(
        context,
        ref cursor,
        path + ".AnimationTracks.TranslationFrames",
        true
      );
      var matrices = ReadMatrixTrack(context, ref cursor, path + ".AnimationTracks.Matrices");
      context.Ensure(cursor, 21, path + ".Transform");
      var animationClassValue = context.ReadUInt32(cursor);
      cursor += 4;
      ValidateTrackLengths(
        commonHeader,
        animationClassValue,
        scaleFrames.Count,
        translationFrames.Count,
        matrices.Count,
        path,
        recordOffset,
        context
      );
      var pivot = ReadVector3(context, cursor, invertY: true);
      cursor += 12;
      var barrelMaximumAngle = data[cursor++];
      var nextRecordMarker = context.ReadUInt32(cursor);
      payloadEnd = cursor + sizeof(uint);
      return new DecodedStaticRecord(
        recordOffset,
        vertices,
        triangles,
        blockCount,
        padding,
        objectFlags,
        texturePathBytes,
        new StaticAnimationTracks(scaleFrames, translationFrames, matrices),
        animationClassValue,
        pivot,
        barrelMaximumAngle,
        nextRecordMarker,
        data.Slice(recordOffset, payloadEnd - recordOffset).ToArray()
      );
    }

    private static IReadOnlyList<Vector3> ReadVectorTrack(
      MshDecodeContext context,
      ref int cursor,
      string path,
      bool invertY
    )
    {
      var profile = context.Profile;
      context.Ensure(cursor, 4, path);
      var countOffset = cursor;
      var count = context.ReadUInt32(cursor);
      cursor += 4;
      if (count > profile.MaxStaticAnimationFramesPerTrack)
      {
        throw context.ResourceLimit(path, countOffset, count, profile.MaxStaticAnimationFramesPerTrack);
      }

      context.EnsureCounted(cursor, count, 12, path);
      var result = new Vector3[(int)count];
      for (var index = 0; index < result.Length; index++)
      {
        result[index] = ReadVector3(context, cursor + index * 12, invertY);
      }

      cursor += checked((int)count * 12);
      return result;
    }

    private static IReadOnlyList<Matrix4x4> ReadMatrixTrack(
      MshDecodeContext context,
      ref int cursor,
      string path
    )
    {
      var profile = context.Profile;
      context.Ensure(cursor, 4, path);
      var countOffset = cursor;
      var count = context.ReadUInt32(cursor);
      cursor += 4;
      if (count > profile.MaxStaticAnimationFramesPerTrack)
      {
        throw context.ResourceLimit(path, countOffset, count, profile.MaxStaticAnimationFramesPerTrack);
      }

      context.EnsureCounted(cursor, count, 64, path);
      var result = new Matrix4x4[(int)count];
      for (var index = 0; index < result.Length; index++)
      {
        var offset = cursor + index * 64;
        result[index] = new Matrix4x4(
          context.ReadSingle(offset),
          context.ReadSingle(offset + 4),
          context.ReadSingle(offset + 8),
          context.ReadSingle(offset + 12),
          context.ReadSingle(offset + 16),
          context.ReadSingle(offset + 20),
          context.ReadSingle(offset + 24),
          context.ReadSingle(offset + 28),
          context.ReadSingle(offset + 32),
          context.ReadSingle(offset + 36),
          context.ReadSingle(offset + 40),
          context.ReadSingle(offset + 44),
          context.ReadSingle(offset + 48),
          context.ReadSingle(offset + 52),
          context.ReadSingle(offset + 56),
          context.ReadSingle(offset + 60)
        );
      }

      cursor += checked((int)count * 64);
      return result;
    }

    private static Vector3 ReadVector3(MshDecodeContext context, int offset, bool invertY)
    {
      var y = context.ReadSingle(offset + 4);
      return new Vector3(context.ReadSingle(offset), invertY ? -y : y, context.ReadSingle(offset + 8));
    }

    private static void ValidateTrackLengths(
      CommonMeshBaseHeader commonHeader,
      uint animationClassValue,
      int scaleCount,
      int translationCount,
      int matrixCount,
      string path,
      int recordOffset,
      MshDecodeContext context
    )
    {
      var effectiveAnimationClass = animationClassValue & 3;
      var expected = effectiveAnimationClass switch
      {
        0 => commonHeader.AnimationLengths.A,
        1 => commonHeader.AnimationLengths.B,
        2 => commonHeader.AnimationLengths.C,
        _ => commonHeader.AnimationLengths.D,
      };
      if (animationClassValue > 3)
      {
        context.AddDiagnosticBounded(
          context.Compatibility(
          path + ".AnimationClassValue",
          recordOffset,
          "An unrecognized animation class was preserved.",
          new Dictionary<string, string>
          {
              ["actual"] = animationClassValue.ToString(CultureInfo.InvariantCulture),
            }
          )
        );
      }

      foreach (
        var track in new[]
      {
        (Name: "ScaleFrames", Count: scaleCount),
        (Name: "TranslationFrames", Count: translationCount),
          (Name: "Matrices", Count: matrixCount),
        }
      )
      {
        if (track.Count > 0 && track.Count < expected)
        {
          throw context.Structural(
            path + ".AnimationTracks." + track.Name,
            recordOffset,
            $"A present animation track has {track.Count} frames but class {animationClassValue} declares {expected}."
          );
        }

        if (track.Count > expected)
        {
          context.AddDiagnosticBounded(
            context.Compatibility(
            path + ".AnimationTracks." + track.Name,
            recordOffset,
            "An animation track longer than its selected declaration was preserved.",
            new Dictionary<string, string>
            {
              ["actual"] = track.Count.ToString(CultureInfo.InvariantCulture),
                ["expected"] = expected.ToString(CultureInfo.InvariantCulture),
              }
            )
          );
        }
      }
    }

    private static void ValidateSharingLink(
      MshDecodeContext context,
      ushort link,
      int absoluteVertexIndex,
      string path,
      int localVertexIndex,
      string field,
      int blockOffset
    )
    {
      if (link != ushort.MaxValue && link >= absoluteVertexIndex)
      {
        throw context.Structural(
          $"{path}.RenderVertices[{localVertexIndex}].{field}",
          blockOffset,
          "A shared vertex index must reference an earlier absolute render vertex."
        );
      }
    }

    private static StaticHierarchy ReconstructHierarchy(
      IReadOnlyList<DecodedStaticRecord> records,
      MeshAssetLineageId lineageId,
      int rootSourceObjectLocalId,
      IReadOnlyList<int>? sourceObjectLocalIds,
      MshDecodeContext context
    )
    {
      var profile = context.Profile;
      if (records.Count == 0)
      {
        throw context.Structural(
          "StaticRenderObjectSequence",
          0,
          "At least one static render object is required."
        );
      }

      var sourceIndex = 0;
      var root = new StaticSourceBuilder(
        SourceId(lineageId, rootSourceObjectLocalId, sourceObjectLocalIds, sourceIndex++),
        null,
        0
      );
      var current = root;
      var recordSources = new SourceObjectId[records.Count];
      for (var index = 0; index < records.Count; index++)
      {
        var record = records[index];
        var unwind = (byte)record.ObjectFlags;
        var beginsNested =
          (record.ObjectFlags & (uint)StaticRenderObjectFlags.BeginsNestedSourceObject) != 0;
        if (index == 0 && (unwind != 0 || beginsNested))
        {
          throw context.Structural(
            "StaticRenderObjectSequence[0].ObjectFlags",
            record.RecordOffset,
            "The first static render object must establish the root source object."
          );
        }

        if (unwind > current.Depth)
        {
          throw context.Structural(
            $"StaticRenderObjectSequence[{index}].ObjectFlags",
            record.RecordOffset,
            "The hierarchy unwind exceeds the current source-object depth."
          );
        }

        for (var count = 0; count < unwind; count++)
        {
          current = current.Parent!;
        }

        if (beginsNested)
        {
          var depth = current.Depth + 1;
          if (depth + 1 > profile.MaxStaticHierarchyDepth)
          {
            throw context.ResourceLimit(
              $"StaticRenderObjectSequence[{index}].ObjectFlags",
              record.RecordOffset,
              depth + 1,
              profile.MaxStaticHierarchyDepth
            );
          }

          var child = new StaticSourceBuilder(
            SourceId(lineageId, rootSourceObjectLocalId, sourceObjectLocalIds, sourceIndex++),
            current,
            depth
          );
          current.Children.Add(child);
          current = child;
        }

        current.RecordIndices.Add(index);
        recordSources[index] = current.Id;
      }

      if (sourceObjectLocalIds is not null && sourceObjectLocalIds.Count != sourceIndex)
      {
        throw new ArgumentException(
          "Source-object identities must match the reconstructed hierarchy.",
          nameof(sourceObjectLocalIds)
        );
      }

      return new StaticHierarchy(root, recordSources, current.Depth);
    }

    private static SourceObjectId SourceId(
      MeshAssetLineageId lineageId,
      int firstLocalId,
      IReadOnlyList<int>? sourceObjectLocalIds,
      int index
    )
    {
      return new SourceObjectId(
        lineageId,
        sourceObjectLocalIds is null ? checked(firstLocalId + index)
          : index < sourceObjectLocalIds.Count ? sourceObjectLocalIds[index]
            : throw new ArgumentException(
              "Source-object identities must match the reconstructed hierarchy.",
            nameof(sourceObjectLocalIds)
          )
      );
    }

    private sealed class DecodedStaticRecord
    {
      internal int RecordOffset { get; }
      internal IReadOnlyList<RenderVertex> RenderVertices { get; }
      internal IReadOnlyList<StaticTriangle> Triangles { get; }
      internal uint VertexBlockCount { get; }
      internal IReadOnlyList<byte> VertexBlockPadding { get; }
      internal uint ObjectFlags { get; }
      internal IReadOnlyList<byte> TexturePathBytes { get; }
      internal StaticAnimationTracks AnimationTracks { get; }
      internal uint AnimationClassValue { get; }
      internal Vector3 Pivot { get; }
      internal byte BarrelMaximumAngle { get; }
      internal uint NextRecordMarker { get; }
      internal byte[] SerializedRepresentation { get; }

      internal DecodedStaticRecord(
        int recordOffset,
        IReadOnlyList<RenderVertex> renderVertices,
        IReadOnlyList<StaticTriangle> triangles,
        uint vertexBlockCount,
        IReadOnlyList<byte> vertexBlockPadding,
        uint objectFlags,
        IReadOnlyList<byte> texturePathBytes,
        StaticAnimationTracks animationTracks,
        uint animationClassValue,
        Vector3 pivot,
        byte barrelMaximumAngle,
        uint nextRecordMarker,
        byte[] serializedRepresentation
      )
      {
        RecordOffset = recordOffset;
        RenderVertices = renderVertices;
        Triangles = triangles;
        VertexBlockCount = vertexBlockCount;
        VertexBlockPadding = vertexBlockPadding;
        ObjectFlags = objectFlags;
        TexturePathBytes = texturePathBytes;
        AnimationTracks = animationTracks;
        AnimationClassValue = animationClassValue;
        Pivot = pivot;
        BarrelMaximumAngle = barrelMaximumAngle;
        NextRecordMarker = nextRecordMarker;
        SerializedRepresentation = serializedRepresentation;
      }
    }

    private sealed class StaticSourceBuilder
    {
      internal SourceObjectId Id { get; }
      internal StaticSourceBuilder? Parent { get; }
      internal int Depth { get; }
      internal List<int> RecordIndices { get; } = new List<int>();
      internal List<StaticRenderObjectId> RenderObjectIds { get; } =
        new List<StaticRenderObjectId>();
      internal List<StaticSourceBuilder> Children { get; } = new List<StaticSourceBuilder>();

      internal StaticSourceBuilder(SourceObjectId id, StaticSourceBuilder? parent, int depth)
      {
        Id = id;
        Parent = parent;
        Depth = depth;
      }

      internal StaticSourceObject Build()
      {
        return new StaticSourceObject(Id, RenderObjectIds, Children.Select(child => child.Build()));
      }
    }

    private sealed class StaticHierarchy
    {
      private readonly StaticSourceBuilder _root;

      internal IReadOnlyList<SourceObjectId> RecordSourceIds { get; }
      internal int FinalDepth { get; }

      internal StaticHierarchy(
        StaticSourceBuilder root,
        IReadOnlyList<SourceObjectId> recordSourceIds,
        int finalDepth
      )
      {
        _root = root;
        RecordSourceIds = recordSourceIds;
        FinalDepth = finalDepth;
      }

      internal void AssignRenderObjectIds(IReadOnlyList<StaticRenderObject> renderObjects)
      {
        Assign(_root, renderObjects);
      }

      internal StaticSourceObject BuildRoot()
      {
        return _root.Build();
      }

      private static void Assign(
        StaticSourceBuilder source,
        IReadOnlyList<StaticRenderObject> renderObjects
      )
      {
        source.RenderObjectIds.AddRange(
          source.RecordIndices.Select(index => renderObjects[index].Id)
        );
        foreach (var child in source.Children)
        {
          Assign(child, renderObjects);
        }
      }
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

  }
}
