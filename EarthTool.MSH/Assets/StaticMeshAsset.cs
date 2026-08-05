#nullable enable

using EarthTool.MSH.Authoring;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
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

  /// <summary>Names the four recognized static animation classes.</summary>
  public enum StaticAnimationClass
  {
    /// <summary>Animation class A.</summary>
    A = 0,
    /// <summary>Animation class B.</summary>
    B = 1,
    /// <summary>Animation class C.</summary>
    C = 2,
    /// <summary>Animation class D.</summary>
    D = 3
  }

  /// <summary>Represents an immutable static MSH asset.</summary>
  public sealed class StaticMeshAsset : MeshAsset
  {
    /// <inheritdoc />
    public override MeshAssetKind Kind => MeshAssetKind.Static;

    /// <summary>Gets the root source-object identity.</summary>
    public SourceObjectId RootSourceObjectId { get; }

    /// <summary>Gets the source-object grouping view reconstructed from the authoritative sequence.</summary>
    public StaticSourceObject RootSourceObject { get; }

    /// <summary>Gets the authoritative static render-object sequence.</summary>
    public IReadOnlyList<StaticRenderObject> StaticRenderObjectSequence { get; }

    /// <summary>Gets the hierarchy unwind serialized before the static render-object sequence.</summary>
    public uint StoredTrailingHierarchyUnwindCount { get; }

    /// <summary>Gets the trailing hierarchy unwind derived from the reconstructed final source depth.</summary>
    public uint ExpectedTrailingHierarchyUnwindCount { get; }

    internal int? NextStaticRenderObjectLocalId { get; }

    internal int? NextSourceObjectLocalId { get; }

    internal StaticMeshAsset(
      MeshAssetLineageId lineageId,
      MeshArchiveFraming archiveFraming,
      CommonMeshBaseHeader commonBaseHeader,
      byte[] rootTrailingBytes,
      IEnumerable<StaticRenderObject> staticRenderObjectSequence,
      byte[] serializedRepresentation,
      MeshAssetOrigin origin,
      StaticSourceObject rootSourceObject,
      uint storedTrailingHierarchyUnwindCount,
      uint expectedTrailingHierarchyUnwindCount,
      int? nextStaticRenderObjectLocalId,
      int? nextSourceObjectLocalId)
      : base(lineageId, archiveFraming, commonBaseHeader, rootTrailingBytes, origin, serializedRepresentation)
    {
      StaticRenderObjectSequence = Array.AsReadOnly(
        new List<StaticRenderObject>(staticRenderObjectSequence).ToArray());
      RootSourceObject = rootSourceObject;
      RootSourceObjectId = rootSourceObject.Id;
      StoredTrailingHierarchyUnwindCount = storedTrailingHierarchyUnwindCount;
      ExpectedTrailingHierarchyUnwindCount = expectedTrailingHierarchyUnwindCount;
      NextStaticRenderObjectLocalId = nextStaticRenderObjectLocalId;
      NextSourceObjectLocalId = nextSourceObjectLocalId;
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

  /// <summary>Names recognized static render-object flag roles using their serialized bit values.</summary>
  [Flags]
  public enum StaticRenderObjectFlags : uint
  {
    /// <summary>No recognized role.</summary>
    None = 0,
    /// <summary>Faces the active viewer.</summary>
    ViewerFaced = 0x00000100,
    /// <summary>Uses the barrel transform role.</summary>
    Barrel = 0x00000200,
    /// <summary>Uses the rotor transform role.</summary>
    Rotor = 0x00000400,
    /// <summary>Begins a nested source object after applying the hierarchy unwind.</summary>
    BeginsNestedSourceObject = 0x00000800,
    /// <summary>Uses marker attachment 1.</summary>
    MarkerAttachment1 = 0x00001000,
    /// <summary>Uses marker attachment 2.</summary>
    MarkerAttachment2 = 0x00002000,
    /// <summary>Uses marker attachment 3.</summary>
    MarkerAttachment3 = 0x00004000,
    /// <summary>Uses marker attachment 4.</summary>
    MarkerAttachment4 = 0x00008000
  }

  internal static class StaticRenderObjectFlagMasks
  {
    internal const StaticRenderObjectFlags MarkerAttachments =
      StaticRenderObjectFlags.MarkerAttachment1
      | StaticRenderObjectFlags.MarkerAttachment2
      | StaticRenderObjectFlags.MarkerAttachment3
      | StaticRenderObjectFlags.MarkerAttachment4;
  }

  /// <summary>Groups render-object identities belonging to one reconstructed source object.</summary>
  public sealed class StaticSourceObject
  {
    /// <summary>Gets the lineage-scoped source-object identity.</summary>
    public SourceObjectId Id { get; }

    /// <summary>Gets render-object references in authoritative sequence order.</summary>
    public IReadOnlyList<StaticRenderObjectId> StaticRenderObjectIds { get; }

    /// <summary>Gets nested source objects in first-seen sequence order.</summary>
    public IReadOnlyList<StaticSourceObject> Children { get; }

    internal StaticSourceObject(
      SourceObjectId id,
      IEnumerable<StaticRenderObjectId> staticRenderObjectIds,
      IEnumerable<StaticSourceObject> children)
    {
      Id = id;
      StaticRenderObjectIds = Array.AsReadOnly(staticRenderObjectIds.ToArray());
      Children = Array.AsReadOnly(children.ToArray());
    }
  }

  /// <summary>Preserves the three optional baked animation tracks of one static render object.</summary>
  public sealed class StaticAnimationTracks
  {
    /// <summary>Gets ordered baked scale frames.</summary>
    public IReadOnlyList<Vector3> ScaleFrames { get; }

    /// <summary>Gets ordered baked translation frames in MSH coordinates.</summary>
    public IReadOnlyList<Vector3> TranslationFrames { get; }

    /// <summary>Gets ordered baked transform matrices.</summary>
    public IReadOnlyList<Matrix4x4> Matrices { get; }

    internal StaticAnimationTracks(
      IEnumerable<Vector3> scaleFrames,
      IEnumerable<Vector3> translationFrames,
      IEnumerable<Matrix4x4> matrices)
    {
      ScaleFrames = Array.AsReadOnly(scaleFrames.ToArray());
      TranslationFrames = Array.AsReadOnly(translationFrames.ToArray());
      Matrices = Array.AsReadOnly(matrices.ToArray());
    }
  }

  /// <summary>Represents one immutable static render object.</summary>
  public sealed class StaticRenderObject
  {
    private readonly byte[] _serializedRepresentation;

    /// <summary>Gets the lineage-scoped render-object identity.</summary>
    public StaticRenderObjectId Id { get; }

    /// <summary>Gets the lineage-local render-object identity.</summary>
    public int LocalId => Id.Value;

    /// <summary>Gets the source object that groups this render object.</summary>
    public SourceObjectId SourceObjectId { get; }

    /// <summary>Gets the ordered active render vertices.</summary>
    public IReadOnlyList<RenderVertex> RenderVertices { get; }

    /// <summary>Gets the ordered triangles.</summary>
    public IReadOnlyList<StaticTriangle> Triangles { get; }

    /// <summary>Gets the exact serialized vertex-block count.</summary>
    public uint VertexBlockCount { get; }

    /// <summary>Gets exact bytes belonging to inactive vertex-block lanes.</summary>
    public IReadOnlyList<byte> VertexBlockPadding { get; }

    /// <summary>Gets the exact serialized object flags.</summary>
    public uint ObjectFlags { get; }

    /// <summary>Gets recognized object-flag roles.</summary>
    public StaticRenderObjectFlags KnownFlags =>
      (StaticRenderObjectFlags)(ObjectFlags & 0x0000FF00);

    /// <summary>Gets the low-byte source hierarchy unwind.</summary>
    public byte HierarchyUnwindCount => (byte)ObjectFlags;

    /// <summary>Gets unclassified object-flag bits 16 through 31.</summary>
    public ushort UnclassifiedObjectFlagsHighWord => (ushort)(ObjectFlags >> 16);

    /// <summary>Gets the exact length-prefixed texture-path bytes.</summary>
    public IReadOnlyList<byte> TexturePathBytes { get; }

    /// <summary>Gets the three independent baked animation tracks.</summary>
    public StaticAnimationTracks AnimationTracks { get; }

    /// <summary>Gets the exact serialized animation-class selector.</summary>
    public uint AnimationClassValue { get; }

    /// <summary>Gets the recognized animation class, or null for another exact value.</summary>
    public StaticAnimationClass? KnownAnimationClass => AnimationClassValue <= 3
      ? (StaticAnimationClass)AnimationClassValue
      : null;

    /// <summary>Gets the source-object pivot in MSH coordinates.</summary>
    public Vector3 Pivot { get; }

    /// <summary>Gets the exact barrel maximum-angle byte.</summary>
    public byte BarrelMaximumAngle { get; }

    /// <summary>Gets the exact zero/nonzero marker linking the next sequence record.</summary>
    public uint NextRecordMarker { get; }

    internal StaticRenderObject(
      StaticRenderObjectId id,
      SourceObjectId sourceObjectId,
      IEnumerable<RenderVertex> renderVertices,
      IEnumerable<StaticTriangle> triangles,
      uint vertexBlockCount,
      IEnumerable<byte> vertexBlockPadding,
      uint objectFlags,
      IEnumerable<byte> texturePathBytes,
      StaticAnimationTracks animationTracks,
      uint animationClassValue,
      Vector3 pivot,
      byte barrelMaximumAngle,
      uint nextRecordMarker,
      byte[] serializedRepresentation)
    {
      Id = id;
      SourceObjectId = sourceObjectId;
      RenderVertices = Array.AsReadOnly(new List<RenderVertex>(renderVertices).ToArray());
      Triangles = Array.AsReadOnly(new List<StaticTriangle>(triangles).ToArray());
      VertexBlockCount = vertexBlockCount;
      VertexBlockPadding = Array.AsReadOnly(vertexBlockPadding.ToArray());
      ObjectFlags = objectFlags;
      TexturePathBytes = Array.AsReadOnly(texturePathBytes.ToArray());
      AnimationTracks = animationTracks;
      AnimationClassValue = animationClassValue;
      Pivot = pivot;
      BarrelMaximumAngle = barrelMaximumAngle;
      NextRecordMarker = nextRecordMarker;
      _serializedRepresentation = (byte[])serializedRepresentation.Clone();
    }

    internal byte[] GetSerializedRepresentation()
    {
      return (byte[])_serializedRepresentation.Clone();
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

    /// <summary>Gets the exact reserved third texture-coordinate lane.</summary>
    public float ReservedTextureComponent { get; }

    /// <summary>Gets an earlier absolute render-vertex index sharing the lighting normal.</summary>
    public ushort NormalSharingIndex { get; }

    /// <summary>Gets an earlier absolute render-vertex index sharing the transformed position.</summary>
    public ushort PositionSharingIndex { get; }

    internal RenderVertex(
      Vector3 position,
      Vector3 normal,
      Vector2 textureCoordinate,
      float textureCoordinateReserved = 0,
      ushort normalSharingIndex = ushort.MaxValue,
      ushort positionSharingIndex = ushort.MaxValue)
    {
      Position = position;
      Normal = normal;
      TextureCoordinate = textureCoordinate;
      ReservedTextureComponent = textureCoordinateReserved;
      NormalSharingIndex = normalSharingIndex;
      PositionSharingIndex = positionSharingIndex;
    }

    /// <inheritdoc />
    public bool Equals(RenderVertex other)
    {
      return Position.Equals(other.Position)
        && Normal.Equals(other.Normal)
        && TextureCoordinate.Equals(other.TextureCoordinate)
        && ReservedTextureComponent.Equals(other.ReservedTextureComponent)
        && NormalSharingIndex == other.NormalSharingIndex
        && PositionSharingIndex == other.PositionSharingIndex;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is RenderVertex other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (Position, Normal, TextureCoordinate, ReservedTextureComponent,
        NormalSharingIndex, PositionSharingIndex).GetHashCode();
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
