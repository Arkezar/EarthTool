using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace EarthTool.MSH;

public enum MeshKind
{
  Static = 0,
  Dynamic = 1
}

public enum MshDiagnosticSeverity
{
  Warning,
  Error
}

public enum PreservationDisposition
{
  Retained,
  Regenerated,
  Invalidated,
  Canonicalized
}

public enum MeshAssetOrigin
{
  Loaded,
  Canonical,
  Expert
}

public enum DynamicEffectKind
{
  Group = 0,
  Billboard = 1,
  ScalableObject = 2,
  Lightning = 3,
  Keelwater = 4
}

public enum TerrainLightKind
{
  Constant = 0,
  Pyramid = 1,
  Trapezium = 2,
  Random = 3
}

public enum AnimationClass
{
  A = 0,
  B = 1,
  C = 2,
  D = 3
}

public readonly record struct Float32Bits(uint Bits)
{
  public float Value => BitConverter.Int32BitsToSingle(unchecked((int)Bits));

  public static Float32Bits FromValue(float value)
    => new(unchecked((uint)BitConverter.SingleToInt32Bits(value)));
}

public readonly record struct StoredVector3(Float32Bits X, Float32Bits Y, Float32Bits Z)
{
  public Vector3 Value => new(X.Value, Y.Value, Z.Value);

  public static StoredVector3 FromValue(Vector3 value)
    => new(Float32Bits.FromValue(value.X), Float32Bits.FromValue(value.Y), Float32Bits.FromValue(value.Z));
}

public readonly record struct MeshAssetLineageId(Guid Value);

public readonly record struct StaticRenderObjectId(MeshAssetLineageId Lineage, int Value);

public readonly record struct SourceObjectId(MeshAssetLineageId Lineage, int Value);

public readonly record struct MshDiagnosticCode(string Value)
{
  public static MshDiagnosticCode InvalidArchiveSignature => new("MSH0001");
  public static MshDiagnosticCode ArchiveTypeKindMismatch => new("MSH1001");
  public static MshDiagnosticCode UnknownDynamicEffect => new("MSH1101");
}

public enum MshDiagnosticValueKind
{
  SignedInteger,
  UnsignedInteger,
  FloatingPoint,
  Text
}

public readonly record struct MshDiagnosticValue(
  MshDiagnosticValueKind Kind,
  long SignedInteger,
  ulong UnsignedInteger,
  double FloatingPoint,
  string? Text)
{
  public static MshDiagnosticValue From(long value)
    => new(MshDiagnosticValueKind.SignedInteger, value, 0, 0, null);

  public static MshDiagnosticValue From(ulong value)
    => new(MshDiagnosticValueKind.UnsignedInteger, 0, value, 0, null);

  public static MshDiagnosticValue From(double value)
    => new(MshDiagnosticValueKind.FloatingPoint, 0, 0, value, null);

  public static MshDiagnosticValue From(string value)
    => new(MshDiagnosticValueKind.Text, 0, 0, 0, value);
}

public sealed record MshDiagnosticFact(string Name, MshDiagnosticValue Value);

public sealed record MshDiagnostic(
  MshDiagnosticCode Code,
  int EventId,
  MshDiagnosticSeverity Severity,
  string FieldPath,
  long? ByteOffset,
  int? ByteLength,
  ImmutableArray<MshDiagnosticFact> Facts,
  string Message);

public sealed class ArchiveFraming
{
  internal ArchiveFraming(uint framingWordRaw, uint? archiveTypeRaw, ImmutableArray<byte> guidBytes)
  {
    FramingWordRaw = framingWordRaw;
    ArchiveTypeRaw = archiveTypeRaw;
    GuidBytes = guidBytes;
  }

  public uint FramingWordRaw { get; }
  public uint? ArchiveTypeRaw { get; }
  public ImmutableArray<byte> GuidBytes { get; }
  public bool HasArchiveType => (FramingWordRaw & 0x10000000) != 0;
  public bool HasGuid => (FramingWordRaw & 0x20000000) != 0;
  public Guid? Guid => GuidBytes.Length == 16 ? new Guid(GuidBytes.AsSpan()) : null;
}

public sealed record PackedAnimationWord(uint Raw)
{
  public byte A => (byte)(Raw >> 24);
  public byte B => (byte)(Raw >> 16);
  public byte C => (byte)(Raw >> 8);
  public byte D => (byte)Raw;
}

public sealed record FootprintRepresentations(
  uint BoxPresenceMaskRaw,
  ImmutableArray<ushort> BoxTopElevationsRaw,
  ImmutableArray<byte> BoxCornerPassageFlagsRaw,
  ImmutableArray<uint> RotatedOccupancyDescriptorsRaw,
  ImmutableArray<ulong> RotatedCornerPassageMapsRaw);

public sealed record AttachmentRecord(
  short XRaw,
  short YRaw,
  short ZRaw,
  byte HeadingRaw,
  byte ExtraByteRaw)
{
  public bool IsCanonicalAbsent => XRaw == short.MinValue && YRaw == short.MinValue && ZRaw == short.MinValue;
  public bool IsRuntimeAvailable => XRaw != short.MinValue;
  public Vector3 Position => new(XRaw / 256f, -YRaw / 256f, ZRaw / 256f);
  public float HeadingRadians => HeadingRaw * MathF.Tau / 256f;
}

public sealed record AttachmentTable(ImmutableArray<AttachmentRecord> Records)
{
  public AttachmentRecord this[int attachmentNumber] => Records[attachmentNumber - 1];

  public float? GetCannonYawHalfRangeRadians(int attachmentNumber)
    => attachmentNumber is >= 1 and <= 4
      ? this[attachmentNumber].ExtraByteRaw * MathF.Tau / 256f
      : null;
}

public sealed record HorizontalExtents(
  ushort PositiveYRaw,
  ushort NegativeYRaw,
  ushort PositiveXRaw,
  ushort NegativeXRaw)
{
  public Vector4 WorldUnits => new(
    PositiveYRaw / 256f,
    NegativeYRaw / 256f,
    PositiveXRaw / 256f,
    NegativeXRaw / 256f);
}

public sealed record BaseHeader(
  MeshKind MeshKind,
  PackedAnimationWord AnimationLengths,
  PackedAnimationWord AnimationFrameIndices,
  FootprintRepresentations FootprintRepresentations,
  AttachmentTable AttachmentTable,
  ImmutableArray<StoredVector3> CannonRenderPositions,
  StaticLightTable<SpotLightRecord> SpotLights,
  StaticLightTable<OmniLightRecord> OmniLights,
  HorizontalExtents HorizontalExtents);

public sealed record StaticLightTable<TLight>(ImmutableArray<TLight> Records)
  where TLight : StaticLightRecord
{
  public TLight this[int lightNumber] => Records[lightNumber - 1];
}

public abstract record StaticLightRecord
{
  private protected StaticLightRecord(
    StoredVector3 position,
    StoredVector3 colorRgb,
    Float32Bits terrainLightAmplitude)
  {
    Position = position;
    ColorRgb = colorRgb;
    TerrainLightAmplitude = terrainLightAmplitude;
  }

  public StoredVector3 Position { get; }
  public StoredVector3 ColorRgb { get; }
  public Float32Bits TerrainLightAmplitude { get; }
}

public sealed record SpotLightRecord : StaticLightRecord
{
  public SpotLightRecord(
    StoredVector3 position,
    StoredVector3 colorRgb,
    Float32Bits terrainLightAmplitude,
    Float32Bits approximateTargetDistance,
    byte targetHeadingRaw,
    ImmutableArray<byte> headingPaddingBytesRaw,
    Float32Bits coneHalfAngleTangent,
    Float32Bits halfFalloffAngleDistanceProduct,
    Float32Bits verticalTargetSlope)
    : base(position, colorRgb, terrainLightAmplitude)
  {
    ApproximateTargetDistance = approximateTargetDistance;
    TargetHeadingRaw = targetHeadingRaw;
    HeadingPaddingBytesRaw = headingPaddingBytesRaw;
    ConeHalfAngleTangent = coneHalfAngleTangent;
    HalfFalloffAngleDistanceProduct = halfFalloffAngleDistanceProduct;
    VerticalTargetSlope = verticalTargetSlope;
  }

  public Float32Bits ApproximateTargetDistance { get; }
  public byte TargetHeadingRaw { get; }
  public ImmutableArray<byte> HeadingPaddingBytesRaw { get; }
  public Float32Bits ConeHalfAngleTangent { get; }
  public Float32Bits HalfFalloffAngleDistanceProduct { get; }
  public Float32Bits VerticalTargetSlope { get; }
  public float TargetHeadingRadians => TargetHeadingRaw * MathF.Tau / 256f;
}

public sealed record OmniLightRecord(
  StoredVector3 PositionValue,
  StoredVector3 ColorRgbValue,
  Float32Bits TerrainLightAmplitudeValue)
  : StaticLightRecord(PositionValue, ColorRgbValue, TerrainLightAmplitudeValue);

public sealed record StaticRenderObjectFlags(uint Raw)
{
  public byte HierarchyUnwindCount => (byte)Raw;
  public bool ViewerFaced => (Raw & 0x00000100) != 0;
  public bool Barrel => (Raw & 0x00000200) != 0;
  public bool Rotor => (Raw & 0x00000400) != 0;
  public bool BeginsNestedSourceObject => (Raw & 0x00000800) != 0;
  public ushort UnclassifiedObjectFlagsHighWordRaw => (ushort)(Raw >> 16);
}

public sealed record TextureCoordinate(
  Float32Bits U,
  Float32Bits StoredV,
  Float32Bits ReservedTextureComponentRaw)
{
  public float V => -StoredV.Value;
}

public sealed record StaticVertex(
  StoredVector3 StoredPosition,
  StoredVector3 StoredNormal,
  TextureCoordinate TextureCoordinate,
  ushort SharedNormalVertexIndexRaw,
  ushort SharedPositionVertexIndexRaw)
{
  public Vector3 Position => new(StoredPosition.X.Value, -StoredPosition.Y.Value, StoredPosition.Z.Value);
  public Vector3 Normal => new(StoredNormal.X.Value, -StoredNormal.Y.Value, StoredNormal.Z.Value);
}

public sealed record Triangle(ushort A, ushort B, ushort C, ushort RenderPassFlagsRaw)
{
  public bool UsesNormalPass => (RenderPassFlagsRaw & 1) != 0;
  public bool UsesSnowPass => (RenderPassFlagsRaw & 2) != 0;
}

public sealed record AnimationClassValue(int Raw)
{
  public AnimationClass? Known => Enum.IsDefined(typeof(AnimationClass), Raw) ? (AnimationClass)Raw : null;
}

public sealed record AnimationTracks(
  ImmutableArray<StoredVector3> ScaleTrack,
  ImmutableArray<StoredVector3> TranslationTrack,
  ImmutableArray<StoredMatrix4x4> MatrixTrack);

public sealed record StoredMatrix4x4(
  Float32Bits M11,
  Float32Bits M12,
  Float32Bits M13,
  Float32Bits M14,
  Float32Bits M21,
  Float32Bits M22,
  Float32Bits M23,
  Float32Bits M24,
  Float32Bits M31,
  Float32Bits M32,
  Float32Bits M33,
  Float32Bits M34,
  Float32Bits M41,
  Float32Bits M42,
  Float32Bits M43,
  Float32Bits M44);

public sealed record StaticRenderObject(
  StaticRenderObjectId Id,
  int Ordinal,
  StaticRenderObjectFlags Flags,
  ImmutableArray<StaticVertex> Vertices,
  ImmutableArray<byte> VertexBlockPadding,
  string TexturePath,
  ImmutableArray<Triangle> Triangles,
  AnimationClassValue AnimationClass,
  AnimationTracks AnimationTracks,
  StoredVector3 PivotPosition,
  byte BarrelMaximumRaiseAngleRaw,
  uint NextRecordMarkerRaw);

public sealed record SourceObjectNode(
  SourceObjectId Id,
  ImmutableArray<StaticRenderObjectId> RenderObjects,
  ImmutableArray<SourceObjectNode> Children);

public sealed record SourceObjectTree(SourceObjectNode Root);

public sealed record DynamicEffectType(int Raw)
{
  public DynamicEffectKind? Known => Enum.IsDefined(typeof(DynamicEffectKind), Raw)
    ? (DynamicEffectKind)Raw
    : null;
}

public sealed record TerrainLightType(int Raw)
{
  public TerrainLightKind? Known => Enum.IsDefined(typeof(TerrainLightKind), Raw)
    ? (TerrainLightKind)Raw
    : null;
}

public sealed record EffectRectangle(
  Float32Bits Left,
  Float32Bits Top,
  Float32Bits Right,
  Float32Bits Bottom);

public sealed record DynamicExtension(
  DynamicEffectType EffectType,
  TerrainLightType TerrainLightType,
  int FirstFrame,
  int FrameCount,
  int SpriteSheetColumnCount,
  int SpriteSheetRowCount,
  int FramePeriodTicks,
  Float32Bits TextureUStep,
  Float32Bits TextureVStep,
  EffectRectangle StartEffectRectangle,
  EffectRectangle EndEffectRectangle,
  Float32Bits EffectZOrCameraDepthOffset,
  Float32Bits RibbonHalfWidth,
  int ReservedZeroRaw,
  int AdditiveFlagRaw,
  StoredVector3 TerrainLightRgb,
  StoredVector3 ColorRgb,
  Float32Bits VisibleTerrainLightGain,
  int AlphaTimingModeRaw,
  Float32Bits StartAlpha,
  Float32Bits EndAlpha,
  Float32Bits StartScale,
  Float32Bits EndScale,
  StoredVector3 ChildStartTranslation,
  StoredVector3 ChildEndTranslation,
  string MeshName,
  string TexturePath)
{
  public bool UsesAdditiveBlending => AdditiveFlagRaw != 0;
  public bool UsesLifetimeProgressAlpha => AlphaTimingModeRaw != 0;
}

public sealed record DynamicObject(
  BaseHeader BaseHeader,
  DynamicExtension Extension,
  ImmutableArray<DynamicObject> Children);

public abstract record MeshAsset
{
  private protected MeshAsset(
    MeshAssetLineageId lineageId,
    ArchiveFraming archiveFraming,
    ImmutableArray<byte> rootTrailingData,
    MeshAssetOrigin origin)
  {
    LineageId = lineageId;
    ArchiveFraming = archiveFraming;
    RootTrailingData = rootTrailingData;
    Origin = origin;
  }

  public MeshAssetLineageId LineageId { get; }
  public ArchiveFraming ArchiveFraming { get; }
  public ImmutableArray<byte> RootTrailingData { get; }
  public MeshAssetOrigin Origin { get; }
  public abstract MeshKind Kind { get; }

  public abstract TResult Match<TResult>(
    Func<StaticMeshAsset, TResult> onStatic,
    Func<DynamicMeshAsset, TResult> onDynamic);

  public abstract void Match(Action<StaticMeshAsset> onStatic, Action<DynamicMeshAsset> onDynamic);
}

public sealed record StaticMeshAsset : MeshAsset
{
  internal StaticMeshAsset(
    MeshAssetLineageId lineageId,
    ArchiveFraming archiveFraming,
    BaseHeader baseHeader,
    ImmutableArray<StaticRenderObject> staticRenderObjects,
    SourceObjectTree sourceObjectTree,
    uint storedTrailingHierarchyUnwindCount,
    uint expectedTrailingHierarchyUnwindCount,
    ImmutableArray<byte> rootTrailingData,
    MeshAssetOrigin origin)
    : base(lineageId, archiveFraming, rootTrailingData, origin)
  {
    BaseHeader = baseHeader;
    StaticRenderObjects = staticRenderObjects;
    SourceObjectTree = sourceObjectTree;
    StoredTrailingHierarchyUnwindCount = storedTrailingHierarchyUnwindCount;
    ExpectedTrailingHierarchyUnwindCount = expectedTrailingHierarchyUnwindCount;
  }

  public override MeshKind Kind => MeshKind.Static;
  public BaseHeader BaseHeader { get; }
  public ImmutableArray<StaticRenderObject> StaticRenderObjects { get; }
  public SourceObjectTree SourceObjectTree { get; }
  public uint StoredTrailingHierarchyUnwindCount { get; }
  public uint ExpectedTrailingHierarchyUnwindCount { get; }

  public StaticRenderObject GetRenderObject(StaticRenderObjectId id)
    => StaticRenderObjects.Single(x => x.Id == id);

  public StaticMeshEditSession Edit() => new(this);

  public override TResult Match<TResult>(
    Func<StaticMeshAsset, TResult> onStatic,
    Func<DynamicMeshAsset, TResult> onDynamic)
    => onStatic(this);

  public override void Match(Action<StaticMeshAsset> onStatic, Action<DynamicMeshAsset> onDynamic)
    => onStatic(this);
}

public sealed record DynamicMeshAsset : MeshAsset
{
  internal DynamicMeshAsset(
    MeshAssetLineageId lineageId,
    ArchiveFraming archiveFraming,
    DynamicObject rootDynamicObject,
    ImmutableArray<byte> rootTrailingData,
    MeshAssetOrigin origin)
    : base(lineageId, archiveFraming, rootTrailingData, origin)
  {
    RootDynamicObject = rootDynamicObject;
  }

  public override MeshKind Kind => MeshKind.Dynamic;
  public DynamicObject RootDynamicObject { get; }

  public override TResult Match<TResult>(
    Func<StaticMeshAsset, TResult> onStatic,
    Func<DynamicMeshAsset, TResult> onDynamic)
    => onDynamic(this);

  public override void Match(Action<StaticMeshAsset> onStatic, Action<DynamicMeshAsset> onDynamic)
    => onDynamic(this);
}

public sealed record MshSafetyProfile(
  long MaximumInputBytes,
  long MaximumOutputBytes,
  int MaximumStringBytes,
  int MaximumStaticRenderObjects,
  int MaximumDynamicDepth)
{
  public static MshSafetyProfile Default { get; } = new(
    256L * 1024 * 1024,
    256L * 1024 * 1024,
    4 * 1024,
    65_536,
    128);
}

public sealed record MshReadOptions(MshSafetyProfile SafetyProfile)
{
  public static MshReadOptions Default { get; } = new(MshSafetyProfile.Default);
}

public sealed record MshWriteOptions(MshSafetyProfile SafetyProfile)
{
  public static MshWriteOptions Default { get; } = new(MshSafetyProfile.Default);
}

public sealed class MshReadResult
{
  private readonly MeshAsset? _asset;

  internal MshReadResult(bool isSuccess, MeshAsset? asset, ImmutableArray<MshDiagnostic> diagnostics)
  {
    IsSuccess = isSuccess;
    _asset = asset;
    Diagnostics = diagnostics;
  }

  public bool IsSuccess { get; }
  public ImmutableArray<MshDiagnostic> Diagnostics { get; }

  public bool TryGetAsset([NotNullWhen(true)] out MeshAsset? asset)
  {
    asset = _asset;
    return IsSuccess;
  }
}

public sealed class MshWriteResult
{
  internal MshWriteResult(bool isSuccess, long bytesWritten, ImmutableArray<MshDiagnostic> diagnostics)
  {
    IsSuccess = isSuccess;
    BytesWritten = bytesWritten;
    Diagnostics = diagnostics;
  }

  public bool IsSuccess { get; }
  public long BytesWritten { get; }
  public ImmutableArray<MshDiagnostic> Diagnostics { get; }
}

public sealed record MshValidationResult(ImmutableArray<MshDiagnostic> Diagnostics)
{
  public bool IsValid => Diagnostics.All(x => x.Severity != MshDiagnosticSeverity.Error);
}

public sealed class MshBuildResult<T>
  where T : class
{
  private readonly T? _value;

  internal MshBuildResult(bool isSuccess, T? value, ImmutableArray<MshDiagnostic> diagnostics)
  {
    IsSuccess = isSuccess;
    _value = value;
    Diagnostics = diagnostics;
  }

  public bool IsSuccess { get; }
  public ImmutableArray<MshDiagnostic> Diagnostics { get; }

  public bool TryGetValue([NotNullWhen(true)] out T? value)
  {
    value = _value;
    return IsSuccess;
  }
}

public sealed class MshReader
{
  private readonly ILogger<MshReader> _logger;

  public MshReader(ILogger<MshReader> logger)
  {
    _logger = logger;
  }

  public ValueTask<MshReadResult> ReadAsync(
    Stream source,
    MshReadOptions? options = null,
    CancellationToken cancellationToken = default)
    => throw new NotImplementedException("Prototype only");

  public ValueTask<MshReadResult> ReadAsync(
    string path,
    MshReadOptions? options = null,
    CancellationToken cancellationToken = default)
    => throw new NotImplementedException("Prototype only");
}

public sealed class MshWriter
{
  private readonly ILogger<MshWriter> _logger;

  public MshWriter(ILogger<MshWriter> logger)
  {
    _logger = logger;
  }

  public ValueTask<MshWriteResult> WriteAsync(
    Stream destination,
    MeshAsset asset,
    MshWriteOptions? options = null,
    CancellationToken cancellationToken = default)
    => throw new NotImplementedException("Prototype only");

  public ValueTask<MshWriteResult> WriteAsync(
    string path,
    MeshAsset asset,
    MshWriteOptions? options = null,
    CancellationToken cancellationToken = default)
    => throw new NotImplementedException("Prototype only");
}

public sealed class MshValidator
{
  public MshValidationResult Validate(MeshAsset asset, MshSafetyProfile? safetyProfile = null)
    => throw new NotImplementedException("Prototype only");
}

public sealed record CanonicalStaticVertex(Vector3 Position, Vector3 Normal, Vector2 TextureCoordinate, int? PositionIdentity, int? NormalIdentity);

public sealed record CanonicalTriangle(ushort A, ushort B, ushort C);

public abstract record SourceObjectContent;

public sealed record RenderPartitionDraft(
  string TexturePath,
  ImmutableArray<CanonicalStaticVertex> Vertices,
  ImmutableArray<CanonicalTriangle> Triangles) : SourceObjectContent;

public sealed record ChildSourceObjectDraft(SourceObjectDraft Child) : SourceObjectContent;

public sealed record SourceObjectDraft(ImmutableArray<SourceObjectContent> OrderedContent);

public sealed class StaticMeshBuilder
{
  public static StaticMeshBuilder Create() => new();

  public StaticMeshBuilder SetAnimationLengths(byte a, byte b, byte c, byte d) => this;

  public StaticMeshBuilder SetRootSourceObject(SourceObjectDraft root) => this;

  public StaticMeshBuilder SetFootprint(ushort presenceMask, ImmutableArray<float> topElevations, ImmutableArray<byte> cornerPassageFlags)
    => this;

  public MshBuildResult<StaticMeshAsset> Build(MshSafetyProfile? safetyProfile = null)
    => throw new NotImplementedException("Prototype only");
}

public sealed record CanonicalDynamicObject(
  DynamicEffectKind Effect,
  ImmutableArray<CanonicalDynamicObject> Children,
  string MeshName,
  string TexturePath,
  float StartScale,
  float EndScale);

public static class DynamicObjectRecipes
{
  public static CanonicalDynamicObject Group(params CanonicalDynamicObject[] children)
    => new(DynamicEffectKind.Group, children.ToImmutableArray(), string.Empty, string.Empty, 0, 0);

  public static CanonicalDynamicObject ScalableObject(string meshName, float startScale, float endScale)
    => new(DynamicEffectKind.ScalableObject, ImmutableArray<CanonicalDynamicObject>.Empty, meshName, string.Empty, startScale, endScale);
}

public sealed class DynamicMeshBuilder
{
  public static DynamicMeshBuilder Create(CanonicalDynamicObject root) => new();

  public MshBuildResult<DynamicMeshAsset> Build(MshSafetyProfile? safetyProfile = null)
    => throw new NotImplementedException("Prototype only");
}

public sealed record PreservationChange(string FieldPath, PreservationDisposition Disposition, string Reason);

public sealed record PreservationReport(ImmutableArray<PreservationChange> Changes);

public sealed class MshEditResult<T>
  where T : class
{
  private readonly T? _value;

  internal MshEditResult(
    bool isSuccess,
    T? value,
    PreservationReport preservation,
    ImmutableArray<MshDiagnostic> diagnostics)
  {
    IsSuccess = isSuccess;
    _value = value;
    Preservation = preservation;
    Diagnostics = diagnostics;
  }

  public bool IsSuccess { get; }
  public PreservationReport Preservation { get; }
  public ImmutableArray<MshDiagnostic> Diagnostics { get; }

  public bool TryGetValue([NotNullWhen(true)] out T? value)
  {
    value = _value;
    return IsSuccess;
  }
}

public sealed class StaticMeshEditSession
{
  private readonly StaticMeshAsset _source;

  internal StaticMeshEditSession(StaticMeshAsset source)
  {
    _source = source;
  }

  public StaticMeshEditSession ReplaceGeometry(
    StaticRenderObjectId renderObject,
    ImmutableArray<CanonicalStaticVertex> vertices,
    ImmutableArray<CanonicalTriangle> triangles)
    => this;

  public StaticMeshEditSession ReplaceTexturePath(StaticRenderObjectId renderObject, string texturePath)
    => this;

  public MshEditResult<StaticMeshAsset> Commit(MshSafetyProfile? safetyProfile = null)
    => new(
      true,
      _source,
      new PreservationReport(ImmutableArray<PreservationChange>.Empty),
      ImmutableArray<MshDiagnostic>.Empty);
}
