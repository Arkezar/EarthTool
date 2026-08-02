using System.Buffers.Binary;
using System.Numerics;

namespace EarthTool.MSH.Tests;

internal static class AttachmentAndCannonMshFixture
{
  internal const int CannonRenderPositionOffset = 0x018;
  internal const int AttachmentTableOffset = 0x1D8;

  internal static byte[] Create(
    IReadOnlyDictionary<int, AttachmentRecord>? attachments = null,
    IReadOnlyDictionary<int, Vector3>? cannonRenderPositions = null,
    uint objectFlags = 0)
  {
    attachments ??= new Dictionary<int, AttachmentRecord>();
    cannonRenderPositions ??= new Dictionary<int, Vector3>();
    var bytes = OneTriangleMshFixture.Create(
      0x20D0A1FF,
      null,
      OneTriangleMshFixture.CreationGuid,
      (data, baseOffset) =>
      {
        foreach (var attachment in attachments)
        {
          WriteAttachment(data, baseOffset, attachment.Key, attachment.Value);
        }
        foreach (var cannon in cannonRenderPositions)
        {
          WriteCannonRenderPosition(data, baseOffset, cannon.Key, cannon.Value);
        }
      });
    var recordOffset = 0x14 + 0x368 + sizeof(uint);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(recordOffset + 0xA8), objectFlags);
    return bytes;
  }

  internal static byte[] GetAttachment(byte[] bytes, int physicalNumber)
  {
    return bytes.AsSpan(0x14 + AttachmentTableOffset + ((physicalNumber - 1) * 8), 8).ToArray();
  }

  internal static byte[] GetCannonRenderPosition(byte[] bytes, int physicalNumber)
  {
    return bytes.AsSpan(0x14 + CannonRenderPositionOffset + ((physicalNumber - 1) * 12), 12).ToArray();
  }

  private static void WriteAttachment(
    byte[] data,
    int baseOffset,
    int physicalNumber,
    AttachmentRecord record)
  {
    var offset = baseOffset + AttachmentTableOffset + ((physicalNumber - 1) * 8);
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), record.X);
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset + 2), record.StoredNegativeY);
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset + 4), record.Z);
    data[offset + 6] = record.Heading;
    data[offset + 7] = record.Extra;
  }

  private static void WriteCannonRenderPosition(
    byte[] data,
    int baseOffset,
    int physicalNumber,
    Vector3 serializedPosition)
  {
    var offset = baseOffset + CannonRenderPositionOffset + ((physicalNumber - 1) * 12);
    WriteSingle(data, offset, serializedPosition.X);
    WriteSingle(data, offset + 4, serializedPosition.Y);
    WriteSingle(data, offset + 8, serializedPosition.Z);
  }

  private static void WriteSingle(byte[] data, int offset, float value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), BitConverter.SingleToInt32Bits(value));
  }

  internal readonly record struct AttachmentRecord(
    short X,
    short StoredNegativeY,
    short Z,
    byte Heading,
    byte Extra);
}
