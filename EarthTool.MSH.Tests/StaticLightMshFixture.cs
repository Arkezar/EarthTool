using System.Buffers.Binary;
using System.Numerics;

namespace EarthTool.MSH.Tests;

internal static class StaticLightMshFixture
{
  internal const int SpotLightOffset = 0x048;
  internal const int OmniLightOffset = 0x108;
  internal const int AttachmentTableOffset = 0x1D8;

  internal static byte[] Create(
    IReadOnlyDictionary<int, SpotRecord>? spots = null,
    IReadOnlyDictionary<int, OmniRecord>? omnis = null,
    IReadOnlyCollection<int>? activeSpots = null,
    IReadOnlyCollection<int>? activeOmnis = null)
  {
    spots ??= new Dictionary<int, SpotRecord>();
    omnis ??= new Dictionary<int, OmniRecord>();
    activeSpots ??= Array.Empty<int>();
    activeOmnis ??= Array.Empty<int>();
    return OneTriangleMshFixture.Create(
      0x20D0A1FF,
      null,
      OneTriangleMshFixture.CreationGuid,
      (data, baseOffset) =>
      {
        foreach (var spot in spots)
        {
          WriteSpot(data, baseOffset, spot.Key, spot.Value);
        }
        foreach (var omni in omnis)
        {
          WriteOmni(data, baseOffset, omni.Key, omni.Value);
        }
        foreach (var number in activeSpots)
        {
          WriteAttachment(data, baseOffset, number + 12, number);
        }
        foreach (var number in activeOmnis)
        {
          WriteAttachment(data, baseOffset, number + 16, number);
        }
      });
  }

  internal static byte[] GetSpot(byte[] bytes, int physicalNumber)
  {
    return bytes.AsSpan(0x14 + SpotLightOffset + ((physicalNumber - 1) * 0x30), 0x30).ToArray();
  }

  internal static byte[] GetOmni(byte[] bytes, int physicalNumber)
  {
    return bytes.AsSpan(0x14 + OmniLightOffset + ((physicalNumber - 1) * 0x1C), 0x1C).ToArray();
  }

  internal static byte[] GetAttachment(byte[] bytes, int physicalNumber)
  {
    return bytes.AsSpan(0x14 + AttachmentTableOffset + ((physicalNumber - 1) * 8), 8).ToArray();
  }

  private static void WriteSpot(
    byte[] data,
    int baseOffset,
    int physicalNumber,
    SpotRecord record)
  {
    var offset = baseOffset + SpotLightOffset + ((physicalNumber - 1) * 0x30);
    WriteVector(data, offset, record.Position);
    WriteVector(data, offset + 0x0C, record.Color);
    WriteSingle(data, offset + 0x18, record.ApproximateTargetDistance);
    data[offset + 0x1C] = record.Heading;
    record.Reserved.CopyTo(data, offset + 0x1D);
    WriteSingle(data, offset + 0x20, record.ConeHalfAngleTangent);
    WriteSingle(data, offset + 0x24, record.HalfFalloffAngleDistanceProduct);
    WriteSingle(data, offset + 0x28, record.VerticalTargetSlope);
    WriteSingle(data, offset + 0x2C, record.TerrainLightAmplitude);
  }

  private static void WriteOmni(
    byte[] data,
    int baseOffset,
    int physicalNumber,
    OmniRecord record)
  {
    var offset = baseOffset + OmniLightOffset + ((physicalNumber - 1) * 0x1C);
    WriteVector(data, offset, record.Position);
    WriteVector(data, offset + 0x0C, record.Color);
    WriteSingle(data, offset + 0x18, record.TerrainLightAmplitude);
  }

  private static void WriteAttachment(byte[] data, int baseOffset, int physicalNumber, int seed)
  {
    var offset = baseOffset + AttachmentTableOffset + ((physicalNumber - 1) * 8);
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), checked((short)(seed * 17)));
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset + 2), checked((short)(seed * -19)));
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset + 4), checked((short)(seed * 23)));
    data[offset + 6] = unchecked((byte)(seed * 29));
    data[offset + 7] = unchecked((byte)(seed * 31));
  }

  private static void WriteVector(byte[] data, int offset, Vector3 value)
  {
    WriteSingle(data, offset, value.X);
    WriteSingle(data, offset + 4, value.Y);
    WriteSingle(data, offset + 8, value.Z);
  }

  private static void WriteSingle(byte[] data, int offset, float value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), BitConverter.SingleToInt32Bits(value));
  }

  internal readonly record struct SpotRecord(
    Vector3 Position,
    Vector3 Color,
    float ApproximateTargetDistance,
    byte Heading,
    byte[] Reserved,
    float ConeHalfAngleTangent,
    float HalfFalloffAngleDistanceProduct,
    float VerticalTargetSlope,
    float TerrainLightAmplitude);

  internal readonly record struct OmniRecord(
    Vector3 Position,
    Vector3 Color,
    float TerrainLightAmplitude);
}
