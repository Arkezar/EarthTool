using System.Buffers.Binary;

namespace EarthTool.MSH.Tests;

internal static class OneTriangleMshFixture
{
  internal static readonly Guid CreationGuid = new("12345678-9abc-def0-1234-56789abcdef0");

  internal static byte[] Create()
  {
    const int archiveHeaderSize = 0x14;
    const int baseHeaderSize = 0x368;
    const int staticRecordSize = 0xDD;
    var data = new byte[archiveHeaderSize + baseHeaderSize + sizeof(uint) + staticRecordSize];

    WriteUInt32(data, 0x00, 0x20D0A1FF);
    CreationGuid.ToByteArray().CopyTo(data, 0x04);

    const int baseOffset = archiveHeaderSize;
    "MESH"u8.CopyTo(data.AsSpan(baseOffset));
    WriteUInt32(data, baseOffset + 0x04, 1);
    WriteUInt32(data, baseOffset + 0x08, 0);

    const int attachmentOffset = baseOffset + 0x1D8;
    for (var attachment = 0; attachment < 49; attachment++)
    {
      var offset = attachmentOffset + (attachment * 8);
      WriteInt16(data, offset, short.MinValue);
      WriteInt16(data, offset + 2, short.MinValue);
      WriteInt16(data, offset + 4, short.MinValue);
    }

    WriteUInt32(data, 0x37C, 1);

    const int recordOffset = 0x380;
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

    return data;
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
