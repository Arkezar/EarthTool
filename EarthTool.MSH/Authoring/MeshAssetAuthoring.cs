#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;

namespace EarthTool.MSH.Authoring
{
  /// <summary>Describes one canonical logical 4x4 footprint.</summary>
  public sealed class CanonicalStaticFootprint
  {
    /// <summary>Gets the low-16 logical occupied-cell mask.</summary>
    public ushort PresenceMask { get; }

    /// <summary>Gets 16 logical unsigned top elevations in world units.</summary>
    public IReadOnlyList<float> TopElevations { get; }

    /// <summary>Gets 16 logical four-bit corner-passage values.</summary>
    public IReadOnlyList<byte> CornerPassageFlags { get; }

    /// <summary>Initializes a canonical semantic footprint.</summary>
    public CanonicalStaticFootprint(
      ushort presenceMask,
      IEnumerable<float> topElevations,
      IEnumerable<byte> cornerPassageFlags
    )
    {
      PresenceMask = presenceMask;
      var elevations = (
        topElevations ?? throw new ArgumentNullException(nameof(topElevations))
      ).ToArray();
      var flags = (
        cornerPassageFlags ?? throw new ArgumentNullException(nameof(cornerPassageFlags))
      ).ToArray();
      if (elevations.Length != 16 || flags.Length != 16)
      {
        throw new ArgumentException("A canonical footprint requires exactly 16 logical cells.");
      }
      if (
        elevations.Any(value =>
          !float.IsFinite(value) || value < 0 || value * 256d > ushort.MaxValue
        )
      )
      {
        throw new ArgumentOutOfRangeException(nameof(topElevations));
      }
      if (flags.Any(value => value > 0x0F))
      {
        throw new ArgumentOutOfRangeException(nameof(cornerPassageFlags));
      }

      TopElevations = Array.AsReadOnly(elevations);
      CornerPassageFlags = Array.AsReadOnly(flags);
    }
  }

  /// <summary>Describes four canonical horizontal extent magnitudes.</summary>
  public sealed class CanonicalHorizontalExtents
  {
    /// <summary>Gets the positive-Y extent.</summary>
    public float PositiveY { get; }

    /// <summary>Gets the negative-Y extent magnitude.</summary>
    public float NegativeY { get; }

    /// <summary>Gets the positive-X extent.</summary>
    public float PositiveX { get; }

    /// <summary>Gets the negative-X extent magnitude.</summary>
    public float NegativeX { get; }

    /// <summary>Initializes canonical semantic horizontal extents.</summary>
    public CanonicalHorizontalExtents(
      float positiveY,
      float negativeY,
      float positiveX,
      float negativeX
    )
    {
      ValidateExtent(positiveY, nameof(positiveY));
      ValidateExtent(negativeY, nameof(negativeY));
      ValidateExtent(positiveX, nameof(positiveX));
      ValidateExtent(negativeX, nameof(negativeX));

      PositiveY = positiveY;
      NegativeY = negativeY;
      PositiveX = positiveX;
      NegativeX = negativeX;
    }

    private static void ValidateExtent(float value, string parameterName)
    {
      if (!float.IsFinite(value) || value < 0 || value * 256d > ushort.MaxValue)
      {
        throw new ArgumentOutOfRangeException(parameterName);
      }
    }
  }

  /// <summary>Describes supported semantic roles for a canonical source object.</summary>
  public sealed class CanonicalStaticObjectRole
  {
    private const StaticRenderObjectFlags AllowedFlags =
      StaticRenderObjectFlags.ViewerFaced
      | StaticRenderObjectFlags.Barrel
      | StaticRenderObjectFlags.Rotor
      | StaticRenderObjectFlags.MarkerAttachment1
      | StaticRenderObjectFlags.MarkerAttachment2
      | StaticRenderObjectFlags.MarkerAttachment3
      | StaticRenderObjectFlags.MarkerAttachment4;

    /// <summary>Gets the recognized semantic role flags.</summary>
    public StaticRenderObjectFlags Flags { get; }

    /// <summary>Gets the canonical barrel maximum raise-angle byte.</summary>
    public byte BarrelMaximumAngle { get; }

    /// <summary>Initializes supported semantic source-object roles.</summary>
    public CanonicalStaticObjectRole(StaticRenderObjectFlags flags, byte barrelMaximumAngle = 0)
    {
      if ((flags & ~AllowedFlags) != 0)
      {
        throw new ArgumentOutOfRangeException(nameof(flags));
      }
      if ((flags & StaticRenderObjectFlags.Barrel) == 0 && barrelMaximumAngle != 0)
      {
        throw new ArgumentOutOfRangeException(nameof(barrelMaximumAngle));
      }
      Flags = flags;
      BarrelMaximumAngle = barrelMaximumAngle;
    }
  }

  /// <summary>Represents one semantic vertex accepted by canonical static authoring.</summary>
  public sealed class CanonicalStaticVertex
  {
    /// <summary>Gets the MSH-space position.</summary>
    public Vector3 Position { get; }

    /// <summary>Gets the MSH-space normal.</summary>
    public Vector3 Normal { get; }

    /// <summary>Gets the native texture coordinate.</summary>
    public Vector2 TextureCoordinate { get; }

    /// <summary>Initializes a canonical static vertex draft.</summary>
    public CanonicalStaticVertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
    {
      Position = position;
      Normal = normal;
      TextureCoordinate = textureCoordinate;
    }
  }

  /// <summary>Represents one triangle accepted by canonical static authoring.</summary>
  public readonly struct CanonicalTriangle : IEquatable<CanonicalTriangle>
  {
    /// <summary>Gets the first vertex index.</summary>
    public ushort Vertex0 { get; }

    /// <summary>Gets the second vertex index.</summary>
    public ushort Vertex1 { get; }

    /// <summary>Gets the third vertex index.</summary>
    public ushort Vertex2 { get; }

    /// <summary>Initializes a canonical triangle draft.</summary>
    public CanonicalTriangle(ushort vertex0, ushort vertex1, ushort vertex2)
    {
      Vertex0 = vertex0;
      Vertex1 = vertex1;
      Vertex2 = vertex2;
    }

    /// <inheritdoc />
    public bool Equals(CanonicalTriangle other)
    {
      return Vertex0 == other.Vertex0 && Vertex1 == other.Vertex1 && Vertex2 == other.Vertex2;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is CanonicalTriangle other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (Vertex0, Vertex1, Vertex2).GetHashCode();
    }
  }

  /// <summary>Describes one canonical static material-partition render object.</summary>
  public sealed class CanonicalStaticRenderObject
  {
    /// <summary>Gets ordered canonical render vertices.</summary>
    public IReadOnlyList<CanonicalStaticVertex> RenderVertices { get; }

    /// <summary>Gets ordered canonical triangles.</summary>
    public IReadOnlyList<CanonicalTriangle> Triangles { get; }

    /// <summary>Gets the optional canonical game-authoritative TEX resource key.</summary>
    public string? TextureResourceKey { get; }

    /// <summary>Initializes one canonical static render-object draft.</summary>
    public CanonicalStaticRenderObject(
      IEnumerable<CanonicalStaticVertex> renderVertices,
      IEnumerable<CanonicalTriangle> triangles,
      string? textureResourceKey = null
    )
    {
      RenderVertices = Array.AsReadOnly(
        (renderVertices ?? throw new ArgumentNullException(nameof(renderVertices))).ToArray()
      );
      Triangles = Array.AsReadOnly(
        (triangles ?? throw new ArgumentNullException(nameof(triangles))).ToArray()
      );
      TextureResourceKey = textureResourceKey;
    }
  }

  /// <summary>Describes one canonical source object and its material partitions.</summary>
  public sealed class CanonicalStaticSourceObject
  {
    /// <summary>Gets this source object's canonical material partitions.</summary>
    public IReadOnlyList<CanonicalStaticRenderObject> RenderObjects { get; }

    /// <summary>Gets ordered canonical child source objects.</summary>
    public IReadOnlyList<CanonicalStaticSourceObject> Children { get; }

    /// <summary>Gets the optional supported source-object role.</summary>
    public CanonicalStaticObjectRole? Role { get; }

    /// <summary>Initializes one canonical source-object draft.</summary>
    public CanonicalStaticSourceObject(
      IEnumerable<CanonicalStaticRenderObject> renderObjects,
      IEnumerable<CanonicalStaticSourceObject>? children = null,
      CanonicalStaticObjectRole? role = null
    )
    {
      RenderObjects = Array.AsReadOnly(
        (renderObjects ?? throw new ArgumentNullException(nameof(renderObjects))).ToArray()
      );
      Children = Array.AsReadOnly(
        (children ?? Array.Empty<CanonicalStaticSourceObject>()).ToArray()
      );
      Role = role;
      if (RenderObjects.Any(item => item is null) || Children.Any(item => item is null))
      {
        throw new ArgumentException("Canonical static collections cannot contain null values.");
      }
    }
  }

  /// <summary>Returns a canonical or expert value without a partial value on expected failure.</summary>
  public sealed class MshBuildResult<T>
    where T : class
  {
    private readonly T? _value;

    /// <summary>Gets whether construction succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets operation-scoped diagnostics.</summary>
    public IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal MshBuildResult(
      bool isSuccess,
      T? value,
      IEnumerable<OperationDiagnostic>? diagnostics = null
    )
    {
      IsSuccess = isSuccess;
      _value = value;
      Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<OperationDiagnostic>()).ToArray());
    }

    /// <summary>Gets the complete value only when construction succeeded.</summary>
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
      value = _value;
      return IsSuccess;
    }
  }

  /// <summary>Builds the currently supported canonical static mesh slice.</summary>
  public sealed class StaticMeshBuilder
  {
    private readonly Guid _creationGuid;
    private readonly MeshAssetLineageId _lineageId;
    private AnimationClassBytes _animationLengths;
    private CanonicalStaticSourceObject? _rootSourceObject;
    private CanonicalStaticFootprint? _footprint;
    private CanonicalHorizontalExtents? _horizontalExtents;

    private StaticMeshBuilder(Guid creationGuid, MeshAssetLineageId lineageId)
    {
      _creationGuid = creationGuid;
      _lineageId = lineageId;
    }

    /// <summary>Creates a builder with stable generated creation and lineage identities.</summary>
    public static StaticMeshBuilder Create()
    {
      return Create(Guid.NewGuid());
    }

    /// <summary>Creates a builder with an explicit creation identity and generated lineage.</summary>
    public static StaticMeshBuilder Create(Guid creationGuid)
    {
      return new StaticMeshBuilder(creationGuid, new MeshAssetLineageId(Guid.NewGuid()));
    }

    /// <summary>Creates a builder with explicit stable identities.</summary>
    public static StaticMeshBuilder Create(Guid creationGuid, MeshAssetLineageId lineageId)
    {
      return new StaticMeshBuilder(creationGuid, lineageId);
    }

    /// <summary>Sets explicit animation declarations for classes A through D.</summary>
    public StaticMeshBuilder SetAnimationLengths(byte a, byte b, byte c, byte d)
    {
      _animationLengths = new AnimationClassBytes(a, b, c, d);
      return this;
    }

    /// <summary>Sets a single root material partition.</summary>
    public StaticMeshBuilder SetRenderObject(
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles
    )
    {
      if (vertices is null)
      {
        throw new ArgumentNullException(nameof(vertices));
      }

      if (triangles is null)
      {
        throw new ArgumentNullException(nameof(triangles));
      }

      _rootSourceObject = new CanonicalStaticSourceObject(
        new[] { new CanonicalStaticRenderObject(vertices, triangles) }
      );
      return this;
    }

    /// <summary>Sets the complete canonical source-object tree.</summary>
    public StaticMeshBuilder SetRootSourceObject(CanonicalStaticSourceObject rootSourceObject)
    {
      _rootSourceObject =
        rootSourceObject ?? throw new ArgumentNullException(nameof(rootSourceObject));
      return this;
    }

    /// <summary>Sets an explicit semantic footprint instead of deriving the historical default.</summary>
    public StaticMeshBuilder SetFootprint(CanonicalStaticFootprint footprint)
    {
      _footprint = footprint ?? throw new ArgumentNullException(nameof(footprint));
      return this;
    }

    /// <summary>Sets explicit semantic horizontal extents instead of deriving them from root geometry.</summary>
    public StaticMeshBuilder SetHorizontalExtents(CanonicalHorizontalExtents horizontalExtents)
    {
      _horizontalExtents =
        horizontalExtents ?? throw new ArgumentNullException(nameof(horizontalExtents));
      return this;
    }

    /// <summary>Builds one immutable canonical snapshot.</summary>
    public MshBuildResult<StaticMeshAsset> Build(MshOperationProfile? profile = null)
    {
      profile ??= MshOperationProfile.Default;
      var failure = AuthoringValidation.ValidateStaticTree(_rootSourceObject, profile);
      if (failure is not null)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { failure });
      }

      failure = AuthoringValidation.ValidateStaticHeader(
        _rootSourceObject!,
        _footprint,
        _horizontalExtents
      );
      if (failure is not null)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { failure });
      }

      try
      {
        var bytes = MshCanonicalSerializer.CreateStatic(
          _creationGuid,
          _animationLengths,
          _rootSourceObject!,
          _footprint,
          _horizontalExtents
        );
        if (bytes.Length > profile.MaxOutputBytes)
        {
          return new MshBuildResult<StaticMeshAsset>(
            false,
            null,
            new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) }
          );
        }

        var decoded = MshV1Decoder.Decode(bytes, profile, CancellationToken.None);
        var decodedAsset = (StaticMeshAsset)decoded.Asset;
        var asset = MeshAssetRebinder.RebindStatic(
          decodedAsset,
          MeshAssetOrigin.Canonical,
          StaticMeshIdentityState.ForLineage(decodedAsset, _lineageId)
        );
        return new MshBuildResult<StaticMeshAsset>(true, asset, decoded.Diagnostics);
      }
      catch (OverflowException)
      {
        return new MshBuildResult<StaticMeshAsset>(
          false,
          null,
          new[]
          {
            AuthoringValidation.Invalid(
              "CommonBaseHeader",
              "A derived fixed-point value is out of range."
            ),
          }
        );
      }
      catch (MshContentException ex)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { ex.Diagnostic });
      }
    }
  }

  /// <summary>Describes one canonical dynamic object before it is accepted into an immutable asset.</summary>
  public sealed class CanonicalDynamicObject
  {
    private readonly List<CanonicalDynamicObject> _children;

    internal CanonicalDynamicRecipe Recipe { get; }

    /// <summary>Gets the recognized effect to author.</summary>
    public DynamicEffectType EffectType { get; }

    /// <summary>Gets the ordered draft children.</summary>
    public IReadOnlyList<CanonicalDynamicObject> Children => _children.AsReadOnly();

    internal CanonicalDynamicObject(
      CanonicalDynamicRecipe recipe,
      IEnumerable<CanonicalDynamicObject>? children
    )
    {
      Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
      EffectType = recipe.EffectType;
      _children = children?.ToList() ?? new List<CanonicalDynamicObject>();
      if (_children.Any(child => child is null))
      {
        throw new ArgumentException(
          "Dynamic child collections cannot contain null values.",
          nameof(children)
        );
      }
    }

    /// <summary>Adds one ordered draft child.</summary>
    public CanonicalDynamicObject AddChild(CanonicalDynamicObject child)
    {
      _children.Add(child ?? throw new ArgumentNullException(nameof(child)));
      return this;
    }

    /// <summary>Authors the translation applied by this object's parent phase.</summary>
    public CanonicalDynamicObject SetChildTranslation(
      Vector3 startTranslation,
      Vector3 endTranslation
    )
    {
      Recipe.ChildStartTranslation = startTranslation;
      Recipe.ChildEndTranslation = endTranslation;
      return this;
    }
  }

  /// <summary>Builds canonical dynamic object trees.</summary>
  public sealed class DynamicMeshBuilder
  {
    private readonly Guid _creationGuid;
    private readonly MeshAssetLineageId _lineageId;
    private CanonicalDynamicObject _root = DynamicEffectRecipes.Group();

    private DynamicMeshBuilder(Guid creationGuid, MeshAssetLineageId lineageId)
    {
      _creationGuid = creationGuid;
      _lineageId = lineageId;
    }

    /// <summary>Creates a builder with stable generated creation and lineage identities.</summary>
    public static DynamicMeshBuilder Create()
    {
      return Create(Guid.NewGuid());
    }

    /// <summary>Creates a builder with an explicit creation identity and generated lineage.</summary>
    public static DynamicMeshBuilder Create(Guid creationGuid)
    {
      return new DynamicMeshBuilder(creationGuid, new MeshAssetLineageId(Guid.NewGuid()));
    }

    /// <summary>Creates a builder with explicit stable identities.</summary>
    public static DynamicMeshBuilder Create(Guid creationGuid, MeshAssetLineageId lineageId)
    {
      return new DynamicMeshBuilder(creationGuid, lineageId);
    }

    /// <summary>Sets the complete canonical root dynamic object.</summary>
    public DynamicMeshBuilder SetRoot(CanonicalDynamicObject root)
    {
      _root = root ?? throw new ArgumentNullException(nameof(root));
      return this;
    }

    /// <summary>Builds one immutable canonical dynamic snapshot.</summary>
    public MshBuildResult<DynamicMeshAsset> Build(MshOperationProfile? profile = null)
    {
      profile ??= MshOperationProfile.Default;
      var failure = AuthoringValidation.ValidateDynamic(
        _root,
        profile,
        out var validationDiagnostics
      );
      if (failure is not null)
      {
        return new MshBuildResult<DynamicMeshAsset>(
          false,
          null,
          validationDiagnostics.Concat(new[] { failure })
        );
      }

      int dynamicLength;
      int outputLength;
      try
      {
        dynamicLength = MshCanonicalSerializer.GetDynamicSerializedLength(_root);
        outputLength = checked(0x18 + dynamicLength);
      }
      catch (OverflowException)
      {
        return new MshBuildResult<DynamicMeshAsset>(
          false,
          null,
          new[] { AuthoringValidation.ResourceLimit(long.MaxValue, profile.MaxOutputBytes) }
        );
      }

      if (outputLength > profile.MaxOutputBytes)
      {
        return new MshBuildResult<DynamicMeshAsset>(
          false,
          null,
          new[] { AuthoringValidation.ResourceLimit(outputLength, profile.MaxOutputBytes) }
        );
      }

      var bytes = MshCanonicalSerializer.CreateDynamic(_creationGuid, _root, dynamicLength);
      var decoded = MshV1Decoder.Decode(bytes, profile, CancellationToken.None);
      var asset = MeshAssetRebinder.RebindDynamic(
        (DynamicMeshAsset)decoded.Asset,
        MeshAssetOrigin.Canonical,
        _lineageId
      );
      return new MshBuildResult<DynamicMeshAsset>(
        true,
        asset,
        validationDiagnostics.Concat(decoded.Diagnostics)
      );
    }
  }

  /// <summary>Describes how one serialized field path was handled by an edit.</summary>
  public enum PreservationDisposition
  {
    /// <summary>The source representation was retained exactly.</summary>
    Retained = 0,

    /// <summary>The representation was regenerated from edited semantic input.</summary>
    Regenerated = 1,

    /// <summary>The source representation was deliberately removed.</summary>
    Invalidated = 2,

    /// <summary>The representation was replaced with its canonical authored form.</summary>
    Canonicalized = 3,
  }

  /// <summary>Describes one preservation effect.</summary>
  public sealed class PreservationChange
  {
    /// <summary>Gets the affected field path.</summary>
    public string FieldPath { get; }

    /// <summary>Gets the preservation disposition.</summary>
    public PreservationDisposition Disposition { get; }

    /// <summary>Gets the stable reason category.</summary>
    public string Reason { get; }

    /// <summary>Initializes one preservation effect.</summary>
    public PreservationChange(string fieldPath, PreservationDisposition disposition, string reason)
    {
      FieldPath = fieldPath ?? throw new ArgumentNullException(nameof(fieldPath));
      Disposition = disposition;
      Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }
  }

  /// <summary>Reports exact field-path effects for one committed edit.</summary>
  public sealed class PreservationReport
  {
    /// <summary>Gets the ordered preservation effects.</summary>
    public IReadOnlyList<PreservationChange> Changes { get; }

    internal PreservationReport(IEnumerable<PreservationChange> changes)
    {
      Changes = Array.AsReadOnly(changes.ToArray());
    }
  }

  /// <summary>Returns an edited snapshot, diagnostics, and preservation effects.</summary>
  public sealed class MshEditResult<T>
    where T : class
  {
    private readonly T? _value;

    /// <summary>Gets whether the edit committed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets operation-scoped diagnostics.</summary>
    public IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    /// <summary>Gets exact preservation effects.</summary>
    public PreservationReport Preservation { get; }

    internal MshEditResult(
      bool isSuccess,
      T? value,
      PreservationReport preservation,
      IEnumerable<OperationDiagnostic>? diagnostics = null
    )
    {
      IsSuccess = isSuccess;
      _value = value;
      Preservation = preservation;
      Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<OperationDiagnostic>()).ToArray());
    }

    /// <summary>Gets the complete edited value only when commit succeeded.</summary>
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
      value = _value;
      return IsSuccess;
    }
  }

  /// <summary>Describes one source-scoped canonical partition addition.</summary>
  internal sealed class StaticRenderObjectAddition
  {
    internal int Ordinal { get; }

    internal int SourceObjectOrdinal { get; }

    internal int LocalId { get; }

    internal int SourceObjectLocalId { get; }

    internal IReadOnlyList<CanonicalStaticVertex> Vertices { get; }

    internal IReadOnlyList<CanonicalTriangle> Triangles { get; }

    internal IReadOnlyList<byte> TexturePathBytes { get; private set; }

    internal StaticRenderObjectAddition(
      int ordinal,
      int sourceObjectOrdinal,
      int localId,
      int sourceObjectLocalId,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles
    )
    {
      Ordinal = ordinal;
      SourceObjectOrdinal = sourceObjectOrdinal;
      LocalId = localId;
      SourceObjectLocalId = sourceObjectLocalId;
      Vertices = vertices;
      Triangles = triangles;
      TexturePathBytes = Array.Empty<byte>();
    }

    internal void SetTexturePathBytes(IEnumerable<byte> texturePathBytes)
    {
      TexturePathBytes = Array.AsReadOnly(texturePathBytes.ToArray());
    }
  }

  internal sealed class StaticSourceObjectAssembly
  {
    internal int Ordinal { get; }

    internal IReadOnlyList<int> StaticRenderObjectOrdinals { get; }

    internal IReadOnlyList<StaticSourceObjectAssembly> Children { get; }

    internal StaticSourceObjectAssembly(
      int ordinal,
      IEnumerable<int> staticRenderObjectOrdinals,
      IEnumerable<StaticSourceObjectAssembly> children
    )
    {
      Ordinal = ordinal;
      StaticRenderObjectOrdinals = Array.AsReadOnly(staticRenderObjectOrdinals.ToArray());
      Children = Array.AsReadOnly(children.ToArray());
    }

    internal static StaticSourceObjectAssembly FromSource(
      StaticSourceObject source,
      IReadOnlyDictionary<StaticSourceObject, int> sourceOrdinals,
      IReadOnlyDictionary<StaticRenderObject, int> renderObjectOrdinals
    )
    {
      return new StaticSourceObjectAssembly(
        sourceOrdinals[source],
        source.StaticRenderObjects.Select(renderObject => renderObjectOrdinals[renderObject]),
        source.Children.Select(child => FromSource(child, sourceOrdinals, renderObjectOrdinals))
      );
    }

  }

  internal sealed class StaticAnimationReplacement
  {
    internal StaticAnimationTracks Tracks { get; }

    internal uint ClassValue { get; }

    internal StaticAnimationReplacement(StaticAnimationTracks tracks, uint classValue)
    {
      Tracks = tracks;
      ClassValue = classValue;
    }
  }

  internal enum StaticLightRecordKind
  {
    Spot,
    Omni,
  }

  internal enum StaticMeshAssemblyChangeKind
  {
    Geometry,
    Pivot,
    TexturePath,
    Animation,
    MarkerFlags,
    Attachment,
    CannonRenderPosition,
    StaticLight,
    AnimationLengths,
    AnimationFrameIndices,
    HorizontalExtents,
    RemovedRenderObject,
    AddedRenderObject,
    Hierarchy,
  }

  internal sealed class StaticMeshAssemblyChange
  {
    internal StaticMeshAssemblyChangeKind Kind { get; }

    internal int? LocalId { get; }

    internal int? PhysicalNumber { get; }

    internal StaticLightRecordKind? LightKind { get; }

    internal IReadOnlyList<string> ChangedFields { get; }

    internal StaticMeshAssemblyChange(
      StaticMeshAssemblyChangeKind kind,
      int? localId = null,
      int? physicalNumber = null,
      StaticLightRecordKind? lightKind = null,
      IEnumerable<string>? changedFields = null
    )
    {
      Kind = kind;
      LocalId = localId;
      PhysicalNumber = physicalNumber;
      LightKind = lightKind;
      ChangedFields = Array.AsReadOnly((changedFields ?? Array.Empty<string>()).ToArray());
    }
  }

  internal sealed class StaticMeshAssemblyTrace
  {
    internal IReadOnlyList<StaticMeshAssemblyChange> Changes { get; }

    internal IReadOnlyList<int> ResultRenderObjectLocalIds { get; }

    internal bool ReplacedSingleRenderObject { get; }

    internal StaticMeshAssemblyTrace(
      IEnumerable<StaticMeshAssemblyChange> changes,
      IEnumerable<int> resultRenderObjectLocalIds,
      bool replacedSingleRenderObject
    )
    {
      Changes = Array.AsReadOnly(changes.ToArray());
      ResultRenderObjectLocalIds = Array.AsReadOnly(resultRenderObjectLocalIds.ToArray());
      ReplacedSingleRenderObject = replacedSingleRenderObject;
    }
  }

  /// <summary>Accumulates one atomic static assembly and emits one immutable snapshot.</summary>
  internal sealed class StaticMeshAssembler
  {
    private readonly StaticMeshAsset? _source;
    private readonly CanonicalStaticSourceObject? _canonicalRoot;
    private readonly Guid? _canonicalCreationGuid;
    private readonly CanonicalStaticFootprint? _canonicalFootprint;
    private readonly CanonicalHorizontalExtents? _canonicalHorizontalExtents;
    private readonly MeshAssetLineageId _lineageId;
    private readonly MeshAssetOrigin _origin;
    private readonly IReadOnlyList<StaticRenderObject> _sourceRenderObjects;
    private readonly IReadOnlyList<StaticSourceObject> _sourceObjects;
    private readonly IReadOnlyList<CanonicalStaticRenderObject> _canonicalRenderObjects;
    private readonly IReadOnlyList<CanonicalStaticSourceObject> _canonicalSourceObjects;
    private readonly Dictionary<StaticRenderObject, int> _sourceRenderObjectOrdinals;
    private readonly Dictionary<StaticSourceObject, int> _sourceObjectOrdinals;
    private readonly Dictionary<CanonicalStaticRenderObject, int> _canonicalRenderObjectOrdinals;
    private readonly IReadOnlyList<int> _renderObjectSourceLocalIds;
    private readonly List<int> _renderObjectOutputLocalIds;
    private readonly List<int> _sourceObjectOutputLocalIds;
    private readonly Dictionary<int, CanonicalTriangle[]> _replacementTriangles = new();
    private readonly Dictionary<int, CanonicalStaticVertex[]> _replacementVertices = new();
    private readonly Dictionary<int, Vector3> _replacementPivots = new();
    private readonly Dictionary<int, byte[]> _replacementTexturePathBytes = new();
    private readonly Dictionary<int, StaticAnimationReplacement> _replacementAnimations = new();
    private readonly Dictionary<int, StaticRenderObjectFlags> _replacementMarkerFlags = new();
    private readonly Dictionary<int, byte[]> _replacementAttachmentRecords = new();
    private readonly Dictionary<int, byte[]> _replacementCannonRenderPositions = new();
    private readonly Dictionary<int, byte[]> _replacementStaticSpotLights = new();
    private readonly Dictionary<int, byte[]> _replacementStaticOmniLights = new();
    private readonly Dictionary<
      (StaticLightRecordKind Kind, int Number),
      HashSet<string>
    > _staticLightFieldChanges = new();
    private readonly HashSet<int> _removedRenderObjects = new();
    private readonly HashSet<int> _allocatedSourceObjects = new();
    private readonly List<StaticRenderObjectAddition> _additions = new();
    private StaticSourceObjectAssembly? _editedRootSourceObject;
    private IReadOnlyList<int>? _editedSequence;
    private int _resultLocalId;
    private CanonicalTriangle[]? _triangles;
    private CanonicalStaticVertex[]? _vertices;
    private bool _committed;
    private bool _invalidSequence;
    private bool _removed;
    private bool _replacementAdded;
    private int? _nextLocalId;
    private int? _nextSourceObjectLocalId;
    private AnimationClassBytes? _replacementAnimationLengths;
    private AnimationClassBytes? _replacementAnimationFrameIndices;
    private CanonicalHorizontalExtents? _replacementHorizontalExtents;

    internal StaticMeshAssembler(StaticMeshAsset source)
    {
      _source = source;
      _lineageId = source.LineageId;
      _origin = source.Origin;
      _sourceRenderObjects = source.StaticRenderObjectSequence;
      _sourceObjects = FlattenSourceObjects(source.RootSourceObject).ToArray();
      _canonicalRenderObjects = Array.Empty<CanonicalStaticRenderObject>();
      _canonicalSourceObjects = Array.Empty<CanonicalStaticSourceObject>();
      _sourceRenderObjectOrdinals = _sourceRenderObjects
        .Select((renderObject, ordinal) => (renderObject, ordinal))
        .ToDictionary(item => item.renderObject, item => item.ordinal);
      _sourceObjectOrdinals = _sourceObjects
        .Select((sourceObject, ordinal) => (sourceObject, ordinal))
        .ToDictionary(item => item.sourceObject, item => item.ordinal);
      _canonicalRenderObjectOrdinals = new Dictionary<CanonicalStaticRenderObject, int>();
      _renderObjectSourceLocalIds = _sourceRenderObjects.Select(item => item.LocalId).ToArray();
      _renderObjectOutputLocalIds = _renderObjectSourceLocalIds.ToList();
      _sourceObjectOutputLocalIds = _sourceObjects.Select(item => item.Id.Value).ToList();
      _resultLocalId = _renderObjectOutputLocalIds[0];
      _nextLocalId = source.NextStaticRenderObjectLocalId;
      _nextSourceObjectLocalId = source.NextSourceObjectLocalId;
    }

    private StaticMeshAssembler(
      Guid creationGuid,
      MeshAssetLineageId lineageId,
      CanonicalStaticSourceObject rootSourceObject,
      CanonicalStaticFootprint footprint,
      CanonicalHorizontalExtents horizontalExtents
    )
    {
      _canonicalRoot = rootSourceObject;
      _canonicalCreationGuid = creationGuid;
      _canonicalFootprint = footprint;
      _canonicalHorizontalExtents = horizontalExtents;
      _lineageId = lineageId;
      _origin = MeshAssetOrigin.Canonical;
      _sourceRenderObjects = Array.Empty<StaticRenderObject>();
      _sourceObjects = Array.Empty<StaticSourceObject>();
      _canonicalSourceObjects = FlattenCanonicalSourceObjects(rootSourceObject).ToArray();
      _canonicalRenderObjects = FlattenCanonicalRenderObjects(rootSourceObject).ToArray();
      _sourceRenderObjectOrdinals = new Dictionary<StaticRenderObject, int>();
      _sourceObjectOrdinals = new Dictionary<StaticSourceObject, int>();
      _canonicalRenderObjectOrdinals = _canonicalRenderObjects
        .Select((renderObject, ordinal) => (renderObject, ordinal))
        .ToDictionary(item => item.renderObject, item => item.ordinal);
      _renderObjectOutputLocalIds = Enumerable
        .Range(1, _canonicalRenderObjects.Count)
        .ToList();
      _renderObjectSourceLocalIds = _renderObjectOutputLocalIds.ToArray();
      _sourceObjectOutputLocalIds = Enumerable.Range(1, _canonicalSourceObjects.Count).ToList();
      _resultLocalId = _renderObjectOutputLocalIds[0];
      _nextLocalId = _renderObjectOutputLocalIds.Count + 1;
      _nextSourceObjectLocalId = _sourceObjectOutputLocalIds.Count + 1;
    }

    internal static StaticMeshAssembler CreateCanonical(
      Guid creationGuid,
      MeshAssetLineageId lineageId,
      CanonicalStaticSourceObject rootSourceObject,
      CanonicalStaticFootprint footprint,
      CanonicalHorizontalExtents horizontalExtents
    )
    {
      return new StaticMeshAssembler(
        creationGuid,
        lineageId,
        rootSourceObject,
        footprint,
        horizontalExtents
      );
    }

    internal int GetRenderObjectOrdinal(StaticRenderObject renderObject)
    {
      return _sourceRenderObjectOrdinals.TryGetValue(renderObject, out var ordinal)
        ? ordinal
        : throw new ArgumentException(
          "The render object does not belong to this assembly.",
          nameof(renderObject)
        );
    }

    internal int GetRenderObjectOrdinal(CanonicalStaticRenderObject renderObject)
    {
      return _canonicalRenderObjectOrdinals.TryGetValue(renderObject, out var ordinal)
        ? ordinal
        : throw new ArgumentException(
          "The canonical render object does not belong to this assembly.",
          nameof(renderObject)
        );
    }

    internal int GetSourceObjectOrdinal(StaticSourceObject sourceObject)
    {
      return _sourceObjectOrdinals.TryGetValue(sourceObject, out var ordinal)
        ? ordinal
        : throw new ArgumentException(
          "The source object does not belong to this assembly.",
          nameof(sourceObject)
        );
    }

    internal int GetRenderObjectOrdinalByOutputLocalId(int localId)
    {
      var ordinal = _renderObjectOutputLocalIds.IndexOf(localId);
      return ordinal >= 0
        ? ordinal
        : throw new ArgumentException(
          "The render-object local ID does not belong to this assembly.",
          nameof(localId)
        );
    }

    internal int GetSourceObjectOrdinalByOutputLocalId(int localId)
    {
      var ordinal = _sourceObjectOutputLocalIds.IndexOf(localId);
      return ordinal >= 0
        ? ordinal
        : throw new ArgumentException(
          "The source-object local ID does not belong to this assembly.",
          nameof(localId)
        );
    }

    internal StaticSourceObjectAssembly CreateSourceObjectAssembly(
      StaticSourceObject sourceObject
    )
    {
      return StaticSourceObjectAssembly.FromSource(
        sourceObject,
        _sourceObjectOrdinals,
        _sourceRenderObjectOrdinals
      );
    }

    internal int GetOutputRenderObjectLocalId(int ordinal)
    {
      return _renderObjectOutputLocalIds[ordinal];
    }

    internal int GetOutputSourceObjectLocalId(int ordinal)
    {
      return _sourceObjectOutputLocalIds[ordinal];
    }

    private int InitialRenderObjectCount => _source is null
      ? _canonicalRenderObjects.Count
      : _sourceRenderObjects.Count;

    internal StaticMeshAssemblyTrace Trace => CreateAssemblyTrace();

    /// <summary>Replaces geometry while retaining the render-object identity.</summary>
    internal void ReplaceGeometry(
      int renderObjectOrdinal,
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles
    )
    {
      EnsureOpen();
      EnsureSourceOrdinal(renderObjectOrdinal);
      if (_removed || _replacementAdded)
      {
        _invalidSequence = true;
      }

      _replacementVertices[renderObjectOrdinal] =
        vertices?.ToArray() ?? throw new ArgumentNullException(nameof(vertices));
      _replacementTriangles[renderObjectOrdinal] =
        triangles?.ToArray() ?? throw new ArgumentNullException(nameof(triangles));
    }

    /// <summary>Removes the current render object before a replacement object is added.</summary>
    internal void RemoveRenderObject(int renderObjectOrdinal)
    {
      EnsureOpen();
      EnsureSourceOrdinal(renderObjectOrdinal);
      if (_replacementAdded || !_removedRenderObjects.Add(renderObjectOrdinal))
      {
        _invalidSequence = true;
      }

      _removed = true;
      _vertices = null;
      _triangles = null;
    }

    /// <summary>Adds a new render object and allocates a fresh lineage-local identity.</summary>
    internal int AddRenderObject(
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles
    )
    {
      EnsureOpen();
      if (!_removed || _replacementAdded)
      {
        _invalidSequence = true;
      }

      _vertices = vertices?.ToArray() ?? throw new ArgumentNullException(nameof(vertices));
      _triangles = triangles?.ToArray() ?? throw new ArgumentNullException(nameof(triangles));
      _resultLocalId = AllocateOutputRenderObjectLocalId();
      _renderObjectOutputLocalIds[0] = _resultLocalId;
      _replacementAdded = true;
      return 0;
    }

    /// <summary>Adds a canonical material partition to an existing source object.</summary>
    internal int AddRenderObject(
      int sourceObjectOrdinal,
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles
    )
    {
      EnsureOpen();
      if (sourceObjectOrdinal < 0 || sourceObjectOrdinal >= _sourceObjectOutputLocalIds.Count)
      {
        throw new ArgumentException(
          "The source-object ordinal does not belong to this assembly.",
          nameof(sourceObjectOrdinal)
        );
      }

      var ordinal = _renderObjectOutputLocalIds.Count;
      var localId = AllocateOutputRenderObjectLocalId();
      _renderObjectOutputLocalIds.Add(localId);
      _additions.Add(
        new StaticRenderObjectAddition(
          ordinal,
          sourceObjectOrdinal,
          localId,
          _sourceObjectOutputLocalIds[sourceObjectOrdinal],
          vertices?.ToArray() ?? throw new ArgumentNullException(nameof(vertices)),
          triangles?.ToArray() ?? throw new ArgumentNullException(nameof(triangles))
        )
      );
      return ordinal;
    }

    internal int AllocateSourceObjectOrdinal()
    {
      EnsureOpen();
      if (!_nextSourceObjectLocalId.HasValue)
      {
        throw new InvalidOperationException(
          "No lineage-local source-object identity remains available."
        );
      }

      var value = _nextSourceObjectLocalId.Value;
      _nextSourceObjectLocalId = value == int.MaxValue ? null : value + 1;
      var ordinal = _sourceObjectOutputLocalIds.Count;
      _sourceObjectOutputLocalIds.Add(value);
      _allocatedSourceObjects.Add(ordinal);
      return ordinal;
    }

    internal void ReplacePivot(int renderObjectOrdinal, Vector3 pivot)
    {
      EnsureOpen();
      if (renderObjectOrdinal < 0 || renderObjectOrdinal >= _renderObjectOutputLocalIds.Count)
      {
        throw new ArgumentException(
          "The render-object identity does not belong to this edit session.",
          nameof(renderObjectOrdinal)
        );
      }
      if (!IsFinite(pivot))
      {
        throw new ArgumentException("The source-object pivot must be finite.", nameof(pivot));
      }

      _replacementPivots[renderObjectOrdinal] = pivot;
    }

    internal void ReplaceTexturePathBytes(
      int renderObjectOrdinal,
      IEnumerable<byte> texturePathBytes
    )
    {
      EnsureOpen();
      var bytes =
        texturePathBytes?.ToArray() ?? throw new ArgumentNullException(nameof(texturePathBytes));
      if (_replacementAdded && renderObjectOrdinal == 0)
      {
        _replacementTexturePathBytes[0] = bytes;
        return;
      }
      if (renderObjectOrdinal >= 0 && renderObjectOrdinal < InitialRenderObjectCount)
      {
        _replacementTexturePathBytes[renderObjectOrdinal] = bytes;
        return;
      }
      var addition =
        _additions.SingleOrDefault(item => item.Ordinal == renderObjectOrdinal)
        ?? throw new ArgumentException(
          "The render-object identity does not belong to this edit session.",
          nameof(renderObjectOrdinal)
        );
      addition.SetTexturePathBytes(bytes);
    }

    internal void ReplaceAnimation(
      int renderObjectOrdinal,
      IEnumerable<Vector3> scaleFrames,
      IEnumerable<Vector3> translationFrames,
      IEnumerable<Matrix4x4> matrices,
      uint animationClassValue
    )
    {
      EnsureOpen();
      EnsureSourceOrdinal(renderObjectOrdinal);
      _replacementAnimations[renderObjectOrdinal] = new StaticAnimationReplacement(
        new StaticAnimationTracks(scaleFrames, translationFrames, matrices),
        animationClassValue
      );
    }

    internal void ReplaceAnimationLengths(AnimationClassBytes animationLengths)
    {
      EnsureOpen();
      _replacementAnimationLengths = animationLengths;
    }

    internal void ReplaceAnimationFrameIndices(
      AnimationClassBytes animationFrameIndices
    )
    {
      EnsureOpen();
      _replacementAnimationFrameIndices = animationFrameIndices;
    }

    internal void ReplaceHorizontalExtents(
      CanonicalHorizontalExtents horizontalExtents
    )
    {
      EnsureOpen();
      _replacementHorizontalExtents =
        horizontalExtents ?? throw new ArgumentNullException(nameof(horizontalExtents));
    }

    internal void ReplaceMarkerAttachmentFlags(
      int renderObjectOrdinal,
      StaticRenderObjectFlags markerFlags
    )
    {
      EnsureOpen();
      if (
        (markerFlags & ~StaticRenderObjectFlagMasks.MarkerAttachments) != 0
        || renderObjectOrdinal < 0
        || renderObjectOrdinal >= _renderObjectOutputLocalIds.Count
      )
      {
        throw new ArgumentOutOfRangeException(nameof(markerFlags));
      }

      _replacementMarkerFlags[renderObjectOrdinal] = markerFlags;
    }

    internal void ReplaceAttachmentRecord(
      int physicalNumber,
      IEnumerable<byte> record
    )
    {
      EnsureOpen();
      if (physicalNumber is < 1 or > 49)
      {
        throw new ArgumentOutOfRangeException(nameof(physicalNumber));
      }
      var bytes = record?.ToArray() ?? throw new ArgumentNullException(nameof(record));
      if (bytes.Length != 8)
      {
        throw new ArgumentException(
          "An attachment record must contain exactly 8 bytes.",
          nameof(record)
        );
      }

      _replacementAttachmentRecords[physicalNumber] = bytes;
    }

    internal void ReplaceCannonRenderPosition(
      int physicalNumber,
      IEnumerable<byte> record
    )
    {
      EnsureOpen();
      if (physicalNumber is < 1 or > 4)
      {
        throw new ArgumentOutOfRangeException(nameof(physicalNumber));
      }
      var bytes = record?.ToArray() ?? throw new ArgumentNullException(nameof(record));
      if (bytes.Length != 12)
      {
        throw new ArgumentException(
          "A cannon render-position record must contain exactly 12 bytes.",
          nameof(record)
        );
      }

      _replacementCannonRenderPositions[physicalNumber] = bytes;
    }

    internal void ReplaceStaticLightRecord(
      StaticLightRecordKind kind,
      int physicalNumber,
      IEnumerable<byte> record,
      IEnumerable<string> changedFields
    )
    {
      EnsureOpen();
      if (physicalNumber is < 1 or > 4)
      {
        throw new ArgumentOutOfRangeException(nameof(physicalNumber));
      }
      var bytes = record?.ToArray() ?? throw new ArgumentNullException(nameof(record));
      var expectedLength = kind == StaticLightRecordKind.Spot ? 0x30 : 0x1C;
      if (bytes.Length != expectedLength)
      {
        throw new ArgumentException(
          $"A static {kind} light record must contain exactly {expectedLength} bytes.",
          nameof(record)
        );
      }

      (
        kind == StaticLightRecordKind.Spot
          ? _replacementStaticSpotLights
          : _replacementStaticOmniLights
      )[physicalNumber] = bytes;
      _staticLightFieldChanges[(kind, physicalNumber)] = new HashSet<string>(
        changedFields ?? throw new ArgumentNullException(nameof(changedFields)),
        StringComparer.Ordinal
      );
    }

    /// <summary>Sets or explicitly clears one game-authoritative TEX resource binding.</summary>
    internal StaticMeshAssembler SetTextureResourceBinding(
      int renderObjectOrdinal,
      string? textureResourceKey
    )
    {
      var bytes = textureResourceKey is null
        ? Array.Empty<byte>()
        : AuthoringValidation.EncodeCanonicalTextureResourceKey(
          textureResourceKey,
          nameof(textureResourceKey)
        );
      ReplaceTexturePathBytes(renderObjectOrdinal, bytes);
      return this;
    }

    internal void ApplyHierarchy(
      StaticSourceObjectAssembly rootSourceObject,
      IReadOnlyList<int> sequence
    )
    {
      EnsureOpen();
      _editedRootSourceObject =
        rootSourceObject ?? throw new ArgumentNullException(nameof(rootSourceObject));
      if (sequence is null)
      {
        throw new ArgumentNullException(nameof(sequence));
      }
      if (sequence.Any(ordinal => ordinal < 0 || ordinal >= _renderObjectOutputLocalIds.Count))
      {
        throw new ArgumentException(
          "The hierarchy sequence contains a render-object ordinal outside this assembly.",
          nameof(sequence)
        );
      }
      _editedSequence = Array.AsReadOnly(sequence.ToArray());
    }

    /// <summary>Commits this session once and returns a new immutable snapshot.</summary>
    public MshEditResult<StaticMeshAsset> Commit(MshOperationProfile? profile = null)
    {
      EnsureOpen();
      _committed = true;
      profile ??= MshOperationProfile.Default;
      if (_source is null)
      {
        return CommitCanonical(profile);
      }

      if (_invalidSequence || _replacementAdded && !_removed)
      {
        return FailedEdit(
          "StaticRenderObjectSequence",
          "The current safe edit slice requires exactly one final render object."
        );
      }

      if (_replacementAdded && _sourceRenderObjects.Count != 1)
      {
        return FailedEdit(
          "StaticRenderObjectSequence",
          "Removing static render objects requires explicit source-hierarchy authoring."
        );
      }

      if (_replacementAdded)
      {
        _replacementVertices[0] = _vertices!;
        _replacementTriangles[0] = _triangles!;
      }

      var finalRootSourceObject = CreateFinalSourceObjectAssembly(
        _editedRootSourceObject
          ?? StaticSourceObjectAssembly.FromSource(
            _source.RootSourceObject,
            _sourceObjectOrdinals,
            _sourceRenderObjectOrdinals
          )
      );

      foreach (var replacement in _replacementVertices)
      {
        var recordIndex = replacement.Key;
        var failure = AuthoringValidation.ValidateStaticForProfile(
          replacement.Value,
          _replacementTriangles[replacement.Key],
          profile,
          $"StaticRenderObjectSequence[{recordIndex}]"
        );
        if (failure is not null)
        {
          return new MshEditResult<StaticMeshAsset>(
            false,
            null,
            new PreservationReport(Array.Empty<PreservationChange>()),
            new[] { failure }
          );
        }
      }

      if (
        _removedRenderObjects.Count == _sourceRenderObjects.Count
        && !_replacementAdded
        && _additions.Count == 0
      )
      {
        return FailedEdit(
          "StaticRenderObjectSequence",
          "At least one static render object must remain."
        );
      }

      foreach (
        var sourceObject in GetSourceAssemblies(finalRootSourceObject)
      )
      {
        if (
          sourceObject.StaticRenderObjectOrdinals.Count == 0
          || sourceObject.StaticRenderObjectOrdinals.All(id => _removedRenderObjects.Contains(id))
            && !_replacementAdded
        )
        {
          return FailedEdit(
            "StaticRenderObjectSequence",
            "A retained source object must contain at least one static render object."
          );
        }
      }
      if (!EditedSequenceMatchesFinalMembership(finalRootSourceObject))
      {
        return FailedEdit(
          "StaticRenderObjectSequence",
          "The authoritative hierarchy sequence must contain each final render-object ordinal exactly once."
        );
      }
      var finalSequence = CreateFinalSequenceForCommit(finalRootSourceObject).ToArray();

      foreach (var addition in _additions)
      {
        var failure = AuthoringValidation.ValidateStaticForProfile(
          addition.Vertices,
          addition.Triangles,
          profile,
          $"StaticRenderObjectSequence[{addition.LocalId}]"
        );
        if (failure is not null)
        {
          return new MshEditResult<StaticMeshAsset>(
            false,
            null,
            new PreservationReport(Array.Empty<PreservationChange>()),
            new[] { failure }
          );
        }
        if (addition.TexturePathBytes.Count > profile.MaxStaticTexturePathBytes)
        {
          return new MshEditResult<StaticMeshAsset>(
            false,
            null,
            new PreservationReport(Array.Empty<PreservationChange>()),
            new[]
            {
              AuthoringValidation.ResourceLimit(
                addition.TexturePathBytes.Count,
                profile.MaxStaticTexturePathBytes
              ),
            }
          );
        }
      }

      if (
        _replacementTexturePathBytes.Values.Any(bytes =>
          bytes.Length > profile.MaxStaticTexturePathBytes
        )
      )
      {
        var actual = _replacementTexturePathBytes.Values.Max(bytes => bytes.Length);
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { AuthoringValidation.ResourceLimit(actual, profile.MaxStaticTexturePathBytes) }
        );
      }

      var bytes =
        _replacementVertices.Count == 0
        && _replacementPivots.Count == 0
        && _replacementTexturePathBytes.Count == 0
        && _replacementAnimations.Count == 0
        && _replacementMarkerFlags.Count == 0
        && _replacementAttachmentRecords.Count == 0
        && _replacementCannonRenderPositions.Count == 0
        && _replacementStaticSpotLights.Count == 0
        && _replacementStaticOmniLights.Count == 0
        && !_replacementAnimationLengths.HasValue
        && !_replacementAnimationFrameIndices.HasValue
        && _replacementHorizontalExtents is null
        && _removedRenderObjects.Count == 0
        && _additions.Count == 0
        && _editedRootSourceObject is null
          ? _source.GetSerializedRepresentation()
          : MshCanonicalSerializer.RewriteStatic(
            _source,
            _replacementVertices.ToDictionary(
              item => GetSerializationRenderObjectLocalId(item.Key),
              item => (IReadOnlyList<CanonicalStaticVertex>)item.Value
            ),
            _replacementTriangles.ToDictionary(
              item => GetSerializationRenderObjectLocalId(item.Key),
              item => (IReadOnlyList<CanonicalTriangle>)item.Value
            ),
            _replacementAdded
              ? Array.Empty<int>()
              : _removedRenderObjects.Select(GetSerializationRenderObjectLocalId),
            _additions,
            CreateSerializationPlan(finalRootSourceObject),
            finalSequence.Select(GetSerializationRenderObjectLocalId).ToArray(),
            ToSerializationIdentityDictionary(_replacementPivots),
            ToSerializationIdentityDictionary(_replacementTexturePathBytes),
            _editedRootSourceObject is not null,
            ToSerializationIdentityDictionary(_replacementMarkerFlags),
            ToSerializationIdentityDictionary(_replacementAnimations),
            _replacementAnimationLengths,
            _replacementAnimationFrameIndices,
            _replacementAttachmentRecords,
            _replacementCannonRenderPositions,
            _replacementStaticSpotLights,
            _replacementStaticOmniLights,
            _replacementHorizontalExtents
          );
      if (bytes.Length > profile.MaxOutputBytes)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) }
        );
      }

      var renderObjectLocalIds = GetResultRenderObjectLocalIds().ToArray();
      if (_replacementAdded)
      {
        renderObjectLocalIds[0] = _resultLocalId;
      }

      MshDecodeResult decoded;
      try
      {
        decoded = MshV1Decoder.Decode(bytes, profile, CancellationToken.None);
      }
      catch (MshContentException ex)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { ex.Diagnostic }
        );
      }

      var decodedAsset = (StaticMeshAsset)decoded.Asset;
      var edited = MeshAssetRebinder.RebindStatic(
        decodedAsset,
          _origin,
          StaticMeshIdentityState.FromLocalIds(
          _lineageId,
          renderObjectLocalIds,
          GetSourceObjectLocalIds(
            finalRootSourceObject
          ),
          _nextLocalId,
          _nextSourceObjectLocalId
        )
      );
      return new MshEditResult<StaticMeshAsset>(
        true,
        edited,
        CreatePreservationReport(edited),
        decoded.Diagnostics
      );
    }

    private MshEditResult<StaticMeshAsset> CommitCanonical(MshOperationProfile profile)
    {
      var treeFailure = AuthoringValidation.ValidateStaticTree(_canonicalRoot, profile);
      if (treeFailure is not null)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { treeFailure }
        );
      }
      var headerFailure = AuthoringValidation.ValidateStaticHeader(
        _canonicalRoot!,
        _canonicalFootprint,
        _canonicalHorizontalExtents
      );
      if (headerFailure is not null)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { headerFailure }
        );
      }
      if (
        _replacementVertices.Count != 0
        || _removedRenderObjects.Count != 0
        || _additions.Count != 0
        || _editedRootSourceObject is not null
        || _replacementTexturePathBytes.Count != 0
        || _replacementMarkerFlags.Count != 0
      )
      {
        return FailedEdit(
          "StaticRenderObjectSequence",
          "Canonical assembly accepts final source geometry and representation overrides only."
        );
      }

      var bytes = MshCanonicalSerializer.CreateStatic(
        _canonicalCreationGuid!.Value,
        _replacementAnimationLengths ?? default,
        _canonicalRoot!,
        _canonicalFootprint,
        _replacementHorizontalExtents ?? _canonicalHorizontalExtents,
        _replacementPivots,
        _replacementAnimations,
        _replacementAnimationFrameIndices,
        _replacementAttachmentRecords,
        _replacementCannonRenderPositions,
        _replacementStaticSpotLights,
        _replacementStaticOmniLights
      );
      if (bytes.Length > profile.MaxOutputBytes)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) }
        );
      }

      MshDecodeResult decoded;
      try
      {
        decoded = MshV1Decoder.Decode(bytes, profile, CancellationToken.None);
      }
      catch (MshContentException ex)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { ex.Diagnostic }
        );
      }
      var authored = MeshAssetRebinder.RebindStatic(
        (StaticMeshAsset)decoded.Asset,
        _origin,
        StaticMeshIdentityState.FromLocalIds(
          _lineageId,
          _renderObjectOutputLocalIds,
          _sourceObjectOutputLocalIds,
          _nextLocalId,
          _nextSourceObjectLocalId
        )
      );
      return new MshEditResult<StaticMeshAsset>(
        true,
        authored,
        new PreservationReport(Array.Empty<PreservationChange>()),
        decoded.Diagnostics
      );
    }

    private PreservationReport CreatePreservationReport(StaticMeshAsset edited)
    {
      var changes = new List<PreservationChange>
      {
        Change("ArchiveFraming", PreservationDisposition.Retained, "Unedited"),
        Change("CommonBaseHeader", PreservationDisposition.Retained, "IndependentRepresentation"),
        Change("RootSourceObjectId", PreservationDisposition.Retained, "RetainedSourceObject"),
      };
      if (_replacementAnimationLengths.HasValue)
      {
        changes.Add(
          Change(
            "CommonBaseHeader.AnimationLengths",
            PreservationDisposition.Regenerated,
            "AnimationEdit"
          )
        );
      }
      if (_replacementAnimationFrameIndices.HasValue)
      {
        changes.Add(
          Change(
            "CommonBaseHeader.AnimationFrameIndices",
            PreservationDisposition.Regenerated,
            "AnimationEdit"
          )
        );
      }
      if (_replacementHorizontalExtents is not null)
      {
        changes.Add(
          Change(
            "CommonBaseHeader.HorizontalExtents",
            PreservationDisposition.Regenerated,
            "EffectivePositionEdit"
          )
        );
      }
      foreach (var physicalNumber in _replacementAttachmentRecords.Keys.OrderBy(value => value))
      {
        var sourceRecord = _source!
          .CommonBaseHeader.AttachmentTable.Skip((physicalNumber - 1) * 8)
          .Take(8)
          .ToArray();
        var replacement = _replacementAttachmentRecords[physicalNumber];
        var sourceActive = BinaryPrimitives.ReadInt16LittleEndian(sourceRecord) != short.MinValue;
        var replacementActive =
          BinaryPrimitives.ReadInt16LittleEndian(replacement) != short.MinValue;
        changes.Add(
          Change(
            $"CommonBaseHeader.AttachmentTable[{physicalNumber}]",
            sourceActive == replacementActive
              ? PreservationDisposition.Regenerated
              : PreservationDisposition.Canonicalized,
            sourceActive == replacementActive ? "AttachmentEdit"
              : replacementActive ? "AttachmentAddition"
              : "AttachmentDeletion"
          )
        );
      }
      foreach (var physicalNumber in _replacementCannonRenderPositions.Keys.OrderBy(value => value))
      {
        changes.Add(
          Change(
            $"CommonBaseHeader.CannonRenderPositions[{physicalNumber}]",
            PreservationDisposition.Regenerated,
            "CannonRenderPositionEdit"
          )
        );
      }
      foreach (
        var replacement in _staticLightFieldChanges
          .OrderBy(item => item.Key.Kind)
          .ThenBy(item => item.Key.Number)
      )
      {
        var collection =
          replacement.Key.Kind == StaticLightRecordKind.Spot
            ? "StaticSpotLights"
            : "StaticOmniLights";
        foreach (var field in replacement.Value.OrderBy(value => value, StringComparer.Ordinal))
        {
          changes.Add(
            Change(
              $"CommonBaseHeader.{collection}[{replacement.Key.Number}].{field}",
              PreservationDisposition.Regenerated,
              "StaticLightEdit"
            )
          );
        }
      }
      if (_editedRootSourceObject is not null)
      {
        changes.Add(
          Change("StaticRenderObjectSequence", PreservationDisposition.Regenerated, "HierarchyEdit")
        );
        changes.Add(
          Change("RootSourceObject", PreservationDisposition.Regenerated, "HierarchyEdit")
        );
      }
      foreach (var replacement in _replacementMarkerFlags)
      {
        var index = GetResultRenderObjectLocalIds()
          .ToList()
          .IndexOf(_renderObjectOutputLocalIds[replacement.Key]);
        if (index >= 0)
        {
          changes.Add(
            Change(
              $"StaticRenderObjectSequence[{index}].ObjectFlags",
              PreservationDisposition.Regenerated,
              "EmitterMarkerOwnership"
            )
          );
        }
      }
      if (_replacementAdded)
      {
        changes.Add(
          Change(
            "StaticRenderObjectSequence[0]",
            PreservationDisposition.Invalidated,
            "RemovedRenderObject"
          )
        );
        changes.Add(
          Change(
            "StaticRenderObjectSequence[0]",
            PreservationDisposition.Canonicalized,
            "NewRenderObject"
          )
        );
        changes.Add(
          Change(
            "StaticRenderObjectSequence[0].TexturePathBytes",
            PreservationDisposition.Canonicalized,
            "NewMaterialBinding"
          )
        );
      }
      else
      {
        for (var index = 0; index < _source!.StaticRenderObjectSequence.Count; index++)
        {
          var record = _source.StaticRenderObjectSequence[index];
          if (_removedRenderObjects.Contains(index))
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}]",
                PreservationDisposition.Invalidated,
                "RemovedRenderObject"
              )
            );
            continue;
          }
          changes.Add(
            Change(
              $"StaticRenderObjectSequence[{index}].Id",
              PreservationDisposition.Retained,
              "RetainedRenderObject"
            )
          );
          if (_replacementVertices.ContainsKey(index))
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].RenderVertices",
                PreservationDisposition.Regenerated,
                "GeometryEdit"
              )
            );
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].Triangles",
                PreservationDisposition.Regenerated,
                "GeometryEdit"
              )
            );
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].VertexBlockPadding",
                PreservationDisposition.Canonicalized,
                "GeometryPacking"
              )
            );
          }
          if (_replacementPivots.ContainsKey(index))
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].Pivot",
                PreservationDisposition.Regenerated,
                "TransformEdit"
              )
            );
          }
          if (_replacementTexturePathBytes.ContainsKey(index))
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].TexturePathBytes",
                PreservationDisposition.Regenerated,
                "MaterialBindingEdit"
              )
            );
          }
          else
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].TexturePathBytes",
                PreservationDisposition.Retained,
                "MaterialBindingReaffirmed"
              )
            );
          }
          if (_replacementAnimations.ContainsKey(index))
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].AnimationTracks.ScaleFrames",
                PreservationDisposition.Regenerated,
                "AnimationEdit"
              )
            );
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].AnimationTracks.TranslationFrames",
                PreservationDisposition.Regenerated,
                "AnimationEdit"
              )
            );
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{index}].AnimationTracks.Matrices",
                PreservationDisposition.Regenerated,
                "AnimationEdit"
              )
            );
          }
        }

        foreach (var addition in _additions)
        {
          var resultIndex = GetResultRenderObjectLocalIds().ToList().IndexOf(addition.LocalId);
          changes.Add(
            Change(
              $"StaticRenderObjectSequence[{resultIndex}]",
              PreservationDisposition.Canonicalized,
              "NewRenderObject"
            )
          );
          changes.Add(
            Change(
              $"StaticRenderObjectSequence[{resultIndex}].TexturePathBytes",
              PreservationDisposition.Canonicalized,
              "NewMaterialBinding"
            )
          );
        }

        for (
          var resultIndex = 0;
          resultIndex < edited.StaticRenderObjectSequence.Count;
          resultIndex++
        )
        {
          var resultRecord = edited.StaticRenderObjectSequence[resultIndex];
          var sourceRecord = _source.StaticRenderObjectSequence.FirstOrDefault(record =>
            record.Id.Equals(resultRecord.Id)
          );
          if (sourceRecord is not null && sourceRecord.ObjectFlags != resultRecord.ObjectFlags)
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{resultIndex}].ObjectFlags",
                PreservationDisposition.Regenerated,
                "SequenceEdit"
              )
            );
          }
          if (
            sourceRecord is not null
            && sourceRecord.NextRecordMarker != resultRecord.NextRecordMarker
          )
          {
            changes.Add(
              Change(
                $"StaticRenderObjectSequence[{resultIndex}].NextRecordMarker",
                PreservationDisposition.Regenerated,
                "SequenceEdit"
              )
            );
          }
          if (
            sourceRecord is not null
            && !_replacementVertices.ContainsKey(_sourceRenderObjectOrdinals[sourceRecord])
          )
          {
            for (
              var vertexIndex = 0;
              vertexIndex < sourceRecord.RenderVertices.Count;
              vertexIndex++
            )
            {
              AddSharingChange(
                changes,
                resultIndex,
                vertexIndex,
                "NormalSharingIndex",
                sourceRecord.RenderVertices[vertexIndex].NormalSharingIndex,
                resultRecord.RenderVertices[vertexIndex].NormalSharingIndex
              );
              AddSharingChange(
                changes,
                resultIndex,
                vertexIndex,
                "PositionSharingIndex",
                sourceRecord.RenderVertices[vertexIndex].PositionSharingIndex,
                resultRecord.RenderVertices[vertexIndex].PositionSharingIndex
              );
            }
          }
        }
        if (_source.StoredTrailingHierarchyUnwindCount != edited.StoredTrailingHierarchyUnwindCount)
        {
          changes.Add(
            Change(
              "StoredTrailingHierarchyUnwindCount",
              PreservationDisposition.Regenerated,
              "SequenceEdit"
            )
          );
        }
      }

      changes.Add(
        Change("RootTrailingBytes", PreservationDisposition.Retained, "IndependentRepresentation")
      );
      return new PreservationReport(changes);
    }

    private static void AddSharingChange(
      ICollection<PreservationChange> changes,
      int recordIndex,
      int vertexIndex,
      string field,
      ushort sourceValue,
      ushort resultValue
    )
    {
      if (sourceValue == resultValue)
      {
        return;
      }

      changes.Add(
        Change(
          $"StaticRenderObjectSequence[{recordIndex}].RenderVertices[{vertexIndex}].{field}",
          resultValue == ushort.MaxValue
            ? PreservationDisposition.Canonicalized
            : PreservationDisposition.Regenerated,
          "GeometryDependency"
        )
      );
    }

    private MshEditResult<StaticMeshAsset> FailedEdit(string path, string message)
    {
      return new MshEditResult<StaticMeshAsset>(
        false,
        null,
        new PreservationReport(Array.Empty<PreservationChange>()),
        new[] { AuthoringValidation.InvalidEdit(path, message) }
      );
    }

    private static PreservationChange Change(
      string path,
      PreservationDisposition disposition,
      string reason
    )
    {
      return new PreservationChange(path, disposition, reason);
    }

    private void EnsureSourceOrdinal(int ordinal)
    {
      if (ordinal < 0 || ordinal >= InitialRenderObjectCount)
      {
        throw new ArgumentException(
          "The render-object ordinal does not belong to this source snapshot.",
          nameof(ordinal)
        );
      }
    }

    private IEnumerable<int> GetSourceObjectLocalIds(StaticSourceObjectAssembly source)
    {
      yield return _sourceObjectOutputLocalIds[source.Ordinal];
      foreach (var child in source.Children)
      {
        foreach (var id in GetSourceObjectLocalIds(child))
        {
          yield return id;
        }
      }
    }

    private StaticMeshAssemblyTrace CreateAssemblyTrace()
    {
      var changes = new List<StaticMeshAssemblyChange>();
      changes.AddRange(
        _replacementVertices.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.Geometry,
          _renderObjectOutputLocalIds[id]
        ))
      );
      changes.AddRange(
        _replacementPivots.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.Pivot,
          _renderObjectOutputLocalIds[id]
        ))
      );
      changes.AddRange(
        _replacementTexturePathBytes.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.TexturePath,
          _renderObjectOutputLocalIds[id]
        ))
      );
      changes.AddRange(
        _replacementAnimations.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.Animation,
          _renderObjectOutputLocalIds[id]
        ))
      );
      changes.AddRange(
        _replacementMarkerFlags.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.MarkerFlags,
          _renderObjectOutputLocalIds[id]
        ))
      );
      changes.AddRange(
        _replacementAttachmentRecords.Keys.Select(number => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.Attachment,
          physicalNumber: number
        ))
      );
      changes.AddRange(
        _replacementCannonRenderPositions.Keys.Select(number => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.CannonRenderPosition,
          physicalNumber: number
        ))
      );
      changes.AddRange(
        _staticLightFieldChanges.Select(item => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.StaticLight,
          physicalNumber: item.Key.Number,
          lightKind: item.Key.Kind,
          changedFields: item.Value
        ))
      );
      changes.AddRange(
        _removedRenderObjects.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.RemovedRenderObject,
          GetSerializationRenderObjectLocalId(id)
        ))
      );
      changes.AddRange(
        _additions.Select(addition => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.AddedRenderObject,
          addition.LocalId
        ))
      );
      if (_replacementAnimationLengths.HasValue)
      {
        changes.Add(new StaticMeshAssemblyChange(StaticMeshAssemblyChangeKind.AnimationLengths));
      }
      if (_replacementAnimationFrameIndices.HasValue)
      {
        changes.Add(
          new StaticMeshAssemblyChange(StaticMeshAssemblyChangeKind.AnimationFrameIndices)
        );
      }
      if (_replacementHorizontalExtents is not null)
      {
        changes.Add(new StaticMeshAssemblyChange(StaticMeshAssemblyChangeKind.HorizontalExtents));
      }
      if (_editedRootSourceObject is not null)
      {
        changes.Add(new StaticMeshAssemblyChange(StaticMeshAssemblyChangeKind.Hierarchy));
      }

      return new StaticMeshAssemblyTrace(
        changes,
        GetResultRenderObjectLocalIds(),
        _replacementAdded
      );
    }

    private IEnumerable<int> GetResultRenderObjectLocalIds()
    {
      if (_replacementAdded)
      {
        yield return _resultLocalId;
        yield break;
      }

      foreach (var ordinal in PlanRenderObjectOrdinals())
      {
        yield return _renderObjectOutputLocalIds[ordinal];
      }
    }

    private IEnumerable<int> PlanRenderObjectOrdinals()
    {
      var root = CreateFinalSourceObjectAssembly(
        _editedRootSourceObject
          ?? StaticSourceObjectAssembly.FromSource(
            _source!.RootSourceObject,
            _sourceObjectOrdinals,
            _sourceRenderObjectOrdinals
          )
      );
      foreach (var ordinal in CreateFinalSequenceForCommit(root))
      {
        yield return ordinal;
      }
    }

    private IEnumerable<int> CreateFinalSequenceForCommit(StaticSourceObjectAssembly root)
    {
      if (_replacementAdded)
      {
        return new[] { 0 };
      }
      if (_editedRootSourceObject is not null)
      {
        return _editedSequence!;
      }

      var additionsBySource = _additions
        .GroupBy(item => item.SourceObjectOrdinal)
        .ToDictionary(group => group.Key, group => group.ToArray());
      var sourceOrdinalByLocalId = _sourceObjects.ToDictionary(
        item => item.Id.Value,
        item => _sourceObjectOrdinals[item]
      );
      var sourceRecords = _sourceRenderObjects
        .Select((record, ordinal) => (record, ordinal))
        .ToArray();
      var lastRecordBySource = sourceRecords
        .GroupBy(item => sourceOrdinalByLocalId[item.record.SourceObjectId.Value])
        .ToDictionary(group => group.Key, group => group.Last().ordinal);
      var sequence = new List<int>();
      foreach (var item in sourceRecords)
      {
        if (!_removedRenderObjects.Contains(item.ordinal))
        {
          sequence.Add(item.ordinal);
        }
        var sourceOrdinal = sourceOrdinalByLocalId[item.record.SourceObjectId.Value];
        if (
          lastRecordBySource[sourceOrdinal] == item.ordinal
          && additionsBySource.TryGetValue(sourceOrdinal, out var additions)
        )
        {
          sequence.AddRange(additions.Select(addition => addition.Ordinal));
        }
      }
      return sequence;
    }

    private bool EditedSequenceMatchesFinalMembership(StaticSourceObjectAssembly root)
    {
      if (_editedSequence is null)
      {
        return true;
      }

      var membership = new HashSet<int>();
      var membershipCount = 0;
      foreach (var sourceObject in GetSourceAssemblies(root))
      {
        foreach (var ordinal in sourceObject.StaticRenderObjectOrdinals)
        {
          membershipCount++;
          if (!membership.Add(ordinal))
          {
            return false;
          }
        }
      }

      return _editedSequence.Count == membershipCount
        && _editedSequence.Distinct().Count() == _editedSequence.Count
        && _editedSequence.All(membership.Contains);
    }

    internal StaticSourceObjectAssembly CreateFinalSourceObjectAssembly(
      StaticSourceObjectAssembly source
    )
    {
      var renderObjectOrdinals = source
        .StaticRenderObjectOrdinals.Where(ordinal =>
          !_removedRenderObjects.Contains(ordinal) || _replacementAdded && ordinal == 0
        )
        .ToList();
      renderObjectOrdinals.AddRange(
        _additions
          .Where(addition =>
            addition.SourceObjectOrdinal == source.Ordinal
            && !renderObjectOrdinals.Contains(addition.Ordinal)
          )
          .Select(addition => addition.Ordinal)
      );
      return new StaticSourceObjectAssembly(
        source.Ordinal,
        renderObjectOrdinals,
        source.Children.Select(CreateFinalSourceObjectAssembly)
      );
    }

    internal static IReadOnlyList<int> CreateFinalSequence(StaticSourceObjectAssembly root)
    {
      return FlattenAssembly(root).ToArray();
    }

    private static IEnumerable<int> FlattenAssembly(StaticSourceObjectAssembly source)
    {
      yield return source.StaticRenderObjectOrdinals[0];
      foreach (var child in source.Children)
      {
        foreach (var ordinal in FlattenAssembly(child))
        {
          yield return ordinal;
        }
      }
      foreach (var ordinal in source.StaticRenderObjectOrdinals.Skip(1))
      {
        yield return ordinal;
      }
    }

    private static IEnumerable<StaticSourceObjectAssembly> GetSourceAssemblies(
      StaticSourceObjectAssembly source
    )
    {
      yield return source;
      foreach (var child in source.Children)
      {
        foreach (var descendant in GetSourceAssemblies(child))
        {
          yield return descendant;
        }
      }
    }

    private StaticSourceObjectSerializationPlan CreateSerializationPlan(
      StaticSourceObjectAssembly source
    )
    {
      return new StaticSourceObjectSerializationPlan(
        _sourceObjectOutputLocalIds[source.Ordinal],
        source.StaticRenderObjectOrdinals.Select(GetSerializationRenderObjectLocalId),
        source.Children.Select(CreateSerializationPlan)
      );
    }

    private Dictionary<int, TValue> ToSerializationIdentityDictionary<TValue>(
      IReadOnlyDictionary<int, TValue> valuesByOrdinal
    )
    {
      return valuesByOrdinal.ToDictionary(
        item => GetSerializationRenderObjectLocalId(item.Key),
        item => item.Value
      );
    }

    private int GetSerializationRenderObjectLocalId(int ordinal)
    {
      return ordinal < _renderObjectSourceLocalIds.Count
        ? _renderObjectSourceLocalIds[ordinal]
        : _renderObjectOutputLocalIds[ordinal];
    }

    private int AllocateOutputRenderObjectLocalId()
    {
      if (!_nextLocalId.HasValue)
      {
        throw new InvalidOperationException(
          "No lineage-local static render-object identity remains available."
        );
      }

      var value = _nextLocalId.Value;
      _nextLocalId = value == int.MaxValue ? null : value + 1;
      return value;
    }

    private static IEnumerable<StaticSourceObject> FlattenSourceObjects(StaticSourceObject source)
    {
      yield return source;
      foreach (var child in source.Children)
      {
        foreach (var descendant in FlattenSourceObjects(child))
        {
          yield return descendant;
        }
      }
    }

    private static IEnumerable<CanonicalStaticSourceObject> FlattenCanonicalSourceObjects(
      CanonicalStaticSourceObject source
    )
    {
      yield return source;
      foreach (var child in source.Children)
      {
        foreach (var descendant in FlattenCanonicalSourceObjects(child))
        {
          yield return descendant;
        }
      }
    }

    private static IEnumerable<CanonicalStaticRenderObject> FlattenCanonicalRenderObjects(
      CanonicalStaticSourceObject source
    )
    {
      yield return source.RenderObjects[0];
      foreach (var child in source.Children)
      {
        foreach (var renderObject in FlattenCanonicalRenderObjects(child))
        {
          yield return renderObject;
        }
      }
      foreach (var renderObject in source.RenderObjects.Skip(1))
      {
        yield return renderObject;
      }
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    private void EnsureOpen()
    {
      if (_committed)
      {
        throw new InvalidOperationException("An edit session can be committed only once.");
      }
    }
  }

  /// <summary>Accumulates one atomic set of static edits and commits at most once.</summary>
  public sealed class StaticMeshEditSession
  {
    private readonly StaticMeshAsset _source;
    private readonly StaticMeshAssembler _assembler;
    private readonly Dictionary<int, int> _allocatedRenderObjectOrdinals = new();
    private readonly Dictionary<int, int> _allocatedSourceObjectOrdinals = new();

    internal StaticMeshEditSession(StaticMeshAsset source)
    {
      _source = source;
      _assembler = new StaticMeshAssembler(source);
    }

    /// <summary>Replaces geometry while retaining the render-object identity.</summary>
    public StaticMeshEditSession ReplaceGeometry(
      StaticRenderObjectId renderObject,
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles
    )
    {
      EnsureSourceRenderObjectIdentity(renderObject, nameof(renderObject));
      _assembler.ReplaceGeometry(GetSourceRenderObjectOrdinal(renderObject), vertices, triangles);
      return this;
    }

    /// <summary>Removes the current render object before a replacement object is added.</summary>
    public StaticMeshEditSession RemoveRenderObject(StaticRenderObjectId renderObject)
    {
      EnsureSourceRenderObjectIdentity(renderObject, nameof(renderObject));
      _assembler.RemoveRenderObject(GetSourceRenderObjectOrdinal(renderObject));
      return this;
    }

    /// <summary>Adds a new render object and allocates a fresh lineage-local identity.</summary>
    public StaticRenderObjectId AddRenderObject(
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles
    )
    {
      var ordinal = _assembler.AddRenderObject(vertices, triangles);
      var localId = _assembler.GetOutputRenderObjectLocalId(ordinal);
      _allocatedRenderObjectOrdinals.Add(localId, ordinal);
      return new StaticRenderObjectId(_source.LineageId, localId);
    }

    /// <summary>Adds a canonical material partition to an existing source object.</summary>
    public StaticRenderObjectId AddRenderObject(
      SourceObjectId sourceObject,
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles
    )
    {
      EnsureSourceObjectIdentity(sourceObject, nameof(sourceObject));
      var ordinal = _assembler.AddRenderObject(
        GetSourceObjectOrdinal(sourceObject),
        vertices,
        triangles
      );
      var localId = _assembler.GetOutputRenderObjectLocalId(ordinal);
      _allocatedRenderObjectOrdinals.Add(localId, ordinal);
      return new StaticRenderObjectId(_source.LineageId, localId);
    }

    internal SourceObjectId AllocateSourceObjectId()
    {
      var ordinal = _assembler.AllocateSourceObjectOrdinal();
      _allocatedSourceObjectOrdinals.Add(_assembler.GetOutputSourceObjectLocalId(ordinal), ordinal);
      return new SourceObjectId(
        _source.LineageId,
        _assembler.GetOutputSourceObjectLocalId(ordinal)
      );
    }

    internal StaticMeshEditSession ReplacePivot(StaticRenderObjectId renderObject, Vector3 pivot)
    {
      EnsureSessionRenderObjectIdentity(renderObject, nameof(renderObject));
      _assembler.ReplacePivot(GetRenderObjectOrdinal(renderObject), pivot);
      return this;
    }

    internal StaticMeshEditSession ReplaceTexturePathBytes(
      StaticRenderObjectId renderObject,
      IEnumerable<byte> texturePathBytes
    )
    {
      EnsureSessionRenderObjectIdentity(renderObject, nameof(renderObject));
      _assembler.ReplaceTexturePathBytes(GetRenderObjectOrdinal(renderObject), texturePathBytes);
      return this;
    }

    internal StaticMeshEditSession ReplaceAnimation(
      StaticRenderObjectId renderObject,
      IEnumerable<Vector3> scaleFrames,
      IEnumerable<Vector3> translationFrames,
      IEnumerable<Matrix4x4> matrices,
      uint animationClassValue
    )
    {
      EnsureSourceRenderObjectIdentity(renderObject, nameof(renderObject));
      _assembler.ReplaceAnimation(
        GetSourceRenderObjectOrdinal(renderObject),
        scaleFrames,
        translationFrames,
        matrices,
        animationClassValue
      );
      return this;
    }

    internal StaticMeshEditSession ReplaceAnimationLengths(AnimationClassBytes animationLengths)
    {
      _assembler.ReplaceAnimationLengths(animationLengths);
      return this;
    }

    internal StaticMeshEditSession ReplaceAnimationFrameIndices(
      AnimationClassBytes animationFrameIndices
    )
    {
      _assembler.ReplaceAnimationFrameIndices(animationFrameIndices);
      return this;
    }

    internal StaticMeshEditSession ReplaceHorizontalExtents(
      CanonicalHorizontalExtents horizontalExtents
    )
    {
      _assembler.ReplaceHorizontalExtents(horizontalExtents);
      return this;
    }

    internal StaticMeshEditSession ReplaceMarkerAttachmentFlags(
      StaticRenderObjectId renderObject,
      StaticRenderObjectFlags markerFlags
    )
    {
      if (!BelongsToSession(renderObject))
      {
        throw new ArgumentOutOfRangeException(nameof(markerFlags));
      }
      _assembler.ReplaceMarkerAttachmentFlags(GetRenderObjectOrdinal(renderObject), markerFlags);
      return this;
    }

    internal StaticMeshEditSession ReplaceAttachmentRecord(
      int physicalNumber,
      IEnumerable<byte> record
    )
    {
      _assembler.ReplaceAttachmentRecord(physicalNumber, record);
      return this;
    }

    internal StaticMeshEditSession ReplaceCannonRenderPosition(
      int physicalNumber,
      IEnumerable<byte> record
    )
    {
      _assembler.ReplaceCannonRenderPosition(physicalNumber, record);
      return this;
    }

    internal StaticMeshEditSession ReplaceStaticLightRecord(
      StaticLightRecordKind kind,
      int physicalNumber,
      IEnumerable<byte> record,
      IEnumerable<string> changedFields
    )
    {
      _assembler.ReplaceStaticLightRecord(kind, physicalNumber, record, changedFields);
      return this;
    }

    /// <summary>Sets or explicitly clears one game-authoritative TEX resource binding.</summary>
    public StaticMeshEditSession SetTextureResourceBinding(
      StaticRenderObjectId renderObject,
      string? textureResourceKey
    )
    {
      EnsureSessionRenderObjectIdentity(renderObject, nameof(renderObject));
      _assembler.SetTextureResourceBinding(GetRenderObjectOrdinal(renderObject), textureResourceKey);
      return this;
    }

    internal StaticMeshEditSession ApplyHierarchy(
      StaticSourceObject rootSourceObject,
      IReadOnlyList<StaticRenderObjectId> sequence
    )
    {
      _assembler.ApplyHierarchy(
        CreateSourceObjectAssembly(rootSourceObject),
        sequence.Select(GetRenderObjectOrdinal).ToArray()
      );
      return this;
    }

    /// <summary>Commits this session once and returns a new immutable snapshot.</summary>
    public MshEditResult<StaticMeshAsset> Commit(MshOperationProfile? profile = null)
    {
      return _assembler.Commit(profile);
    }

    private void EnsureSourceRenderObjectIdentity(StaticRenderObjectId id, string parameterName)
    {
      if (
        !id.Lineage.Equals(_source.LineageId)
        || !_source.StaticRenderObjectSequence.Any(item => item.LocalId == id.Value)
      )
      {
        throw new ArgumentException(
          "The render-object identity does not belong to this source snapshot.",
          parameterName
        );
      }
    }

    private int GetSourceRenderObjectOrdinal(StaticRenderObjectId id)
    {
      var renderObject = _source.StaticRenderObjectSequence.Single(item => item.Id.Equals(id));
      return _assembler.GetRenderObjectOrdinal(renderObject);
    }

    private int GetRenderObjectOrdinal(StaticRenderObjectId id)
    {
      if (_allocatedRenderObjectOrdinals.TryGetValue(id.Value, out var ordinal))
      {
        return ordinal;
      }
      return GetSourceRenderObjectOrdinal(id);
    }

    private void EnsureSessionRenderObjectIdentity(StaticRenderObjectId id, string parameterName)
    {
      if (!BelongsToSession(id))
      {
        throw new ArgumentException(
          "The render-object identity does not belong to this edit session.",
          parameterName
        );
      }
    }

    private bool BelongsToSession(StaticRenderObjectId id)
    {
      return id.Lineage.Equals(_source.LineageId)
        && (
          _source.StaticRenderObjectSequence.Any(item => item.LocalId == id.Value)
          || _allocatedRenderObjectOrdinals.ContainsKey(id.Value)
        );
    }

    private void EnsureSourceObjectIdentity(SourceObjectId id, string parameterName)
    {
      if (
        !id.Lineage.Equals(_source.LineageId)
        || !GetSourceObjects(_source.RootSourceObject).Any(item => item.Id.Equals(id))
          && !_allocatedSourceObjectOrdinals.ContainsKey(id.Value)
      )
      {
        throw new ArgumentException(
          "The source-object identity does not belong to this source snapshot.",
          parameterName
        );
      }
    }

    private int GetSourceObjectOrdinal(SourceObjectId id)
    {
      if (_allocatedSourceObjectOrdinals.TryGetValue(id.Value, out var ordinal))
      {
        return ordinal;
      }
      var sourceObject = GetSourceObjects(_source.RootSourceObject).Single(item => item.Id.Equals(id));
      return _assembler.GetSourceObjectOrdinal(sourceObject);
    }

    private StaticSourceObjectAssembly CreateSourceObjectAssembly(StaticSourceObject sourceObject)
    {
      return new StaticSourceObjectAssembly(
        GetSourceObjectOrdinal(sourceObject.Id),
        sourceObject.StaticRenderObjectIds.Select(GetRenderObjectOrdinal),
        sourceObject.Children.Select(CreateSourceObjectAssembly)
      );
    }

    private static IEnumerable<StaticSourceObject> GetSourceObjects(StaticSourceObject source)
    {
      yield return source;
      foreach (var child in source.Children)
      {
        foreach (var descendant in GetSourceObjects(child))
        {
          yield return descendant;
        }
      }
    }

  }

  internal static class AuthoringValidation
  {
    internal static OperationDiagnostic? ValidateStaticTree(
      CanonicalStaticSourceObject? root,
      MshOperationProfile profile
    )
    {
      if (root is null)
      {
        return Invalid("RootSourceObject", "A canonical root source object is required.");
      }

      var renderObjectCount = 0;
      return ValidateStaticSourceObject(
        root,
        "RootSourceObject",
        1,
        profile,
        ref renderObjectCount
      );
    }

    internal static OperationDiagnostic? ValidateDynamic(
      CanonicalDynamicObject root,
      MshOperationProfile profile,
      out IReadOnlyList<OperationDiagnostic> diagnostics
    )
    {
      var ancestors = new HashSet<CanonicalDynamicObject>();
      var seen = new HashSet<CanonicalDynamicObject>();
      var warnings = new List<OperationDiagnostic>();
      var objectCount = 0;
      var stringBytes = 0;
      var failure = ValidateDynamicObject(
        root,
        "RootDynamicObject",
        1,
        profile,
        ancestors,
        seen,
        warnings,
        ref objectCount,
        ref stringBytes
      );
      if (warnings.Count > profile.MaxDiagnostics)
      {
        warnings.RemoveRange(
          profile.MaxDiagnostics - 1,
          warnings.Count - profile.MaxDiagnostics + 1
        );
        warnings.Add(
          new OperationDiagnostic(
            MshDiagnosticCodes.DiagnosticsTruncated,
            1010,
            DiagnosticSeverity.Warning,
            "$",
            "Additional diagnostics were suppressed by the operation profile."
          )
        );
      }

      diagnostics = warnings.AsReadOnly();
      return failure;
    }

    internal static OperationDiagnostic? ValidateStatic(
      IReadOnlyList<CanonicalStaticVertex>? vertices,
      IReadOnlyList<CanonicalTriangle>? triangles
    )
    {
      if (vertices is null || triangles is null)
      {
        return Invalid("StaticRenderObject", "Canonical static geometry is required.");
      }

      if (vertices.Count == 0 || vertices.Count > 65536 || triangles.Count == 0)
      {
        return Invalid(
          "StaticRenderObject",
          "Canonical static geometry counts are outside the supported format range."
        );
      }

      for (var index = 0; index < vertices.Count; index++)
      {
        var vertex = vertices[index];
        if (!IsFinite(vertex.Position))
        {
          return Invalid(
            $"StaticRenderObject.RenderVertices[{index}].Position",
            "Position must be finite."
          );
        }

        if (!IsFinite(vertex.Normal))
        {
          return Invalid(
            $"StaticRenderObject.RenderVertices[{index}].Normal",
            "Normal must be finite."
          );
        }

        if (!IsFinite(vertex.TextureCoordinate))
        {
          return Invalid(
            $"StaticRenderObject.RenderVertices[{index}].TextureCoordinate",
            "Texture coordinate must be finite."
          );
        }
      }

      for (var index = 0; index < triangles.Count; index++)
      {
        var triangle = triangles[index];
        if (
          triangle.Vertex0 >= vertices.Count
          || triangle.Vertex1 >= vertices.Count
          || triangle.Vertex2 >= vertices.Count
        )
        {
          return Invalid(
            $"StaticRenderObject.Triangles[{index}]",
            "Triangle indices must reference active vertices."
          );
        }
      }

      return null;
    }

    internal static OperationDiagnostic? ValidateStaticHeader(
      CanonicalStaticSourceObject root,
      CanonicalStaticFootprint? footprint,
      CanonicalHorizontalExtents? horizontalExtents
    )
    {
      var vertices = root.RenderObjects.SelectMany(item => item.RenderVertices).ToArray();
      if (footprint is null)
      {
        var maximumZ = vertices.Max(vertex => vertex.Position.Z);
        if (maximumZ < 0 || maximumZ * 256d > ushort.MaxValue)
        {
          return Invalid(
            "CommonBaseHeader.Footprint",
            "The derived footprint height is out of range."
          );
        }
      }
      if (horizontalExtents is null)
      {
        var extent = vertices
          .SelectMany(vertex =>
            new[]
            {
              Math.Max(0, vertex.Position.X),
              Math.Max(0, -vertex.Position.X),
              Math.Max(0, vertex.Position.Y),
              Math.Max(0, -vertex.Position.Y),
            }
          )
          .Max();
        if (extent * 256d > ushort.MaxValue)
        {
          return Invalid(
            "CommonBaseHeader.HorizontalExtents",
            "The derived horizontal extents are out of range."
          );
        }
      }
      return null;
    }

    internal static OperationDiagnostic? ValidateStaticForProfile(
      IReadOnlyList<CanonicalStaticVertex>? vertices,
      IReadOnlyList<CanonicalTriangle>? triangles,
      MshOperationProfile profile,
      string path
    )
    {
      var failure = ValidateStatic(vertices, triangles);
      if (failure is not null || vertices is null || triangles is null)
      {
        return failure;
      }

      if (vertices.Count > profile.MaxStaticVerticesPerObject)
      {
        return ResourceLimit(
          vertices.Count,
          profile.MaxStaticVerticesPerObject,
          path + ".RenderVertices"
        );
      }

      var blockCount = (vertices.Count + 3) / 4;
      if (blockCount > profile.MaxStaticVertexBlocksPerObject)
      {
        return ResourceLimit(
          blockCount,
          profile.MaxStaticVertexBlocksPerObject,
          path + ".VertexBlockCount"
        );
      }

      return triangles.Count > profile.MaxStaticTrianglesPerObject
        ? ResourceLimit(triangles.Count, profile.MaxStaticTrianglesPerObject, path + ".Triangles")
        : null;
    }

    private static OperationDiagnostic? ValidateStaticSourceObject(
      CanonicalStaticSourceObject source,
      string path,
      int depth,
      MshOperationProfile profile,
      ref int renderObjectCount
    )
    {
      if (depth > profile.MaxStaticHierarchyDepth)
      {
        return ResourceLimit(depth, profile.MaxStaticHierarchyDepth, path);
      }

      if (source.RenderObjects.Count == 0)
      {
        return Invalid(
          path + ".RenderObjects",
          "Every canonical source object requires a material partition."
        );
      }

      foreach (var renderObject in source.RenderObjects)
      {
        renderObjectCount++;
        if (renderObjectCount > profile.MaxStaticRenderObjects)
        {
          return ResourceLimit(
            renderObjectCount,
            profile.MaxStaticRenderObjects,
            "StaticRenderObjectSequence"
          );
        }

        var failure = ValidateStaticForProfile(
          renderObject.RenderVertices,
          renderObject.Triangles,
          profile,
          path + ".RenderObjects"
        );
        if (failure is not null)
        {
          return failure;
        }
        if (renderObject.TextureResourceKey is not null)
        {
          if (!IsCanonicalTextureResourceKey(renderObject.TextureResourceKey))
          {
            return Invalid(
              path + ".RenderObjects.TextureResourceKey",
              "TEX resource keys must use safe Textures\\...\\*.tex spelling."
            );
          }
          var byteCount = Encoding.ASCII.GetByteCount(renderObject.TextureResourceKey);
          if (byteCount > profile.MaxStaticTexturePathBytes)
          {
            return ResourceLimit(
              byteCount,
              profile.MaxStaticTexturePathBytes,
              path + ".RenderObjects.TextureResourceKey"
            );
          }
        }
      }

      for (var index = 0; index < source.Children.Count; index++)
      {
        var failure = ValidateStaticSourceObject(
          source.Children[index],
          path
            + ".Children["
            + index.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "]",
          depth + 1,
          profile,
          ref renderObjectCount
        );
        if (failure is not null)
        {
          return failure;
        }
      }

      return null;
    }

    internal static byte[] EncodeCanonicalTextureResourceKey(string value, string parameterName)
    {
      if (!IsCanonicalTextureResourceKey(value))
      {
        throw new ArgumentException(
          "TEX resource keys must use safe Textures\\...\\*.tex spelling.",
          parameterName
        );
      }
      return Encoding.ASCII.GetBytes(value);
    }

    internal static OperationDiagnostic Invalid(string path, string message)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.InvalidAuthoringInput,
        1011,
        DiagnosticSeverity.Error,
        path,
        message
      );
    }

    internal static OperationDiagnostic InvalidEdit(string path, string message)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.InvalidEdit,
        1012,
        DiagnosticSeverity.Error,
        path,
        message
      );
    }

    internal static OperationDiagnostic ResourceLimit(long actual, int maximum)
    {
      return CreateResourceLimit(
        "$",
        "The serialized representation exceeds the configured operation profile.",
        actual,
        maximum
      );
    }

    private static OperationDiagnostic? ValidateDynamicObject(
      CanonicalDynamicObject current,
      string path,
      int depth,
      MshOperationProfile profile,
      HashSet<CanonicalDynamicObject> ancestors,
      HashSet<CanonicalDynamicObject> seen,
      List<OperationDiagnostic> diagnostics,
      ref int objectCount,
      ref int stringBytes
    )
    {
      if (!ancestors.Add(current))
      {
        return Invalid(path, "A canonical dynamic tree cannot contain ancestor or self cycles.");
      }

      if (!seen.Add(current))
      {
        diagnostics.Add(
          new OperationDiagnostic(
            MshDiagnosticCodes.CompatibilityAnomaly,
            1009,
            DiagnosticSeverity.Warning,
            path,
            "A reused draft instance will be serialized as an independent dynamic object."
          )
        );
      }

      var behaviorFailure = DynamicEffectBehavior.ValidateAuthoring(
        current.Recipe,
        depth == 1 ? DynamicObjectPlacement.Root : DynamicObjectPlacement.Child
      );
      if (behaviorFailure?.Field == DynamicBehaviorField.EffectType)
      {
        return behaviorFailure.At(path);
      }

      if (depth > profile.MaxDynamicDepth)
      {
        return ResourceLimit(depth, profile.MaxDynamicDepth, path);
      }

      objectCount++;
      if (objectCount > profile.MaxDynamicObjects)
      {
        return ResourceLimit(objectCount, profile.MaxDynamicObjects, path);
      }

      if (current.Children.Count > profile.MaxDynamicChildrenPerObject)
      {
        return ResourceLimit(
          current.Children.Count,
          profile.MaxDynamicChildrenPerObject,
          path + ".Children"
        );
      }

      var recipeFailure = ValidateDynamicRecipe(
        current,
        path,
        profile,
        behaviorFailure,
        ref stringBytes
      );
      if (recipeFailure is not null)
      {
        return recipeFailure;
      }

      for (var index = 0; index < current.Children.Count; index++)
      {
        var failure = ValidateDynamicObject(
          current.Children[index],
          path
            + ".Children["
            + index.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "]",
          depth + 1,
          profile,
          ancestors,
          seen,
          diagnostics,
          ref objectCount,
          ref stringBytes
        );
        if (failure is not null)
        {
          return failure;
        }
      }

      ancestors.Remove(current);
      return null;
    }

    private static OperationDiagnostic? ValidateDynamicRecipe(
      CanonicalDynamicObject current,
      string path,
      MshOperationProfile profile,
      DynamicBehaviorFinding? behaviorFailure,
      ref int stringBytes
    )
    {
      var recipe = current.Recipe;
      if (behaviorFailure is not null)
      {
        return behaviorFailure.At(path);
      }

      if (
        !string.IsNullOrEmpty(recipe.TextureResourceKey)
        && !IsCanonicalTextureResourceKey(recipe.TextureResourceKey)
      )
      {
        return Invalid(
          path + ".Extension.TexturePathBytes",
          "Texture resource keys must use safe Textures\\...\\*.tex spelling."
        );
      }

      try
      {
        var meshBytes = MshCanonicalSerializer.EncodeDynamicString(recipe.MeshResourceKey).Length;
        var textureBytes = MshCanonicalSerializer
          .EncodeDynamicString(recipe.TextureResourceKey)
          .Length;
        stringBytes = checked(stringBytes + meshBytes + textureBytes);
      }
      catch (EncoderFallbackException)
      {
        return Invalid(
          path + ".Extension",
          "Dynamic resource keys must be representable as ISO-8859-2 bytes."
        );
      }
      catch (OverflowException)
      {
        return ResourceLimit(long.MaxValue, profile.MaxDynamicStringBytes, path + ".Extension");
      }

      return stringBytes > profile.MaxDynamicStringBytes
        ? ResourceLimit(stringBytes, profile.MaxDynamicStringBytes, path + ".Extension")
        : null;
    }

    internal static bool IsCanonicalTextureResourceKey(string value)
    {
      if (
        !value.StartsWith("Textures\\", StringComparison.OrdinalIgnoreCase)
        || !value.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)
        || value.Contains('/')
        || value.Contains(':')
        || value.Contains('?')
        || value.Contains('#')
        || value.Any(character =>
          character > 0x7F
          || char.IsControl(character)
          || character is '*' or '"' or '<' or '>' or '|'
        )
      )
      {
        return false;
      }

      var segments = value.Split('\\');
      return segments.Length >= 2
        && segments[^1].Length > 4
        && segments.All(segment =>
          segment.Length > 0
          && segment is not "." and not ".."
          && !segment.EndsWith(" ", StringComparison.Ordinal)
          && !segment.EndsWith(".", StringComparison.Ordinal)
        );
    }

    private static OperationDiagnostic ResourceLimit(long actual, int maximum, string path)
    {
      return CreateResourceLimit(
        path,
        "The canonical dynamic tree exceeds the configured operation profile.",
        actual,
        maximum
      );
    }

    private static OperationDiagnostic CreateResourceLimit(
      string path,
      string message,
      long actual,
      int maximum
    )
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.ResourceLimitExceeded,
        1004,
        DiagnosticSeverity.Error,
        path,
        message,
        data: new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(System.Globalization.CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }
      );
    }

    private static bool IsFinite(Vector3 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }

    private static bool IsFinite(Vector2 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y);
    }

    private static bool IsFinite(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value);
    }
  }
}
