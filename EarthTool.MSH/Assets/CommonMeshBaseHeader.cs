#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace EarthTool.MSH.Assets
{
  /// <summary>Provides exact fixed-region ownership and named semantic views for a MESH base header.</summary>
  public sealed class CommonMeshBaseHeader
  {
    internal const int SerializedSize = 0x368;

    private readonly byte[] _serializedRepresentation;

    internal static CommonMeshBaseHeader CanonicalDynamic { get; } =
      CreateCanonical(MeshAssetKind.Dynamic, new AnimationClassBytes());

    /// <summary>Gets the MSH format version.</summary>
    public uint Version { get; }

    /// <summary>Gets the exact root payload discriminator.</summary>
    public uint MeshKind { get; }

    /// <summary>Gets the exact box-presence mask.</summary>
    public uint BoxPresenceMask { get; }

    /// <summary>Gets animation lengths for classes A through D.</summary>
    public AnimationClassBytes AnimationLengths { get; }

    /// <summary>Gets current frame indices for animation classes A through D.</summary>
    public AnimationClassBytes AnimationFrameIndices { get; }

    /// <summary>Gets the exact four cannon render-position records.</summary>
    public IReadOnlyList<byte> CannonRenderPositions { get; }

    /// <summary>Gets the exact four static spot-light records.</summary>
    public IReadOnlyList<byte> StaticSpotLights { get; }

    /// <summary>Gets the exact four static omni-light records.</summary>
    public IReadOnlyList<byte> StaticOmniLights { get; }

    /// <summary>Gets the exact reverse-packed box-top elevations.</summary>
    public IReadOnlyList<byte> BoxTopElevations { get; }

    /// <summary>Gets the exact reverse-packed box corner-passage flags.</summary>
    public IReadOnlyList<byte> BoxCornerPassageFlags { get; }

    /// <summary>Gets the exact four rotated occupancy descriptors.</summary>
    public IReadOnlyList<byte> RotatedOccupancyDescriptors { get; }

    /// <summary>Gets the exact four rotated corner-passage maps.</summary>
    public IReadOnlyList<byte> RotatedCornerPassageMaps { get; }

    /// <summary>Gets all 49 exact physical attachment records.</summary>
    public IReadOnlyList<byte> AttachmentTable { get; }

    /// <summary>Gets the exact +Y, -Y, +X, and -X extent words.</summary>
    public IReadOnlyList<byte> HorizontalExtents { get; }

    /// <summary>Gets the complete exact 0x368-byte serialized representation.</summary>
    public IReadOnlyList<byte> SerializedRepresentation { get; }

    internal bool IsCanonicalDynamic =>
      _serializedRepresentation.AsSpan().SequenceEqual(CanonicalDynamic._serializedRepresentation);

    internal CommonMeshBaseHeader(byte[] serializedRepresentation)
    {
      if (serializedRepresentation.Length != SerializedSize)
      {
        throw new ArgumentException(
          "A common MESH base header must contain exactly 0x368 bytes.",
          nameof(serializedRepresentation)
        );
      }

      _serializedRepresentation = (byte[])serializedRepresentation.Clone();
      var data = _serializedRepresentation.AsSpan();
      Version = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0x04, 4));
      MeshKind = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0x08, 4));
      BoxPresenceMask = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0x0C, 4));
      AnimationLengths = ReadAnimationClassBytes(data.Slice(0x10, 4));
      AnimationFrameIndices = ReadAnimationClassBytes(data.Slice(0x14, 4));
      CannonRenderPositions = Copy(data, 0x018, 0x030);
      StaticSpotLights = Copy(data, 0x048, 0x0C0);
      StaticOmniLights = Copy(data, 0x108, 0x070);
      BoxTopElevations = Copy(data, 0x178, 0x020);
      BoxCornerPassageFlags = Copy(data, 0x198, 0x010);
      RotatedOccupancyDescriptors = Copy(data, 0x1A8, 0x010);
      RotatedCornerPassageMaps = Copy(data, 0x1B8, 0x020);
      AttachmentTable = Copy(data, 0x1D8, 0x188);
      HorizontalExtents = Copy(data, 0x360, 0x008);
      SerializedRepresentation = Array.AsReadOnly(_serializedRepresentation);
    }

    internal static CommonMeshBaseHeader CreateCanonicalStatic(AnimationClassBytes animationLengths)
    {
      return CreateCanonical(MeshAssetKind.Static, animationLengths);
    }

    private static CommonMeshBaseHeader CreateCanonical(
      MeshAssetKind meshKind,
      AnimationClassBytes animationLengths
    )
    {
      var serializedRepresentation = new byte[SerializedSize];
      serializedRepresentation[0] = (byte)'M';
      serializedRepresentation[1] = (byte)'E';
      serializedRepresentation[2] = (byte)'S';
      serializedRepresentation[3] = (byte)'H';
      BinaryPrimitives.WriteUInt32LittleEndian(serializedRepresentation.AsSpan(0x04), 1);
      BinaryPrimitives.WriteUInt32LittleEndian(
        serializedRepresentation.AsSpan(0x08),
        (uint)meshKind
      );
      WriteAnimationClassBytes(serializedRepresentation, 0x10, animationLengths);

      for (var attachment = 0; attachment < 49; attachment++)
      {
        var offset = 0x1D8 + (attachment * 8);
        BinaryPrimitives.WriteInt16LittleEndian(
          serializedRepresentation.AsSpan(offset),
          short.MinValue
        );
        BinaryPrimitives.WriteInt16LittleEndian(
          serializedRepresentation.AsSpan(offset + 2),
          short.MinValue
        );
        BinaryPrimitives.WriteInt16LittleEndian(
          serializedRepresentation.AsSpan(offset + 4),
          short.MinValue
        );
      }

      return new CommonMeshBaseHeader(serializedRepresentation);
    }

    private static AnimationClassBytes ReadAnimationClassBytes(ReadOnlySpan<byte> bytes)
    {
      return new AnimationClassBytes(bytes[3], bytes[2], bytes[1], bytes[0]);
    }

    private static void WriteAnimationClassBytes(
      Span<byte> data,
      int offset,
      AnimationClassBytes value
    )
    {
      data[offset] = value.D;
      data[offset + 1] = value.C;
      data[offset + 2] = value.B;
      data[offset + 3] = value.A;
    }

    private static IReadOnlyList<byte> Copy(ReadOnlySpan<byte> data, int offset, int length)
    {
      return Array.AsReadOnly(data.Slice(offset, length).ToArray());
    }
  }
}
