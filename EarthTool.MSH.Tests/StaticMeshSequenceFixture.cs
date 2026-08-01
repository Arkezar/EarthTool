using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Tests;

internal sealed class StaticMeshSequenceFixture
{
  private const int ArchiveHeaderSize = 0x14;
  private const int BaseHeaderSize = 0x368;

  internal byte[] Data { get; }
  internal IReadOnlyList<int> RecordOffsets { get; }

  private StaticMeshSequenceFixture(byte[] data, IReadOnlyList<int> recordOffsets)
  {
    Data = data;
    RecordOffsets = recordOffsets;
  }

  internal static StaticMeshSequenceFixture CreateInterleaved()
  {
    var records = new[]
    {
      new Record(0x00000100, "Textures\\root-a.tex", Vector3.Zero, 0, 0xDEADBEEF, false, false),
      new Record(0x00000A00, "Textures\\barrel.tex", new Vector3(1, 2, 3), 64, 2, true, true),
      new Record(0x00001001, "Textures\\root-b.tex", new Vector3(4, 5, 6), 0, 3, false, false),
      new Record(0x00000C00, "Textures\\rotor.tex", new Vector3(7, 8, 9), 0, 0, false, false)
    };
    return Create(records, storedTrailingUnwind: 2);
  }

  internal static StaticMeshSequenceFixture CreateSingle()
  {
    return Create(
      new[] { new Record(0, string.Empty, Vector3.Zero, 0, 0, false, false) },
      storedTrailingUnwind: 1);
  }

  private static StaticMeshSequenceFixture Create(
    IReadOnlyList<Record> records,
    uint storedTrailingUnwind)
  {
    var recordBytes = records.Select(WriteRecord).ToArray();
    var length = ArchiveHeaderSize + BaseHeaderSize + sizeof(uint) + recordBytes.Sum(bytes => bytes.Length);
    var data = new byte[length];
    WriteUInt32(data, 0, 0x20D0A1FF);
    new Guid("12345678-9abc-def0-1234-56789abcdef0").ToByteArray().CopyTo(data, 4);
    "MESH"u8.CopyTo(data.AsSpan(ArchiveHeaderSize));
    WriteUInt32(data, ArchiveHeaderSize + 4, 1);
    WriteUInt32(data, ArchiveHeaderSize + 8, 0);
    data[ArchiveHeaderSize + 0x13] = 1;
    for (var attachment = 0; attachment < 49; attachment++)
    {
      var offset = ArchiveHeaderSize + 0x1D8 + attachment * 8;
      WriteInt16(data, offset, short.MinValue);
      WriteInt16(data, offset + 2, short.MinValue);
      WriteInt16(data, offset + 4, short.MinValue);
    }

    var cursor = ArchiveHeaderSize + BaseHeaderSize;
    WriteUInt32(data, cursor, storedTrailingUnwind);
    cursor += sizeof(uint);
    var offsets = new List<int>();
    foreach (var bytes in recordBytes)
    {
      offsets.Add(cursor);
      bytes.CopyTo(data, cursor);
      cursor += bytes.Length;
    }

    return new StaticMeshSequenceFixture(data, offsets.AsReadOnly());
  }

  private static byte[] WriteRecord(Record record)
  {
    var texture = Encoding.ASCII.GetBytes(record.TexturePath);
    var trackSize = record.WithTracks ? 12 + 12 + 64 : 0;
    var data = new byte[8 + 0xA0 + 4 + 4 + texture.Length + 4 + 8 + 12 + trackSize + 4 + 12 + 1 + 4];
    WriteUInt32(data, 0, 3);
    WriteUInt32(data, 4, 1);
    for (var lane = 0; lane < 3; lane++)
    {
      WriteSingle(data, 8 + lane * 4, lane);
      WriteSingle(data, 8 + 0x10 + lane * 4, -lane);
      WriteSingle(data, 8 + 0x20 + lane * 4, lane + 1);
      WriteSingle(data, 8 + 0x50 + lane * 4, 1);
      WriteSingle(data, 8 + 0x60 + lane * 4, lane / 2f);
      WriteSingle(data, 8 + 0x70 + lane * 4, 1 - lane / 2f);
      WriteSingle(data, 8 + 0x80 + lane * 4, lane + 0.25f);
      WriteUInt16(data, 8 + 0x90 + lane * 2,
        lane == 0 && record.NormalSharesFirstVertex ? (ushort)0 : ushort.MaxValue);
      WriteUInt16(data, 8 + 0x98 + lane * 2, ushort.MaxValue);
    }

    data[8 + 0x0C] = 0x5A;
    var cursor = 8 + 0xA0;
    WriteUInt32(data, cursor, record.ObjectFlags);
    cursor += 4;
    WriteUInt32(data, cursor, (uint)texture.Length);
    cursor += 4;
    texture.CopyTo(data, cursor);
    cursor += texture.Length;
    WriteUInt32(data, cursor, 1);
    cursor += 4;
    WriteUInt16(data, cursor, 0);
    WriteUInt16(data, cursor + 2, 1);
    WriteUInt16(data, cursor + 4, 2);
    WriteUInt16(data, cursor + 6, 3);
    cursor += 8;
    WriteUInt32(data, cursor, record.WithTracks ? 1u : 0);
    cursor += 4;
    if (record.WithTracks)
    {
      WriteVector3(data, cursor, Vector3.One);
      cursor += 12;
    }

    WriteUInt32(data, cursor, record.WithTracks ? 1u : 0);
    cursor += 4;
    if (record.WithTracks)
    {
      WriteVector3(data, cursor, new Vector3(10, -20, 30));
      cursor += 12;
    }

    WriteUInt32(data, cursor, record.WithTracks ? 1u : 0);
    cursor += 4;
    if (record.WithTracks)
    {
      WriteMatrix(data, cursor, Matrix4x4.Identity);
      cursor += 64;
    }

    WriteUInt32(data, cursor, 0);
    cursor += 4;
    WriteVector3(data, cursor, new Vector3(record.Pivot.X, -record.Pivot.Y, record.Pivot.Z));
    cursor += 12;
    data[cursor++] = record.BarrelMaximumAngle;
    WriteUInt32(data, cursor, record.NextRecordMarker);
    return data;
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

  private static void WriteVector3(byte[] data, int offset, Vector3 value)
  {
    WriteSingle(data, offset, value.X);
    WriteSingle(data, offset + 4, value.Y);
    WriteSingle(data, offset + 8, value.Z);
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

  private sealed record Record(
    uint ObjectFlags,
    string TexturePath,
    Vector3 Pivot,
    byte BarrelMaximumAngle,
    uint NextRecordMarker,
    bool WithTracks,
    bool NormalSharesFirstVertex);
}
