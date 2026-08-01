using System.Buffers.Binary;
using System.Numerics;

namespace EarthTool.MSH.Tests;

internal static class StaticAnimationMshFixture
{
  internal static byte[] Create(
    uint animationClassValue,
    AnimationLengths lengths,
    IReadOnlyList<Vector3>? scales = null,
    IReadOnlyList<Vector3>? translations = null,
    IReadOnlyList<Matrix4x4>? matrices = null,
    Vector3? pivot = null,
    AnimationLengths? frameIndices = null)
  {
    const int archiveHeaderSize = 0x14;
    const int baseHeaderSize = 0x368;
    const int fixedRecordSize = 0xDD;
    scales ??= Array.Empty<Vector3>();
    translations ??= Array.Empty<Vector3>();
    matrices ??= Array.Empty<Matrix4x4>();
    var data = new byte[
      archiveHeaderSize
      + baseHeaderSize
      + sizeof(uint)
      + fixedRecordSize
      + (scales.Count * 12)
      + (translations.Count * 12)
      + (matrices.Count * 64)];

    WriteUInt32(data, 0, 0x20D0A1FF);
    OneTriangleMshFixture.CreationGuid.ToByteArray().CopyTo(data, 4);
    "MESH"u8.CopyTo(data.AsSpan(archiveHeaderSize));
    WriteUInt32(data, archiveHeaderSize + 4, 1);
    WriteAnimationBytes(data, archiveHeaderSize + 0x10, lengths);
    WriteAnimationBytes(data, archiveHeaderSize + 0x14, frameIndices ?? default);
    for (var attachment = 0; attachment < 49; attachment++)
    {
      var offset = archiveHeaderSize + 0x1D8 + attachment * 8;
      WriteInt16(data, offset, short.MinValue);
      WriteInt16(data, offset + 2, short.MinValue);
      WriteInt16(data, offset + 4, short.MinValue);
    }

    var recordOffset = archiveHeaderSize + baseHeaderSize + sizeof(uint);
    WriteUInt32(data, archiveHeaderSize + baseHeaderSize, 1);
    WriteUInt32(data, recordOffset, 3);
    WriteUInt32(data, recordOffset + 4, 1);
    WriteSingle(data, recordOffset + 0x08 + 0x04, 1);
    WriteSingle(data, recordOffset + 0x08 + 0x10 + 0x08, -1);
    for (var lane = 0; lane < 3; lane++)
    {
      WriteSingle(data, recordOffset + 0x08 + 0x50 + lane * 4, 1);
      WriteSingle(data, recordOffset + 0x08 + 0x70 + lane * 4, 0.5f);
      WriteUInt16(data, recordOffset + 0x08 + 0x90 + lane * 2, ushort.MaxValue);
      WriteUInt16(data, recordOffset + 0x08 + 0x98 + lane * 2, ushort.MaxValue);
    }

    var cursor = recordOffset + 0x08 + 0xA0;
    WriteUInt32(data, cursor, 0);
    cursor += 4;
    WriteUInt32(data, cursor, 0);
    cursor += 4;
    WriteUInt32(data, cursor, 1);
    cursor += 4;
    WriteUInt16(data, cursor, 0);
    WriteUInt16(data, cursor + 2, 1);
    WriteUInt16(data, cursor + 4, 2);
    WriteUInt16(data, cursor + 6, 1);
    cursor += 8;
    WriteVectorTrack(data, ref cursor, scales, false);
    WriteVectorTrack(data, ref cursor, translations, true);
    WriteUInt32(data, cursor, checked((uint)matrices.Count));
    cursor += 4;
    foreach (var matrix in matrices)
    {
      WriteMatrix(data, cursor, matrix);
      cursor += 64;
    }

    WriteUInt32(data, cursor, animationClassValue);
    cursor += 4;
    WriteVector3(data, cursor, pivot ?? Vector3.Zero, true);
    cursor += 12;
    data[cursor++] = 0;
    WriteUInt32(data, cursor, 0);
    return data;
  }

  private static void WriteAnimationBytes(byte[] data, int offset, AnimationLengths value)
  {
    data[offset] = value.D;
    data[offset + 1] = value.C;
    data[offset + 2] = value.B;
    data[offset + 3] = value.A;
  }

  private static void WriteVectorTrack(
    byte[] data,
    ref int cursor,
    IReadOnlyList<Vector3> values,
    bool invertY)
  {
    WriteUInt32(data, cursor, checked((uint)values.Count));
    cursor += 4;
    foreach (var value in values)
    {
      WriteVector3(data, cursor, value, invertY);
      cursor += 12;
    }
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

  private static void WriteSingle(byte[] data, int offset, float value)
  {
    WriteUInt32(data, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
  }

  private static void WriteUInt32(byte[] data, int offset, uint value)
  {
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
  }

  private static void WriteUInt16(byte[] data, int offset, ushort value)
  {
    BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);
  }

  private static void WriteInt16(byte[] data, int offset, short value)
  {
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), value);
  }

  internal readonly record struct AnimationLengths(byte A, byte B, byte C, byte D);
}
