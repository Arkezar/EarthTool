#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;

namespace EarthTool.MSH.Internal
{
  internal readonly struct CanonicalAttachmentRecord
  {
    internal Vector3 Position { get; }

    internal byte Heading { get; }

    internal byte Extra { get; }

    internal CanonicalAttachmentRecord(Vector3 position, byte heading, byte extra)
    {
      Position = position;
      Heading = heading;
      Extra = extra;
    }
  }

  internal readonly struct CanonicalCannonRenderPosition
  {
    internal Vector3 Position { get; }

    internal CanonicalCannonRenderPosition(Vector3 position)
    {
      Position = position;
    }
  }

  internal readonly struct CanonicalSpotLight
  {
    internal Vector3 Position { get; }

    internal Vector3 Color { get; }

    internal float ApproximateTargetDistance { get; }

    internal byte Heading { get; }

    internal float ConeHalfAngleTangent { get; }

    internal float HalfFalloffAngleDistanceProduct { get; }

    internal float VerticalTargetSlope { get; }

    internal float TerrainLightAmplitude { get; }

    internal CanonicalSpotLight(
      Vector3 position,
      Vector3 color,
      float approximateTargetDistance,
      byte heading,
      float coneHalfAngleTangent,
      float halfFalloffAngleDistanceProduct,
      float verticalTargetSlope,
      float terrainLightAmplitude
    )
    {
      Position = position;
      Color = color;
      ApproximateTargetDistance = approximateTargetDistance;
      Heading = heading;
      ConeHalfAngleTangent = coneHalfAngleTangent;
      HalfFalloffAngleDistanceProduct = halfFalloffAngleDistanceProduct;
      VerticalTargetSlope = verticalTargetSlope;
      TerrainLightAmplitude = terrainLightAmplitude;
    }
  }

  internal readonly struct CanonicalOmniLight
  {
    internal Vector3 Position { get; }

    internal Vector3 Color { get; }

    internal float TerrainLightAmplitude { get; }

    internal CanonicalOmniLight(
      Vector3 position,
      Vector3 color,
      float terrainLightAmplitude
    )
    {
      Position = position;
      Color = color;
      TerrainLightAmplitude = terrainLightAmplitude;
    }
  }

  internal sealed class CanonicalStaticBaseHeaderInput
  {
    internal AnimationClassBytes AnimationLengths { get; }

    internal AnimationClassBytes AnimationFrameIndices { get; }

    internal IReadOnlyList<CanonicalStaticVertex> Vertices { get; }

    internal CanonicalStaticFootprint? Footprint { get; }

    internal CanonicalHorizontalExtents? HorizontalExtents { get; }

    internal IReadOnlyDictionary<int, CanonicalAttachmentRecord> AttachmentRecords { get; }

    internal IReadOnlyDictionary<int, CanonicalCannonRenderPosition> CannonRenderPositions { get; }

    internal IReadOnlyDictionary<int, CanonicalSpotLight> StaticSpotLights { get; }

    internal IReadOnlyDictionary<int, CanonicalOmniLight> StaticOmniLights { get; }

    internal CanonicalStaticBaseHeaderInput(
      AnimationClassBytes animationLengths,
      IEnumerable<CanonicalStaticVertex> vertices,
      CanonicalStaticFootprint? footprint = null,
      CanonicalHorizontalExtents? horizontalExtents = null,
      AnimationClassBytes? animationFrameIndices = null,
      IReadOnlyDictionary<int, CanonicalAttachmentRecord>? attachmentRecords = null,
      IReadOnlyDictionary<int, CanonicalCannonRenderPosition>? cannonRenderPositions = null,
      IReadOnlyDictionary<int, CanonicalSpotLight>? staticSpotLights = null,
      IReadOnlyDictionary<int, CanonicalOmniLight>? staticOmniLights = null
    )
    {
      AnimationLengths = animationLengths;
      AnimationFrameIndices = animationFrameIndices ?? default;
      Vertices = Array.AsReadOnly(
        (vertices ?? throw new ArgumentNullException(nameof(vertices))).ToArray()
      );
      Footprint = footprint;
      HorizontalExtents = horizontalExtents;
      AttachmentRecords = CopyRecords(attachmentRecords, 49, nameof(attachmentRecords));
      CannonRenderPositions = CopyRecords(
        cannonRenderPositions,
        4,
        nameof(cannonRenderPositions)
      );
      StaticSpotLights = CopyRecords(staticSpotLights, 4, nameof(staticSpotLights));
      StaticOmniLights = CopyRecords(staticOmniLights, 4, nameof(staticOmniLights));
    }

    private static IReadOnlyDictionary<int, T> CopyRecords<T>(
      IReadOnlyDictionary<int, T>? records,
      int count,
      string parameterName
    )
      where T : struct
    {
      var result = new Dictionary<int, T>();
      if (records is null)
      {
        return new ReadOnlyDictionary<int, T>(result);
      }

      foreach (var record in records)
      {
        if (record.Key < 1 || record.Key > count)
        {
          throw new ArgumentOutOfRangeException(parameterName);
        }
        result.Add(record.Key, record.Value);
      }

      return new ReadOnlyDictionary<int, T>(result);
    }
  }

  internal static class CanonicalBaseHeaderEncoder
  {
    private const int AnimationLengthsOffset = 0x10;
    private const int AnimationFrameIndicesOffset = 0x14;
    private const int CannonRenderPositionsOffset = 0x018;
    private const int StaticSpotLightsOffset = 0x048;
    private const int StaticOmniLightsOffset = 0x108;
    private const int AttachmentTableOffset = 0x1D8;
    private const int HorizontalExtentsOffset = 0x360;
    private const int AttachmentCount = 49;
    private const int AttachmentRecordSize = 8;
    private const short AbsentAttachmentCoordinate = short.MinValue;
    private static readonly byte[] _dynamicBytes = CreateBase(MeshAssetKind.Dynamic, default);

    internal static CommonMeshBaseHeader Dynamic { get; } = new(_dynamicBytes);

    internal static CommonMeshBaseHeader EncodeStatic(CanonicalStaticBaseHeaderInput input)
    {
      if (input is null)
      {
        throw new ArgumentNullException(nameof(input));
      }

      var header = CreateBase(MeshAssetKind.Static, input.AnimationLengths);
      WriteAnimationClassBytes(header, AnimationFrameIndicesOffset, input.AnimationFrameIndices);
      WriteFootprint(header, ResolveFootprint(input));
      WriteHorizontalExtents(header, ResolveHorizontalExtents(input));
      WriteAttachmentRecords(header, input.AttachmentRecords);
      WriteCannonRenderPositions(header, input.CannonRenderPositions);
      WriteStaticSpotLights(header, input.StaticSpotLights);
      WriteStaticOmniLights(header, input.StaticOmniLights);
      return new CommonMeshBaseHeader(header);
    }

    internal static CommonMeshBaseHeader RewriteStatic(
      CommonMeshBaseHeader source,
      AnimationClassBytes? animationLengths,
      AnimationClassBytes? animationFrameIndices,
      IReadOnlyDictionary<int, byte[]> attachmentRecords,
      IReadOnlyDictionary<int, byte[]> cannonRenderPositions,
      IReadOnlyDictionary<int, byte[]> staticSpotLights,
      IReadOnlyDictionary<int, byte[]> staticOmniLights,
      CanonicalHorizontalExtents? horizontalExtents
    )
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      var header = source.SerializedRepresentation.ToArray();
      if (animationLengths.HasValue)
      {
        WriteAnimationClassBytes(header, AnimationLengthsOffset, animationLengths.Value);
      }
      if (animationFrameIndices.HasValue)
      {
        WriteAnimationClassBytes(header, AnimationFrameIndicesOffset, animationFrameIndices.Value);
      }
      WriteExactRecords(header, AttachmentTableOffset, AttachmentRecordSize, attachmentRecords);
      WriteExactRecords(header, CannonRenderPositionsOffset, 12, cannonRenderPositions);
      WriteExactRecords(header, StaticSpotLightsOffset, 0x30, staticSpotLights);
      WriteExactRecords(header, StaticOmniLightsOffset, 0x1C, staticOmniLights);
      if (horizontalExtents is not null)
      {
        WriteHorizontalExtents(header, horizontalExtents);
      }

      return new CommonMeshBaseHeader(header);
    }

    internal static bool IsCanonicalDynamic(ReadOnlySpan<byte> serializedRepresentation)
    {
      return serializedRepresentation.SequenceEqual(_dynamicBytes);
    }

    internal static (
      bool OccupancyDescriptors,
      bool CornerPassageMaps
    ) GetCanonicalRotatedFootprintMatches(CommonMeshBaseHeader header)
    {
      var cornerFlags = Enumerable
        .Range(0, 16)
        .Select(index => (byte)(header.BoxCornerPassageFlags[15 - index] & 0x0F))
        .ToArray();

      var footprint = new CanonicalStaticFootprint(
        (ushort)header.BoxPresenceMask,
        new float[16],
        cornerFlags
      );
      var canonical = EncodeStatic(
        new CanonicalStaticBaseHeaderInput(
          default,
          Array.Empty<CanonicalStaticVertex>(),
          footprint,
          new CanonicalHorizontalExtents(0, 0, 0, 0)
        )
      );
      return (
        header.RotatedOccupancyDescriptors.SequenceEqual(canonical.RotatedOccupancyDescriptors),
        header.RotatedCornerPassageMaps.SequenceEqual(canonical.RotatedCornerPassageMaps)
      );
    }

    internal static byte[] EncodeAttachmentRecord(CanonicalAttachmentRecord record)
    {
      var result = new byte[AttachmentRecordSize];
      WriteInt16(result, 0, ToSignedFixedPoint(record.Position.X, true));
      WriteInt16(result, 2, ToSignedFixedPoint(record.Position.Y, false));
      WriteInt16(result, 4, ToSignedFixedPoint(record.Position.Z, false));
      result[6] = record.Heading;
      result[7] = record.Extra;
      return result;
    }

    internal static byte[] EncodeCannonRenderPosition(CanonicalCannonRenderPosition record)
    {
      var result = new byte[12];
      WriteVector3(result, 0, record.Position);
      return result;
    }

    internal static byte[] EncodeStaticSpotLight(CanonicalSpotLight record)
    {
      var result = new byte[0x30];
      WriteVector3(result, 0, record.Position);
      WriteVector3(result, 0x0C, record.Color);
      WriteSingle(result, 0x18, record.ApproximateTargetDistance);
      result[0x1C] = record.Heading;
      WriteSingle(result, 0x20, record.ConeHalfAngleTangent);
      WriteSingle(result, 0x24, record.HalfFalloffAngleDistanceProduct);
      WriteSingle(result, 0x28, record.VerticalTargetSlope);
      WriteSingle(result, 0x2C, record.TerrainLightAmplitude);
      return result;
    }

    internal static byte[] EncodeStaticOmniLight(CanonicalOmniLight record)
    {
      var result = new byte[0x1C];
      WriteVector3(result, 0, record.Position);
      WriteVector3(result, 0x0C, record.Color);
      WriteSingle(result, 0x18, record.TerrainLightAmplitude);
      return result;
    }

    internal static byte[] CreateAbsentAttachmentRecord()
    {
      var result = new byte[AttachmentRecordSize];
      WriteInt16(result, 0, AbsentAttachmentCoordinate);
      WriteInt16(result, 2, AbsentAttachmentCoordinate);
      WriteInt16(result, 4, AbsentAttachmentCoordinate);
      return result;
    }

    private static byte[] CreateBase(
      MeshAssetKind meshKind,
      AnimationClassBytes animationLengths
    )
    {
      var header = new byte[CommonMeshBaseHeader.SerializedSize];
      header[0] = (byte)'M';
      header[1] = (byte)'E';
      header[2] = (byte)'S';
      header[3] = (byte)'H';
      BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0x04), 1);
      BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0x08), (uint)meshKind);
      WriteAnimationClassBytes(header, AnimationLengthsOffset, animationLengths);

      var absentAttachment = CreateAbsentAttachmentRecord();
      for (var attachment = 0; attachment < AttachmentCount; attachment++)
      {
        var offset = AttachmentTableOffset + (attachment * AttachmentRecordSize);
        absentAttachment.CopyTo(header, offset);
      }

      return header;
    }

    private static CanonicalStaticFootprint ResolveFootprint(
      CanonicalStaticBaseHeaderInput input
    )
    {
      return input.Footprint
        ?? new CanonicalStaticFootprint(
          0x8000,
          Enumerable
            .Range(0, 16)
            .Select(index => index == 15 ? input.Vertices.Max(vertex => vertex.Position.Z) : 0),
          new byte[16]
        );
    }

    private static CanonicalHorizontalExtents ResolveHorizontalExtents(
      CanonicalStaticBaseHeaderInput input
    )
    {
      return input.HorizontalExtents
        ?? new CanonicalHorizontalExtents(
          Math.Max(0, input.Vertices.Max(vertex => vertex.Position.Y)),
          -Math.Min(0, input.Vertices.Min(vertex => vertex.Position.Y)),
          Math.Max(0, input.Vertices.Max(vertex => vertex.Position.X)),
          -Math.Min(0, input.Vertices.Min(vertex => vertex.Position.X))
        );
    }

    private static void WriteFootprint(byte[] header, CanonicalStaticFootprint footprint)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0x0C), footprint.PresenceMask);
      for (var logicalIndex = 0; logicalIndex < 16; logicalIndex++)
      {
        BinaryPrimitives.WriteUInt16LittleEndian(
          header.AsSpan(0x196 - (logicalIndex * sizeof(ushort))),
          ToUnsignedFixedPoint(footprint.TopElevations[logicalIndex])
        );
        header[0x1A7 - logicalIndex] = footprint.CornerPassageFlags[logicalIndex];
      }
      WriteRotatedFootprint(header, footprint);
    }

    private static void WriteRotatedFootprint(
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
              (footprint.CornerPassageFlags[logicalIndex] & (1 << flagMaps[quarterTurn][bit]))
              != 0
            )
            {
              rotatedNibble |= checked((byte)(1 << bit));
            }
          }
          var shift = rotatedLogicalIndex * 4;
          rotatedFlags =
            (rotatedFlags & ~(0xFul << shift)) | ((ulong)rotatedNibble << shift);
        }

        uint descriptor = rotatedMask;
        if (occupiedPhysicalSlots.Count != 0)
        {
          var minimumRow = occupiedPhysicalSlots.Min(slot => slot / 4);
          var maximumRow = occupiedPhysicalSlots.Max(slot => slot / 4);
          var minimumColumn = occupiedPhysicalSlots.Min(slot => slot % 4);
          var maximumColumn = occupiedPhysicalSlots.Max(slot => slot % 4);
          var biasA = minimumRow + (int)Math.Truncate((maximumColumn + 1 - minimumRow) / 2d);
          var biasB =
            minimumColumn + (int)Math.Truncate((maximumRow + 1 - minimumColumn) / 2d);
          descriptor |= (uint)anchors[quarterTurn].X << 30;
          descriptor |= (uint)anchors[quarterTurn].Y << 28;
          descriptor |= (uint)biasA << 26;
          descriptor |= (uint)biasB << 24;
        }
        BinaryPrimitives.WriteUInt32LittleEndian(
          header.AsSpan(0x1A8 + (quarterTurn * sizeof(uint))),
          descriptor
        );
        BinaryPrimitives.WriteUInt64LittleEndian(
          header.AsSpan(0x1B8 + (quarterTurn * sizeof(ulong))),
          rotatedFlags
        );
      }
    }

    private static void WriteHorizontalExtents(
      byte[] header,
      CanonicalHorizontalExtents horizontalExtents
    )
    {
      BinaryPrimitives.WriteUInt16LittleEndian(
        header.AsSpan(HorizontalExtentsOffset),
        ToUnsignedFixedPoint(horizontalExtents.PositiveY)
      );
      BinaryPrimitives.WriteUInt16LittleEndian(
        header.AsSpan(HorizontalExtentsOffset + 2),
        ToUnsignedFixedPoint(horizontalExtents.NegativeY)
      );
      BinaryPrimitives.WriteUInt16LittleEndian(
        header.AsSpan(HorizontalExtentsOffset + 4),
        ToUnsignedFixedPoint(horizontalExtents.PositiveX)
      );
      BinaryPrimitives.WriteUInt16LittleEndian(
        header.AsSpan(HorizontalExtentsOffset + 6),
        ToUnsignedFixedPoint(horizontalExtents.NegativeX)
      );
    }

    private static void WriteAttachmentRecords(
      byte[] header,
      IReadOnlyDictionary<int, CanonicalAttachmentRecord> records
    )
    {
      foreach (var record in records)
      {
        var offset = AttachmentTableOffset + ((record.Key - 1) * AttachmentRecordSize);
        EncodeAttachmentRecord(record.Value).CopyTo(header, offset);
      }
    }

    private static void WriteCannonRenderPositions(
      byte[] header,
      IReadOnlyDictionary<int, CanonicalCannonRenderPosition> records
    )
    {
      foreach (var record in records)
      {
        EncodeCannonRenderPosition(record.Value)
          .CopyTo(header, CannonRenderPositionsOffset + ((record.Key - 1) * 12));
      }
    }

    private static void WriteStaticSpotLights(
      byte[] header,
      IReadOnlyDictionary<int, CanonicalSpotLight> records
    )
    {
      foreach (var record in records)
      {
        var offset = StaticSpotLightsOffset + ((record.Key - 1) * 0x30);
        EncodeStaticSpotLight(record.Value).CopyTo(header, offset);
      }
    }

    private static void WriteStaticOmniLights(
      byte[] header,
      IReadOnlyDictionary<int, CanonicalOmniLight> records
    )
    {
      foreach (var record in records)
      {
        var offset = StaticOmniLightsOffset + ((record.Key - 1) * 0x1C);
        EncodeStaticOmniLight(record.Value).CopyTo(header, offset);
      }
    }

    private static void WriteExactRecords(
      byte[] header,
      int regionOffset,
      int recordSize,
      IReadOnlyDictionary<int, byte[]> records
    )
    {
      foreach (var record in records)
      {
        record.Value.CopyTo(header, regionOffset + ((record.Key - 1) * recordSize));
      }
    }

    private static void WriteVector3(byte[] header, int offset, Vector3 value)
    {
      WriteSingle(header, offset, value.X);
      WriteSingle(header, offset + 4, value.Y);
      WriteSingle(header, offset + 8, value.Z);
    }

    private static void WriteSingle(byte[] header, int offset, float value)
    {
      BinaryPrimitives.WriteInt32LittleEndian(
        header.AsSpan(offset),
        BitConverter.SingleToInt32Bits(value == 0 ? 0 : value)
      );
    }

    private static void WriteInt16(byte[] header, int offset, short value)
    {
      BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(offset), value);
    }

    private static void WriteAnimationClassBytes(
      Span<byte> header,
      int offset,
      AnimationClassBytes value
    )
    {
      header[offset] = value.D;
      header[offset + 1] = value.C;
      header[offset + 2] = value.B;
      header[offset + 3] = value.A;
    }

    private static ushort ToUnsignedFixedPoint(float value)
    {
      return checked((ushort)Math.Truncate(value * 256d));
    }

    private static short ToSignedFixedPoint(float value, bool rejectAbsentSentinel)
    {
      var scaled = Math.Truncate(value * 256d);
      if (!double.IsFinite(scaled) || scaled < short.MinValue || scaled > short.MaxValue)
      {
        throw new OverflowException();
      }

      var result = (short)scaled;
      if (rejectAbsentSentinel && result == AbsentAttachmentCoordinate)
      {
        throw new OverflowException();
      }
      return result;
    }
  }
}
