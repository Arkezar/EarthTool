#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Internal
{
  internal static class MshCanonicalSerializer
  {
    internal const int DynamicRecordSize = 0x410;
    private static readonly Encoding _dynamicStringEncoding = CreateDynamicStringEncoding();

    internal static long GetCanonicalStaticSerializedLength(
      IEnumerable<(int VertexCount, int TriangleCount)> geometry
    )
    {
      return checked(
        sizeof(uint)
        + 16L
        + CommonMeshBaseHeader.SerializedSize
        + CanonicalStaticRenderObjectSequenceEncoder.GetMinimumSerializedLength(geometry)
      );
    }

    internal static byte[] CreateStatic(
      Guid creationGuid,
      IReadOnlyList<byte> commonHeader,
      IReadOnlyList<byte> sequence
    )
    {
      return CreateStatic(
        new MeshArchiveFraming(0x20D0A1FF, null, creationGuid),
        commonHeader,
        sequence
      );
    }

    internal static byte[] CreateDynamic(
      Guid creationGuid,
      CanonicalDynamicObject root,
      int serializedLength
    )
    {
      var framing = new MeshArchiveFraming(0x30D0A1FF, 1, creationGuid);
      var archiveHeader = CreateArchiveHeader(framing);
      var result = new byte[archiveHeader.Length + serializedLength];
      archiveHeader.CopyTo(result, 0);
      var cursor = archiveHeader.Length;
      WriteCanonicalDynamicRecord(result, ref cursor, root);
      return result;
    }

    internal static int GetDynamicSerializedLength(CanonicalDynamicObject root)
    {
      var meshNameLength = EncodeDynamicString(root.Recipe.MeshResourceKey).Length;
      var texturePathLength = EncodeDynamicString(root.Recipe.TextureResourceKey).Length;
      var length = checked(DynamicRecordSize + meshNameLength + texturePathLength);
      foreach (var child in root.Children)
      {
        length = checked(length + GetDynamicSerializedLength(child));
      }

      return length;
    }

    internal static byte[] EncodeDynamicString(string value)
    {
      return _dynamicStringEncoding.GetBytes(value);
    }

    internal static byte[] CreateCanonicalDynamicRecord()
    {
      var record = new byte[DynamicRecordSize];
      CanonicalBaseHeaderEncoder.Dynamic.SerializedRepresentation.CopyTo(record, 0);
      return record;
    }

    private static void WriteCanonicalDynamicRecord(
      byte[] destination,
      ref int cursor,
      CanonicalDynamicObject source
    )
    {
      var recordOffset = cursor;
      var record = CreateCanonicalDynamicRecord();
      var recipe = source.Recipe;
      WriteUInt32(record, 0x368, (uint)recipe.EffectType);
      WriteUInt32(record, 0x36C, (uint)recipe.LightType);
      WriteInt32(record, 0x370, recipe.FirstSourceFrame);
      WriteInt32(record, 0x374, recipe.FrameCount);
      WriteInt32(record, 0x378, recipe.SpriteSheetColumnCount);
      WriteInt32(record, 0x37C, recipe.SpriteSheetRowCount);
      WriteInt32(record, 0x380, recipe.FramePeriodTicks);
      WriteSingle(
        record,
        0x384,
        recipe.SpriteSheetColumnCount == 0 ? 0 : 1f / recipe.SpriteSheetColumnCount
      );
      WriteSingle(
        record,
        0x388,
        recipe.SpriteSheetRowCount == 0 ? 0 : 1f / recipe.SpriteSheetRowCount
      );
      WriteRectangle(record, 0x38C, recipe.StartEffectRectangle);
      WriteRectangle(record, 0x39C, recipe.EndEffectRectangle);
      WriteSingle(record, 0x3AC, recipe.EffectDepthOffset);
      WriteSingle(record, 0x3B0, recipe.RibbonHalfWidth);
      WriteUInt32(record, 0x3B8, recipe.Additive ? 1u : 0u);
      WriteVector3(record, 0x3BC, recipe.TerrainLightColor, invertY: false);
      WriteVector3(record, 0x3C8, recipe.VisibleEffectColor, invertY: false);
      WriteSingle(record, 0x3D4, recipe.VisibleTerrainLightGain);
      WriteInt32(record, 0x3D8, (int)recipe.AlphaTiming);
      WriteSingle(record, 0x3DC, recipe.EndAlpha);
      WriteSingle(record, 0x3E0, recipe.StartAlpha);
      WriteSingle(record, 0x3E4, recipe.EndModelScale);
      WriteSingle(record, 0x3E8, recipe.StartModelScale);
      WriteVector3(record, 0x3EC, recipe.ChildStartTranslation, invertY: true);
      WriteVector3(record, 0x3F8, recipe.ChildEndTranslation, invertY: true);
      record.AsSpan(0, 0x404).CopyTo(destination.AsSpan(recordOffset));
      cursor += 0x404;
      var meshName = EncodeDynamicString(recipe.MeshResourceKey);
      var texturePath = EncodeDynamicString(recipe.TextureResourceKey);
      WriteUInt32(destination, cursor, checked((uint)meshName.Length));
      cursor += sizeof(uint);
      meshName.CopyTo(destination, cursor);
      cursor += meshName.Length;
      WriteUInt32(destination, cursor, checked((uint)texturePath.Length));
      cursor += sizeof(uint);
      texturePath.CopyTo(destination, cursor);
      cursor += texturePath.Length;
      WriteUInt32(destination, cursor, checked((uint)source.Children.Count));
      cursor += sizeof(uint);
      foreach (var child in source.Children)
      {
        WriteCanonicalDynamicRecord(destination, ref cursor, child);
      }
    }

    private static byte[] CreateStatic(
      MeshArchiveFraming framing,
      IReadOnlyList<byte> commonHeader,
      IReadOnlyList<byte> sequence
    )
    {
      var archiveHeader = CreateArchiveHeader(framing);
      var result = new byte[
        archiveHeader.Length
          + CommonMeshBaseHeader.SerializedSize
          + sequence.Count
      ];
      archiveHeader.CopyTo(result, 0);
      commonHeader.CopyTo(result, archiveHeader.Length);
      sequence.CopyTo(result, archiveHeader.Length + CommonMeshBaseHeader.SerializedSize);
      return result;
    }

    private static byte[] CreateArchiveHeader(MeshArchiveFraming framing)
    {
      var length =
        sizeof(uint)
        + (framing.ArchiveType.HasValue ? sizeof(uint) : 0)
        + (framing.CreationGuid.HasValue ? 16 : 0);
      var result = new byte[length];
      WriteUInt32(result, 0, framing.Declaration);
      var cursor = sizeof(uint);
      if (framing.ArchiveType.HasValue)
      {
        WriteUInt32(result, cursor, framing.ArchiveType.Value);
        cursor += sizeof(uint);
      }

      if (framing.CreationGuid.HasValue)
      {
        framing.CreationGuid.Value.ToByteArray().CopyTo(result, cursor);
      }

      return result;
    }

    private static void WriteRectangle(byte[] data, int offset, EffectRectangle rectangle)
    {
      WriteSingle(data, offset, rectangle.X0);
      WriteSingle(data, offset + 4, rectangle.Y1);
      WriteSingle(data, offset + 8, rectangle.X1);
      WriteSingle(data, offset + 12, rectangle.Y0);
    }

    private static void WriteVector3(byte[] data, int offset, Vector3 value, bool invertY)
    {
      WriteSingle(data, offset, value.X);
      WriteSingle(data, offset + 4, invertY && value.Y != 0 ? -value.Y : value.Y);
      WriteSingle(data, offset + 8, value.Z);
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
      BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, sizeof(uint)));
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteInt32(byte[] data, int offset, int value)
    {
      BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), value);
    }

    private static void WriteSingle(byte[] data, int offset, float value)
    {
      WriteUInt32(data, offset, unchecked((uint)BitConverter.SingleToInt32Bits(value)));
    }

    private static Encoding CreateDynamicStringEncoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      return Encoding.GetEncoding(
        28592,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback
      );
    }
  }

  internal static class ReadOnlyListCopyExtensions
  {
    internal static void CopyTo<T>(
      this IReadOnlyList<T> source,
      T[] destination,
      int destinationIndex
    )
    {
      for (var index = 0; index < source.Count; index++)
      {
        destination[destinationIndex + index] = source[index];
      }
    }
  }
}
