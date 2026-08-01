#nullable enable

using EarthTool.MSH.Authoring;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;

namespace EarthTool.MSH.Assets
{
  /// <summary>Identifies the closed payload branch of a mesh asset.</summary>
  public enum MeshAssetKind
  {
    /// <summary>Static geometry payload.</summary>
    Static = 0,
    /// <summary>Dynamic effect payload.</summary>
    Dynamic = 1
  }

  /// <summary>Identifies how an immutable mesh snapshot was constructed.</summary>
  public enum MeshAssetOrigin
  {
    /// <summary>Accepted from serialized MSH input.</summary>
    Loaded = 0,
    /// <summary>Created through a canonical semantic builder.</summary>
    Canonical = 1,
    /// <summary>Created through the exact serialized expert boundary.</summary>
    Expert = 2
  }

  /// <summary>Scopes nonserialized object identities to one mesh lineage.</summary>
  public readonly struct MeshAssetLineageId : IEquatable<MeshAssetLineageId>
  {
    /// <summary>Gets the lineage UUID.</summary>
    public Guid Value { get; }

    /// <summary>Initializes a lineage identity.</summary>
    public MeshAssetLineageId(Guid value)
    {
      Value = value;
    }

    /// <inheritdoc />
    public bool Equals(MeshAssetLineageId other)
    {
      return Value.Equals(other.Value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is MeshAssetLineageId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return Value.GetHashCode();
    }
  }

  /// <summary>Identifies one static render object within an asset lineage.</summary>
  public readonly struct StaticRenderObjectId : IEquatable<StaticRenderObjectId>
  {
    /// <summary>Gets the owning lineage.</summary>
    public MeshAssetLineageId Lineage { get; }
    /// <summary>Gets the lineage-local value.</summary>
    public int Value { get; }

    /// <summary>Initializes a static render-object identity.</summary>
    public StaticRenderObjectId(MeshAssetLineageId lineage, int value)
    {
      Lineage = lineage;
      Value = value;
    }

    /// <inheritdoc />
    public bool Equals(StaticRenderObjectId other)
    {
      return Lineage.Equals(other.Lineage) && Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is StaticRenderObjectId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (Lineage, Value).GetHashCode();
    }
  }

  /// <summary>Identifies one source object within an asset lineage.</summary>
  public readonly struct SourceObjectId : IEquatable<SourceObjectId>
  {
    /// <summary>Gets the owning lineage.</summary>
    public MeshAssetLineageId Lineage { get; }
    /// <summary>Gets the lineage-local value.</summary>
    public int Value { get; }

    /// <summary>Initializes a source-object identity.</summary>
    public SourceObjectId(MeshAssetLineageId lineage, int value)
    {
      Lineage = lineage;
      Value = value;
    }

    /// <inheritdoc />
    public bool Equals(SourceObjectId other)
    {
      return Lineage.Equals(other.Lineage) && Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is SourceObjectId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (Lineage, Value).GetHashCode();
    }
  }

  /// <summary>Preserves the independently serialized top-level MSH framing declaration.</summary>
  public sealed class MeshArchiveFraming
  {
    /// <summary>Gets the exact archive framing declaration.</summary>
    public uint Declaration { get; }

    /// <summary>Gets the optional independently serialized archive type.</summary>
    public uint? ArchiveType { get; }

    /// <summary>Gets the optional creation identity.</summary>
    public Guid? CreationGuid { get; }

    internal MeshArchiveFraming(uint declaration, uint? archiveType, Guid? creationGuid)
    {
      Declaration = declaration;
      ArchiveType = archiveType;
      CreationGuid = creationGuid;
    }
  }

  /// <summary>Defines the closed immutable root for safely accepted MSH assets.</summary>
  public abstract class MeshAsset
  {
    private readonly byte[] _serializedRepresentation;

    /// <summary>Gets the lineage that scopes nonserialized object identities.</summary>
    public MeshAssetLineageId LineageId { get; }

    /// <summary>Gets how this snapshot was constructed.</summary>
    public MeshAssetOrigin Origin { get; }

    /// <summary>Gets the closed payload branch.</summary>
    public abstract MeshAssetKind Kind { get; }

    /// <summary>Gets the preserved archive framing.</summary>
    public MeshArchiveFraming ArchiveFraming { get; }

    /// <summary>Gets the exact common 0x368-byte MESH base header.</summary>
    public CommonMeshBaseHeader CommonBaseHeader { get; }

    /// <summary>Gets the opaque bytes following the complete declared root payload.</summary>
    public IReadOnlyList<byte> RootTrailingBytes { get; }

    internal MeshAsset(
      MeshAssetLineageId lineageId,
      MeshArchiveFraming archiveFraming,
      CommonMeshBaseHeader commonBaseHeader,
      byte[] rootTrailingBytes,
      MeshAssetOrigin origin,
      byte[] serializedRepresentation)
    {
      LineageId = lineageId;
      ArchiveFraming = archiveFraming;
      CommonBaseHeader = commonBaseHeader;
      RootTrailingBytes = Array.AsReadOnly((byte[])rootTrailingBytes.Clone());
      Origin = origin;
      _serializedRepresentation = (byte[])serializedRepresentation.Clone();
    }

    /// <summary>Matches the closed asset branch without a concrete cast.</summary>
    public abstract TResult Match<TResult>(
      Func<StaticMeshAsset, TResult> onStatic,
      Func<DynamicMeshAsset, TResult> onDynamic);

    /// <summary>Visits the closed asset branch without a concrete cast.</summary>
    public abstract void Match(Action<StaticMeshAsset> onStatic, Action<DynamicMeshAsset> onDynamic);

    internal byte[] GetSerializedRepresentation()
    {
      return (byte[])_serializedRepresentation.Clone();
    }

    internal int SerializedLength => _serializedRepresentation.Length;
  }

  /// <summary>Preserves the four reverse-packed animation-class bytes.</summary>
  public readonly struct AnimationClassBytes : IEquatable<AnimationClassBytes>
  {
    /// <summary>Gets the class A byte.</summary>
    public byte A { get; }
    /// <summary>Gets the class B byte.</summary>
    public byte B { get; }
    /// <summary>Gets the class C byte.</summary>
    public byte C { get; }
    /// <summary>Gets the class D byte.</summary>
    public byte D { get; }

    /// <summary>Initializes four animation-class bytes.</summary>
    public AnimationClassBytes(byte a, byte b, byte c, byte d)
    {
      A = a;
      B = b;
      C = c;
      D = d;
    }

    /// <inheritdoc />
    public bool Equals(AnimationClassBytes other)
    {
      return A == other.A && B == other.B && C == other.C && D == other.D;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is AnimationClassBytes other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (A, B, C, D).GetHashCode();
    }
  }

  /// <summary>Provides exact fixed-region ownership and named semantic views for a MESH base header.</summary>
  public sealed class CommonMeshBaseHeader
  {
    /// <summary>Gets the MSH format version.</summary>
    public uint Version { get; }
    /// <summary>Gets the exact root payload discriminator.</summary>
    public uint MeshKind { get; }
    /// <summary>Gets the exact box-presence mask.</summary>
    public uint BoxPresenceMask { get; }
    /// <summary>Gets animation lengths for classes A through D.</summary>
    public AnimationClassBytes AnimationLengths { get; }
    /// <summary>Gets current frame indices for classes A through D.</summary>
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

    internal CommonMeshBaseHeader(byte[] serializedRepresentation)
    {
      if (serializedRepresentation.Length != 0x368)
      {
        throw new ArgumentException("A common MESH base header must contain exactly 0x368 bytes.",
          nameof(serializedRepresentation));
      }

      var data = serializedRepresentation.AsSpan();
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
      SerializedRepresentation = Array.AsReadOnly((byte[])serializedRepresentation.Clone());
    }

    private static AnimationClassBytes ReadAnimationClassBytes(ReadOnlySpan<byte> bytes)
    {
      return new AnimationClassBytes(bytes[3], bytes[2], bytes[1], bytes[0]);
    }

    private static IReadOnlyList<byte> Copy(ReadOnlySpan<byte> data, int offset, int length)
    {
      return Array.AsReadOnly(data.Slice(offset, length).ToArray());
    }
  }

  /// <summary>Represents an immutable static MSH asset.</summary>
  public sealed class StaticMeshAsset : MeshAsset
  {
    /// <inheritdoc />
    public override MeshAssetKind Kind => MeshAssetKind.Static;

    /// <summary>Gets the root source-object identity for the current supported static slice.</summary>
    public SourceObjectId RootSourceObjectId { get; }

    /// <summary>Gets the authoritative static render-object sequence.</summary>
    public IReadOnlyList<StaticRenderObject> StaticRenderObjectSequence { get; }

    internal StaticMeshAsset(
      MeshAssetLineageId lineageId,
      MeshArchiveFraming archiveFraming,
      CommonMeshBaseHeader commonBaseHeader,
      byte[] rootTrailingBytes,
      IEnumerable<StaticRenderObject> staticRenderObjectSequence,
      byte[] serializedRepresentation,
      MeshAssetOrigin origin,
      SourceObjectId rootSourceObjectId)
      : base(lineageId, archiveFraming, commonBaseHeader, rootTrailingBytes, origin, serializedRepresentation)
    {
      StaticRenderObjectSequence = Array.AsReadOnly(
        new List<StaticRenderObject>(staticRenderObjectSequence).ToArray());
      RootSourceObjectId = rootSourceObjectId;
    }

    /// <summary>Starts a one-shot edit session for this snapshot.</summary>
    public StaticMeshEditSession Edit()
    {
      return new StaticMeshEditSession(this);
    }

    /// <inheritdoc />
    public override TResult Match<TResult>(
      Func<StaticMeshAsset, TResult> onStatic,
      Func<DynamicMeshAsset, TResult> onDynamic)
    {
      if (onStatic is null)
      {
        throw new ArgumentNullException(nameof(onStatic));
      }

      if (onDynamic is null)
      {
        throw new ArgumentNullException(nameof(onDynamic));
      }

      return onStatic(this);
    }

    /// <inheritdoc />
    public override void Match(Action<StaticMeshAsset> onStatic, Action<DynamicMeshAsset> onDynamic)
    {
      if (onStatic is null)
      {
        throw new ArgumentNullException(nameof(onStatic));
      }

      if (onDynamic is null)
      {
        throw new ArgumentNullException(nameof(onDynamic));
      }

      onStatic(this);
    }
  }

  /// <summary>Represents one immutable static render object.</summary>
  public sealed class StaticRenderObject
  {
    /// <summary>Gets the lineage-scoped render-object identity.</summary>
    public StaticRenderObjectId Id { get; }

    /// <summary>Gets the lineage-local render-object identity.</summary>
    public int LocalId => Id.Value;

    /// <summary>Gets the ordered active render vertices.</summary>
    public IReadOnlyList<RenderVertex> RenderVertices { get; }

    /// <summary>Gets the ordered triangles.</summary>
    public IReadOnlyList<StaticTriangle> Triangles { get; }

    internal StaticRenderObject(
      StaticRenderObjectId id,
      IEnumerable<RenderVertex> renderVertices,
      IEnumerable<StaticTriangle> triangles)
    {
      Id = id;
      RenderVertices = Array.AsReadOnly(new List<RenderVertex>(renderVertices).ToArray());
      Triangles = Array.AsReadOnly(new List<StaticTriangle>(triangles).ToArray());
    }
  }

  /// <summary>Names recognized dynamic-effect values without hiding unrecognized serialized values.</summary>
  public enum DynamicEffectType
  {
    /// <summary>Child-container effect.</summary>
    Group = 0,
    /// <summary>Explosion effect.</summary>
    Explosion = 1,
    /// <summary>Track effect.</summary>
    Track = 2,
    /// <summary>Scalable-object effect.</summary>
    ScalableObject = 3,
    /// <summary>Mapped explosion effect.</summary>
    MappedExplosion = 4,
    /// <summary>Flat explosion effect.</summary>
    FlatExplosion = 5,
    /// <summary>Laser effect.</summary>
    Laser = 6,
    /// <summary>Laser-wall effect.</summary>
    LaserWall = 7,
    /// <summary>Shockwave effect.</summary>
    Shockwave = 8,
    /// <summary>Line effect.</summary>
    Line = 9,
    /// <summary>Sphere effect.</summary>
    Sphere = 10,
    /// <summary>Electrical-cannon effect.</summary>
    ElectricalCannon = 11,
    /// <summary>Lightning effect.</summary>
    Lightning = 12,
    /// <summary>Smoke effect.</summary>
    Smoke = 13,
    /// <summary>Keelwater effect.</summary>
    Keelwater = 14
  }

  /// <summary>Names recognized dynamic terrain-light values.</summary>
  public enum DynamicLightType
  {
    /// <summary>Constant intensity.</summary>
    Constant = 0,
    /// <summary>Pyramid intensity profile.</summary>
    Pyramid = 1,
    /// <summary>Trapezium intensity profile.</summary>
    Trapezium = 2,
    /// <summary>Random intensity profile.</summary>
    Random = 3
  }

  /// <summary>Names canonical alpha interpolation timing modes.</summary>
  public enum DynamicAlphaTiming
  {
    /// <summary>Interpolate alpha with the selected frame phase.</summary>
    FramePhase = 0,
    /// <summary>Interpolate alpha with lifetime progress.</summary>
    LifetimeProgress = 1
  }

  /// <summary>Preserves one four-lane dynamic-effect rectangle.</summary>
  public readonly struct EffectRectangle : IEquatable<EffectRectangle>
  {
    /// <summary>Gets the first X lane.</summary>
    public float X0 { get; }
    /// <summary>Gets the first Y lane.</summary>
    public float Y1 { get; }
    /// <summary>Gets the second X lane.</summary>
    public float X1 { get; }
    /// <summary>Gets the second Y lane.</summary>
    public float Y0 { get; }
    /// <summary>Gets the semantic left lane without sorting.</summary>
    public float Left => X0;
    /// <summary>Gets the semantic top lane without sorting.</summary>
    public float Top => Y1;
    /// <summary>Gets the semantic right lane without sorting.</summary>
    public float Right => X1;
    /// <summary>Gets the semantic bottom lane without sorting.</summary>
    public float Bottom => Y0;

    /// <summary>Initializes an exact effect rectangle.</summary>
    public EffectRectangle(float x0, float y1, float x1, float y0)
    {
      X0 = x0;
      Y1 = y1;
      X1 = x1;
      Y0 = y0;
    }

    /// <inheritdoc />
    public bool Equals(EffectRectangle other)
    {
      return X0.Equals(other.X0)
        && Y1.Equals(other.Y1)
        && X1.Equals(other.X1)
        && Y0.Equals(other.Y0);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is EffectRectangle other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (X0, Y1, X1, Y0).GetHashCode();
    }
  }

  /// <summary>Preserves one complete fixed and variable dynamic-effect extension.</summary>
  public sealed class DynamicEffectExtension
  {
    /// <summary>Gets the exact serialized effect value.</summary>
    public uint EffectType { get; }
    /// <summary>Gets the recognized effect view, or null for an unrecognized value.</summary>
    public DynamicEffectType? KnownEffectType { get; }
    /// <summary>Gets the exact serialized light value.</summary>
    public uint LightType { get; }
    /// <summary>Gets the recognized light view, or null for an unrecognized value.</summary>
    public DynamicLightType? KnownLightType { get; }
    /// <summary>Gets the first source frame.</summary>
    public int FirstSourceFrame { get; }
    /// <summary>Gets the exact signed frame-count representation.</summary>
    public int FrameCount { get; }
    /// <summary>Gets the sprite-sheet column count.</summary>
    public int SpriteSheetColumnCount { get; }
    /// <summary>Gets the sprite-sheet row count.</summary>
    public int SpriteSheetRowCount { get; }
    /// <summary>Gets the frame period in simulation ticks.</summary>
    public int FramePeriodTicks { get; }
    /// <summary>Gets the serialized reciprocal column count.</summary>
    public float ReciprocalColumnCount { get; }
    /// <summary>Gets the serialized reciprocal row count.</summary>
    public float ReciprocalRowCount { get; }
    /// <summary>Gets the start effect rectangle.</summary>
    public EffectRectangle StartEffectRectangle { get; }
    /// <summary>Gets the end effect rectangle.</summary>
    public EffectRectangle EndEffectRectangle { get; }
    /// <summary>Gets the effect depth offset.</summary>
    public float EffectDepthOffset { get; }
    /// <summary>Gets the signed ribbon half-width.</summary>
    public float RibbonHalfWidth { get; }
    /// <summary>Gets the preserved reserved dynamic word.</summary>
    public uint ReservedWord { get; }
    /// <summary>Gets the exact additive-flag representation.</summary>
    public int AdditiveFlag { get; }
    /// <summary>Gets whether the exact additive representation selects additive blending.</summary>
    public bool UsesAdditiveBlending => AdditiveFlag != 0;
    /// <summary>Gets the terrain-light RGB values.</summary>
    public Vector3 TerrainLightColor { get; }
    /// <summary>Gets the visible effect RGB values.</summary>
    public Vector3 VisibleEffectColor { get; }
    /// <summary>Gets the visible terrain-light gain.</summary>
    public float VisibleTerrainLightGain { get; }
    /// <summary>Gets the exact alpha timing mode.</summary>
    public int AlphaTimingMode { get; }
    /// <summary>Gets the canonical alpha timing value, or null for another exact nonzero representation.</summary>
    public DynamicAlphaTiming? KnownAlphaTiming { get; }
    /// <summary>Gets whether alpha interpolation uses lifetime progress.</summary>
    public bool UsesLifetimeProgressAlpha => AlphaTimingMode != 0;
    /// <summary>Gets the end alpha.</summary>
    public float EndAlpha { get; }
    /// <summary>Gets the start alpha.</summary>
    public float StartAlpha { get; }
    /// <summary>Gets the end model scale.</summary>
    public float EndModelScale { get; }
    /// <summary>Gets the start model scale.</summary>
    public float StartModelScale { get; }
    /// <summary>Gets the child start translation in MSH coordinates.</summary>
    public Vector3 ChildStartTranslation { get; }
    /// <summary>Gets the child end translation in MSH coordinates.</summary>
    public Vector3 ChildEndTranslation { get; }
    /// <summary>Gets the exact mesh-name bytes.</summary>
    public IReadOnlyList<byte> MeshNameBytes { get; }
    /// <summary>Gets the exact texture-path bytes.</summary>
    public IReadOnlyList<byte> TexturePathBytes { get; }
    /// <summary>Gets the exact fixed 0x9C-byte extension representation.</summary>
    public IReadOnlyList<byte> SerializedRepresentation { get; }

    internal DynamicEffectExtension(
      byte[] serializedRepresentation,
      byte[] meshNameBytes,
      byte[] texturePathBytes)
    {
      if (serializedRepresentation.Length != 0x9C)
      {
        throw new ArgumentException("A fixed dynamic extension must contain exactly 0x9C bytes.",
          nameof(serializedRepresentation));
      }

      var data = serializedRepresentation.AsSpan();
      EffectType = ReadUInt32(data, 0x00);
      KnownEffectType = EffectType <= (uint)DynamicEffectType.Keelwater
        ? (DynamicEffectType)EffectType
        : null;
      LightType = ReadUInt32(data, 0x04);
      KnownLightType = LightType <= (uint)DynamicLightType.Random
        ? (DynamicLightType)LightType
        : null;
      FirstSourceFrame = ReadInt32(data, 0x08);
      FrameCount = ReadInt32(data, 0x0C);
      SpriteSheetColumnCount = ReadInt32(data, 0x10);
      SpriteSheetRowCount = ReadInt32(data, 0x14);
      FramePeriodTicks = ReadInt32(data, 0x18);
      ReciprocalColumnCount = ReadSingle(data, 0x1C);
      ReciprocalRowCount = ReadSingle(data, 0x20);
      StartEffectRectangle = ReadRectangle(data, 0x24);
      EndEffectRectangle = ReadRectangle(data, 0x34);
      EffectDepthOffset = ReadSingle(data, 0x44);
      RibbonHalfWidth = ReadSingle(data, 0x48);
      ReservedWord = ReadUInt32(data, 0x4C);
      AdditiveFlag = ReadInt32(data, 0x50);
      TerrainLightColor = ReadVector3(data, 0x54, invertY: false);
      VisibleEffectColor = ReadVector3(data, 0x60, invertY: false);
      VisibleTerrainLightGain = ReadSingle(data, 0x6C);
      AlphaTimingMode = ReadInt32(data, 0x70);
      KnownAlphaTiming = AlphaTimingMode == 0
        ? DynamicAlphaTiming.FramePhase
        : AlphaTimingMode == 1
          ? DynamicAlphaTiming.LifetimeProgress
          : null;
      EndAlpha = ReadSingle(data, 0x74);
      StartAlpha = ReadSingle(data, 0x78);
      EndModelScale = ReadSingle(data, 0x7C);
      StartModelScale = ReadSingle(data, 0x80);
      ChildStartTranslation = ReadVector3(data, 0x84, invertY: true);
      ChildEndTranslation = ReadVector3(data, 0x90, invertY: true);
      MeshNameBytes = Array.AsReadOnly((byte[])meshNameBytes.Clone());
      TexturePathBytes = Array.AsReadOnly((byte[])texturePathBytes.Clone());
      SerializedRepresentation = Array.AsReadOnly((byte[])serializedRepresentation.Clone());
    }

    private static EffectRectangle ReadRectangle(ReadOnlySpan<byte> data, int offset)
    {
      return new EffectRectangle(
        ReadSingle(data, offset),
        ReadSingle(data, offset + 4),
        ReadSingle(data, offset + 8),
        ReadSingle(data, offset + 12));
    }

    private static Vector3 ReadVector3(ReadOnlySpan<byte> data, int offset, bool invertY)
    {
      var y = ReadSingle(data, offset + 4);
      return new Vector3(ReadSingle(data, offset), invertY ? -y : y, ReadSingle(data, offset + 8));
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    }

    private static int ReadInt32(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4));
    }

    private static float ReadSingle(ReadOnlySpan<byte> data, int offset)
    {
      return BitConverter.Int32BitsToSingle(ReadInt32(data, offset));
    }
  }

  /// <summary>Represents one immutable complete dynamic object.</summary>
  public sealed class DynamicObject
  {
    /// <summary>Gets the exact inherited common base header.</summary>
    public CommonMeshBaseHeader CommonBaseHeader { get; }
    /// <summary>Gets the complete dynamic-effect extension.</summary>
    public DynamicEffectExtension Extension { get; }
    /// <summary>Gets the ordered child objects.</summary>
    public IReadOnlyList<DynamicObject> Children { get; }

    internal DynamicObject(
      CommonMeshBaseHeader commonBaseHeader,
      DynamicEffectExtension extension,
      IEnumerable<DynamicObject> children)
    {
      CommonBaseHeader = commonBaseHeader;
      Extension = extension;
      Children = Array.AsReadOnly(new List<DynamicObject>(children).ToArray());
    }
  }

  /// <summary>Represents an immutable dynamic MSH asset.</summary>
  public sealed class DynamicMeshAsset : MeshAsset
  {
    /// <inheritdoc />
    public override MeshAssetKind Kind => MeshAssetKind.Dynamic;

    /// <summary>Gets the root dynamic object.</summary>
    public DynamicObject RootDynamicObject { get; }

    internal DynamicMeshAsset(
      MeshAssetLineageId lineageId,
      MeshArchiveFraming archiveFraming,
      CommonMeshBaseHeader commonBaseHeader,
      DynamicObject rootDynamicObject,
      byte[] rootTrailingBytes,
      byte[] serializedRepresentation,
      MeshAssetOrigin origin)
      : base(lineageId, archiveFraming, commonBaseHeader, rootTrailingBytes, origin, serializedRepresentation)
    {
      RootDynamicObject = rootDynamicObject;
    }

    /// <inheritdoc />
    public override TResult Match<TResult>(
      Func<StaticMeshAsset, TResult> onStatic,
      Func<DynamicMeshAsset, TResult> onDynamic)
    {
      if (onDynamic is null)
      {
        throw new ArgumentNullException(nameof(onDynamic));
      }

      if (onStatic is null)
      {
        throw new ArgumentNullException(nameof(onStatic));
      }

      return onDynamic(this);
    }

    /// <inheritdoc />
    public override void Match(Action<StaticMeshAsset> onStatic, Action<DynamicMeshAsset> onDynamic)
    {
      if (onDynamic is null)
      {
        throw new ArgumentNullException(nameof(onDynamic));
      }

      if (onStatic is null)
      {
        throw new ArgumentNullException(nameof(onStatic));
      }

      onDynamic(this);
    }
  }

  /// <summary>Represents one immutable artist-editable render vertex.</summary>
  public readonly struct RenderVertex : IEquatable<RenderVertex>
  {
    /// <summary>Gets the MSH-space position.</summary>
    public Vector3 Position { get; }

    /// <summary>Gets the MSH-space normal.</summary>
    public Vector3 Normal { get; }

    /// <summary>Gets the native texture coordinate.</summary>
    public Vector2 TextureCoordinate { get; }

    internal RenderVertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
    {
      Position = position;
      Normal = normal;
      TextureCoordinate = textureCoordinate;
    }

    /// <inheritdoc />
    public bool Equals(RenderVertex other)
    {
      return Position.Equals(other.Position)
        && Normal.Equals(other.Normal)
        && TextureCoordinate.Equals(other.TextureCoordinate);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is RenderVertex other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (Position, Normal, TextureCoordinate).GetHashCode();
    }
  }

  /// <summary>Represents one immutable indexed static triangle.</summary>
  public readonly struct StaticTriangle : IEquatable<StaticTriangle>
  {
    /// <summary>Gets the first vertex index.</summary>
    public ushort Vertex0 { get; }

    /// <summary>Gets the second vertex index.</summary>
    public ushort Vertex1 { get; }

    /// <summary>Gets the third vertex index.</summary>
    public ushort Vertex2 { get; }

    /// <summary>Gets the preserved triangle render-pass flags.</summary>
    public ushort TriangleRenderPassFlags { get; }

    /// <summary>Initializes a static triangle.</summary>
    public StaticTriangle(ushort vertex0, ushort vertex1, ushort vertex2, ushort triangleRenderPassFlags)
    {
      Vertex0 = vertex0;
      Vertex1 = vertex1;
      Vertex2 = vertex2;
      TriangleRenderPassFlags = triangleRenderPassFlags;
    }

    /// <inheritdoc />
    public bool Equals(StaticTriangle other)
    {
      return Vertex0 == other.Vertex0
        && Vertex1 == other.Vertex1
        && Vertex2 == other.Vertex2
        && TriangleRenderPassFlags == other.TriangleRenderPassFlags;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is StaticTriangle other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (Vertex0, Vertex1, Vertex2, TriangleRenderPassFlags).GetHashCode();
    }
  }
}
