using System.Buffers.Binary;

namespace EarthTool.MSH.Tests;

internal static class OneTriangleMshFixture
{
  internal static readonly Guid CreationGuid = new("12345678-9abc-def0-1234-56789abcdef0");

  internal static byte[] Create()
  {
    return Create(0x20D0A1FF, null, CreationGuid);
  }

  internal static byte[] Create(
    uint declaration,
    uint? archiveType,
    Guid? creationGuid,
    Action<byte[], int>? configureBaseHeader = null,
    byte[]? rootTrailingBytes = null)
  {
    const int baseHeaderSize = 0x368;
    const int staticRecordSize = 0xDD;
    var archiveHeaderSize = sizeof(uint)
      + (archiveType.HasValue ? sizeof(uint) : 0)
      + (creationGuid.HasValue ? 16 : 0);
    rootTrailingBytes ??= Array.Empty<byte>();
    var data = new byte[
      archiveHeaderSize + baseHeaderSize + sizeof(uint) + staticRecordSize + rootTrailingBytes.Length];

    WriteUInt32(data, 0x00, declaration);
    var archiveCursor = sizeof(uint);
    if (archiveType.HasValue)
    {
      WriteUInt32(data, archiveCursor, archiveType.Value);
      archiveCursor += sizeof(uint);
    }

    if (creationGuid.HasValue)
    {
      creationGuid.Value.ToByteArray().CopyTo(data, archiveCursor);
    }

    var baseOffset = archiveHeaderSize;
    "MESH"u8.CopyTo(data.AsSpan(baseOffset));
    WriteUInt32(data, baseOffset + 0x04, 1);
    WriteUInt32(data, baseOffset + 0x08, 0);

    var attachmentOffset = baseOffset + 0x1D8;
    for (var attachment = 0; attachment < 49; attachment++)
    {
      var offset = attachmentOffset + (attachment * 8);
      WriteInt16(data, offset, short.MinValue);
      WriteInt16(data, offset + 2, short.MinValue);
      WriteInt16(data, offset + 4, short.MinValue);
    }

    configureBaseHeader?.Invoke(data, baseOffset);

    WriteUInt32(data, baseOffset + baseHeaderSize, 1);

    var recordOffset = baseOffset + baseHeaderSize + sizeof(uint);
    WriteUInt32(data, recordOffset, 3);
    WriteUInt32(data, recordOffset + 0x04, 1);

    WriteSingle(data, recordOffset + 0x08 + 0x04, 1);
    WriteSingle(data, recordOffset + 0x08 + 0x10 + 0x08, -1);
    for (var lane = 0; lane < 3; lane++)
    {
      WriteSingle(data, recordOffset + 0x08 + 0x50 + (lane * sizeof(float)), 1);
      WriteSingle(data, recordOffset + 0x08 + 0x70 + (lane * sizeof(float)), 0.5f);
      WriteUInt16(data, recordOffset + 0x08 + 0x90 + (lane * sizeof(ushort)), ushort.MaxValue);
      WriteUInt16(data, recordOffset + 0x08 + 0x98 + (lane * sizeof(ushort)), ushort.MaxValue);
    }

    var cursor = recordOffset + 0x08 + 0xA0;
    WriteUInt32(data, cursor, 0);
    cursor += sizeof(uint);
    WriteUInt32(data, cursor, 0);
    cursor += sizeof(uint);
    WriteUInt32(data, cursor, 1);
    cursor += sizeof(uint);
    WriteUInt16(data, cursor, 0);
    WriteUInt16(data, cursor + 0x02, 1);
    WriteUInt16(data, cursor + 0x04, 2);
    WriteUInt16(data, cursor + 0x06, 1);
    cursor += 0x08;
    WriteUInt32(data, cursor, 0);
    WriteUInt32(data, cursor + 0x04, 0);
    WriteUInt32(data, cursor + 0x08, 0);
    cursor += 0x0C;
    WriteUInt32(data, cursor, 0);
    cursor += sizeof(uint) + 0x0C;
    data[cursor] = 0;
    cursor++;
    WriteUInt32(data, cursor, 0);
    rootTrailingBytes.CopyTo(data, cursor + sizeof(uint));

    return data;
  }

  internal static void WriteDistinctCommonHeaderRegions(byte[] data, int baseOffset)
  {
    WriteUInt32(data, baseOffset + 0x0C, 0xA1B2C3D4);
    WriteUInt32(data, baseOffset + 0x10, 0x01020304);
    WriteUInt32(data, baseOffset + 0x14, 0x11121314);
    Fill(data, baseOffset + 0x18, 0x30, 0x21);
    Fill(data, baseOffset + 0x48, 0xC0, 0x31);
    Fill(data, baseOffset + 0x108, 0x70, 0x41);
    Fill(data, baseOffset + 0x178, 0x20, 0x51);
    Fill(data, baseOffset + 0x198, 0x10, 0x61);
    Fill(data, baseOffset + 0x1A8, 0x10, 0x71);
    Fill(data, baseOffset + 0x1B8, 0x20, 0x81);
    Fill(data, baseOffset + 0x1D8, 0x188, 0x91);
    Fill(data, baseOffset + 0x360, 0x08, 0xA1);
  }

  private static void Fill(byte[] data, int offset, int length, byte seed)
  {
    for (var index = 0; index < length; index++)
    {
      data[offset + index] = unchecked((byte)(seed + index));
    }
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

  private static void WriteSingle(byte[] data, int offset, float value)
  {
    WriteUInt32(data, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
  }
}
