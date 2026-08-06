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
    private AnimationClassBytes _animationLengths;
    private CanonicalStaticSourceObject? _rootSourceObject;
    private CanonicalStaticFootprint? _footprint;
    private CanonicalHorizontalExtents? _horizontalExtents;

    private StaticMeshBuilder(Guid creationGuid)
    {
      _creationGuid = creationGuid;
    }

    /// <summary>Creates a builder with a generated creation identity.</summary>
    public static StaticMeshBuilder Create()
    {
      return Create(Guid.NewGuid());
    }

    /// <summary>Creates a builder with an explicit creation identity.</summary>
    public static StaticMeshBuilder Create(Guid creationGuid)
    {
      return new StaticMeshBuilder(creationGuid);
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

        var decoded = MshV1Decoder.Decode(
          bytes,
          profile,
          CancellationToken.None,
          MeshAssetOrigin.Canonical
        );
        return new MshBuildResult<StaticMeshAsset>(
          true,
          (StaticMeshAsset)decoded.Asset,
          decoded.Diagnostics
        );
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
    private CanonicalDynamicObject _root = DynamicEffectRecipes.Group();

    private DynamicMeshBuilder(Guid creationGuid)
    {
      _creationGuid = creationGuid;
    }

    /// <summary>Creates a builder with a generated creation identity.</summary>
    public static DynamicMeshBuilder Create()
    {
      return Create(Guid.NewGuid());
    }

    /// <summary>Creates a builder with an explicit creation identity.</summary>
    public static DynamicMeshBuilder Create(Guid creationGuid)
    {
      return new DynamicMeshBuilder(creationGuid);
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
      var decoded = MshV1Decoder.Decode(
        bytes,
        profile,
        CancellationToken.None,
        MeshAssetOrigin.Canonical
      );
      return new MshBuildResult<DynamicMeshAsset>(
        true,
        (DynamicMeshAsset)decoded.Asset,
        validationDiagnostics.Concat(decoded.Diagnostics)
      );
    }
  }

  /// <summary>Describes one source-scoped canonical partition addition.</summary>
  internal sealed class StaticRenderObjectAddition
  {
    internal int Ordinal { get; }

    internal int SourceObjectOrdinal { get; }

    internal IReadOnlyList<CanonicalStaticVertex> Vertices { get; }

    internal IReadOnlyList<CanonicalTriangle> Triangles { get; }

    internal IReadOnlyList<byte> TexturePathBytes { get; private set; }

    internal StaticRenderObjectAddition(
      int ordinal,
      int sourceObjectOrdinal,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles
    )
    {
      Ordinal = ordinal;
      SourceObjectOrdinal = sourceObjectOrdinal;
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

    internal int? RenderObjectOrdinal { get; }

    internal int? PhysicalNumber { get; }

    internal StaticLightRecordKind? LightKind { get; }

    internal IReadOnlyList<string> ChangedFields { get; }

    internal StaticMeshAssemblyChange(
      StaticMeshAssemblyChangeKind kind,
      int? renderObjectOrdinal = null,
      int? physicalNumber = null,
      StaticLightRecordKind? lightKind = null,
      IEnumerable<string>? changedFields = null
    )
    {
      Kind = kind;
      RenderObjectOrdinal = renderObjectOrdinal;
      PhysicalNumber = physicalNumber;
      LightKind = lightKind;
      ChangedFields = Array.AsReadOnly((changedFields ?? Array.Empty<string>()).ToArray());
    }
  }

  internal sealed class StaticMeshAssemblyTrace
  {
    internal IReadOnlyList<StaticMeshAssemblyChange> Changes { get; }

    internal IReadOnlyList<int> ResultRenderObjectOrdinals { get; }

    internal bool ReplacedSingleRenderObject { get; }

    internal StaticMeshAssemblyTrace(
      IEnumerable<StaticMeshAssemblyChange> changes,
      IEnumerable<int> resultRenderObjectOrdinals,
      bool replacedSingleRenderObject
    )
    {
      Changes = Array.AsReadOnly(changes.ToArray());
      ResultRenderObjectOrdinals = Array.AsReadOnly(resultRenderObjectOrdinals.ToArray());
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
    private readonly MeshAssetOrigin _origin;
    private readonly IReadOnlyList<StaticRenderObject> _sourceRenderObjects;
    private readonly IReadOnlyList<StaticSourceObject> _sourceObjects;
    private readonly IReadOnlyList<CanonicalStaticRenderObject> _canonicalRenderObjects;
    private readonly IReadOnlyList<CanonicalStaticSourceObject> _canonicalSourceObjects;
    private readonly Dictionary<StaticRenderObject, int> _sourceRenderObjectOrdinals;
    private readonly Dictionary<StaticSourceObject, int> _sourceObjectOrdinals;
    private readonly Dictionary<CanonicalStaticRenderObject, int> _canonicalRenderObjectOrdinals;
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
    private CanonicalTriangle[]? _triangles;
    private CanonicalStaticVertex[]? _vertices;
    private bool _committed;
    private bool _invalidSequence;
    private bool _removed;
    private bool _replacementAdded;
    private AnimationClassBytes? _replacementAnimationLengths;
    private AnimationClassBytes? _replacementAnimationFrameIndices;
    private CanonicalHorizontalExtents? _replacementHorizontalExtents;

    internal StaticMeshAssembler(StaticMeshAsset source)
    {
      _source = source;
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
    }

    private StaticMeshAssembler(
      Guid creationGuid,
      CanonicalStaticSourceObject rootSourceObject,
      CanonicalStaticFootprint footprint,
      CanonicalHorizontalExtents horizontalExtents
    )
    {
      _canonicalRoot = rootSourceObject;
      _canonicalCreationGuid = creationGuid;
      _canonicalFootprint = footprint;
      _canonicalHorizontalExtents = horizontalExtents;
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
    }

    internal static StaticMeshAssembler CreateCanonical(
      Guid creationGuid,
      CanonicalStaticSourceObject rootSourceObject,
      CanonicalStaticFootprint footprint,
      CanonicalHorizontalExtents horizontalExtents
    )
    {
      return new StaticMeshAssembler(
        creationGuid,
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

    private int InitialRenderObjectCount => _source is null
      ? _canonicalRenderObjects.Count
      : _sourceRenderObjects.Count;

    private int InitialSourceObjectCount => _source is null
      ? _canonicalSourceObjects.Count
      : _sourceObjects.Count;

    private int RenderObjectCount => InitialRenderObjectCount + _additions.Count;

    private int SourceObjectCount => InitialSourceObjectCount + _allocatedSourceObjects.Count;

    internal StaticMeshAssemblyTrace Trace => CreateAssemblyTrace();

    /// <summary>Replaces geometry for one source render-object ordinal.</summary>
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

    /// <summary>Adds a replacement render object.</summary>
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
      if (sourceObjectOrdinal < 0 || sourceObjectOrdinal >= SourceObjectCount)
      {
        throw new ArgumentException(
          "The source-object ordinal does not belong to this assembly.",
          nameof(sourceObjectOrdinal)
        );
      }

      var ordinal = InitialRenderObjectCount + _additions.Count;
      _additions.Add(
        new StaticRenderObjectAddition(
          ordinal,
          sourceObjectOrdinal,
          vertices?.ToArray() ?? throw new ArgumentNullException(nameof(vertices)),
          triangles?.ToArray() ?? throw new ArgumentNullException(nameof(triangles))
        )
      );
      return ordinal;
    }

    internal int AllocateSourceObjectOrdinal()
    {
      EnsureOpen();
      var ordinal = SourceObjectCount;
      _allocatedSourceObjects.Add(ordinal);
      return ordinal;
    }

    internal void ReplacePivot(int renderObjectOrdinal, Vector3 pivot)
    {
      EnsureOpen();
      if (renderObjectOrdinal < 0 || renderObjectOrdinal >= RenderObjectCount)
      {
        throw new ArgumentException(
          "The render-object ordinal does not belong to this assembly.",
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
          "The render-object ordinal does not belong to this assembly.",
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
        || renderObjectOrdinal >= RenderObjectCount
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
      if (sequence.Any(ordinal => ordinal < 0 || ordinal >= RenderObjectCount))
      {
        throw new ArgumentException(
          "The hierarchy sequence contains a render-object ordinal outside this assembly.",
          nameof(sequence)
        );
      }
      _editedSequence = Array.AsReadOnly(sequence.ToArray());
    }

    /// <summary>Commits this session once and returns a new immutable snapshot.</summary>
    internal MshBuildResult<StaticMeshAsset> Commit(MshOperationProfile? profile = null)
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
          return new MshBuildResult<StaticMeshAsset>(false, null, new[] { failure });
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
          $"StaticRenderObjectSequence[{addition.Ordinal}]"
        );
        if (failure is not null)
        {
          return new MshBuildResult<StaticMeshAsset>(false, null, new[] { failure });
        }
        if (addition.TexturePathBytes.Count > profile.MaxStaticTexturePathBytes)
        {
          return new MshBuildResult<StaticMeshAsset>(
            false,
            null,
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
        return new MshBuildResult<StaticMeshAsset>(
          false,
          null,
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
              item => item.Key,
              item => (IReadOnlyList<CanonicalStaticVertex>)item.Value
            ),
            _replacementTriangles.ToDictionary(
              item => item.Key,
              item => (IReadOnlyList<CanonicalTriangle>)item.Value
            ),
            _replacementAdded ? Array.Empty<int>() : _removedRenderObjects,
            _additions,
            CreateSerializationPlan(finalRootSourceObject),
            finalSequence,
            _replacementPivots,
            _replacementTexturePathBytes,
            _editedRootSourceObject is not null,
            _replacementMarkerFlags,
            _replacementAnimations,
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
        return new MshBuildResult<StaticMeshAsset>(
          false,
          null,
          new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) }
        );
      }

      MshDecodeResult decoded;
      try
      {
        decoded = MshV1Decoder.Decode(bytes, profile, CancellationToken.None, _origin);
      }
      catch (MshContentException ex)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { ex.Diagnostic });
      }

      var edited = (StaticMeshAsset)decoded.Asset;
      return new MshBuildResult<StaticMeshAsset>(
        true,
        edited,
        decoded.Diagnostics
      );
    }

    private MshBuildResult<StaticMeshAsset> CommitCanonical(MshOperationProfile profile)
    {
      var treeFailure = AuthoringValidation.ValidateStaticTree(_canonicalRoot, profile);
      if (treeFailure is not null)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { treeFailure });
      }
      var headerFailure = AuthoringValidation.ValidateStaticHeader(
        _canonicalRoot!,
        _canonicalFootprint,
        _canonicalHorizontalExtents
      );
      if (headerFailure is not null)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { headerFailure });
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
        return new MshBuildResult<StaticMeshAsset>(
          false,
          null,
          new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) }
        );
      }

      MshDecodeResult decoded;
      try
      {
        decoded = MshV1Decoder.Decode(bytes, profile, CancellationToken.None, _origin);
      }
      catch (MshContentException ex)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { ex.Diagnostic });
      }
      var authored = (StaticMeshAsset)decoded.Asset;
      return new MshBuildResult<StaticMeshAsset>(
        true,
        authored,
        decoded.Diagnostics
      );
    }


    private MshBuildResult<StaticMeshAsset> FailedEdit(string path, string message)
    {
      return new MshBuildResult<StaticMeshAsset>(
        false,
        null,
        new[] { AuthoringValidation.Invalid(path, message) }
      );
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

    private StaticMeshAssemblyTrace CreateAssemblyTrace()
    {
      var changes = new List<StaticMeshAssemblyChange>();
      changes.AddRange(
        _replacementVertices.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.Geometry,
          id
        ))
      );
      changes.AddRange(
        _replacementPivots.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.Pivot,
          id
        ))
      );
      changes.AddRange(
        _replacementTexturePathBytes.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.TexturePath,
          id
        ))
      );
      changes.AddRange(
        _replacementAnimations.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.Animation,
          id
        ))
      );
      changes.AddRange(
        _replacementMarkerFlags.Keys.Select(id => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.MarkerFlags,
          id
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
          id
        ))
      );
      changes.AddRange(
        _additions.Select(addition => new StaticMeshAssemblyChange(
          StaticMeshAssemblyChangeKind.AddedRenderObject,
          addition.Ordinal
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
        PlanRenderObjectOrdinals(),
        _replacementAdded
      );
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
      var sourceOrdinalByRecord = _sourceObjects
        .SelectMany(source => source.StaticRenderObjects.Select(record => (source, record)))
        .ToDictionary(item => item.record, item => _sourceObjectOrdinals[item.source]);
      var sourceRecords = _sourceRenderObjects
        .Select((record, ordinal) => (record, ordinal))
        .ToArray();
      var lastRecordBySource = sourceRecords
        .GroupBy(item => sourceOrdinalByRecord[item.record])
        .ToDictionary(group => group.Key, group => group.Last().ordinal);
      var sequence = new List<int>();
      foreach (var item in sourceRecords)
      {
        if (!_removedRenderObjects.Contains(item.ordinal))
        {
          sequence.Add(item.ordinal);
        }
        var sourceOrdinal = sourceOrdinalByRecord[item.record];
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
        source.Ordinal,
        source.StaticRenderObjectOrdinals,
        source.Children.Select(CreateSerializationPlan)
      );
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
        throw new InvalidOperationException("An assembly can be committed only once.");
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
