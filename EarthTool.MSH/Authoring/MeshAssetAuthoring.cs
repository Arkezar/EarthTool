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
      string? textureResourceKey = null)
    {
      RenderVertices = Array.AsReadOnly(
        (renderVertices ?? throw new ArgumentNullException(nameof(renderVertices))).ToArray());
      Triangles = Array.AsReadOnly(
        (triangles ?? throw new ArgumentNullException(nameof(triangles))).ToArray());
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

    /// <summary>Initializes one canonical source-object draft.</summary>
    public CanonicalStaticSourceObject(
      IEnumerable<CanonicalStaticRenderObject> renderObjects,
      IEnumerable<CanonicalStaticSourceObject>? children = null)
    {
      RenderObjects = Array.AsReadOnly(
        (renderObjects ?? throw new ArgumentNullException(nameof(renderObjects))).ToArray());
      Children = Array.AsReadOnly((children ?? Array.Empty<CanonicalStaticSourceObject>()).ToArray());
      if (RenderObjects.Any(item => item is null) || Children.Any(item => item is null))
      {
        throw new ArgumentException("Canonical static collections cannot contain null values.");
      }
    }
  }

  /// <summary>Returns a canonical or expert value without a partial value on expected failure.</summary>
  public sealed class MshBuildResult<T> where T : class
  {
    private readonly T? _value;

    /// <summary>Gets whether construction succeeded.</summary>
    public bool IsSuccess { get; }
    /// <summary>Gets operation-scoped diagnostics.</summary>
    public IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal MshBuildResult(bool isSuccess, T? value, IEnumerable<OperationDiagnostic>? diagnostics = null)
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

    private StaticMeshBuilder(Guid creationGuid, MeshAssetLineageId lineageId)
    {
      _creationGuid = creationGuid;
      _lineageId = lineageId;
    }

    /// <summary>Creates a builder with stable generated creation and lineage identities.</summary>
    public static StaticMeshBuilder Create()
    {
      return new StaticMeshBuilder(Guid.NewGuid(), new MeshAssetLineageId(Guid.NewGuid()));
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
      IEnumerable<CanonicalTriangle> triangles)
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
        new[] { new CanonicalStaticRenderObject(vertices, triangles) });
      return this;
    }

    /// <summary>Sets the complete canonical source-object tree.</summary>
    public StaticMeshBuilder SetRootSourceObject(CanonicalStaticSourceObject rootSourceObject)
    {
      _rootSourceObject = rootSourceObject ?? throw new ArgumentNullException(nameof(rootSourceObject));
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

      try
      {
        var bytes = MshCanonicalSerializer.CreateStatic(
          _creationGuid,
          _animationLengths,
          _rootSourceObject!);
        if (bytes.Length > profile.MaxOutputBytes)
        {
          return new MshBuildResult<StaticMeshAsset>(false, null,
            new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) });
        }

        var decoded = MshV1Decoder.Decode(
          bytes,
          profile,
          CancellationToken.None,
          _lineageId,
          MeshAssetOrigin.Canonical);
        return new MshBuildResult<StaticMeshAsset>(
          true,
          (StaticMeshAsset)decoded.Asset,
          decoded.Diagnostics);
      }
      catch (OverflowException)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null,
          new[] { AuthoringValidation.Invalid("CommonBaseHeader", "A derived fixed-point value is out of range.") });
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
      IEnumerable<CanonicalDynamicObject>? children)
    {
      Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
      EffectType = recipe.EffectType;
      _children = children?.ToList() ?? new List<CanonicalDynamicObject>();
      if (_children.Any(child => child is null))
      {
        throw new ArgumentException("Dynamic child collections cannot contain null values.", nameof(children));
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
      Vector3 endTranslation)
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
      return new DynamicMeshBuilder(Guid.NewGuid(), new MeshAssetLineageId(Guid.NewGuid()));
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
        out var validationDiagnostics);
      if (failure is not null)
      {
        return new MshBuildResult<DynamicMeshAsset>(
          false,
          null,
          validationDiagnostics.Concat(new[] { failure }));
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
        return new MshBuildResult<DynamicMeshAsset>(false, null,
          new[] { AuthoringValidation.ResourceLimit(long.MaxValue, profile.MaxOutputBytes) });
      }

      if (outputLength > profile.MaxOutputBytes)
      {
        return new MshBuildResult<DynamicMeshAsset>(false, null,
          new[] { AuthoringValidation.ResourceLimit(outputLength, profile.MaxOutputBytes) });
      }

      var bytes = MshCanonicalSerializer.CreateDynamic(_creationGuid, _root, dynamicLength);
      var decoded = MshV1Decoder.Decode(
        bytes,
        profile,
        CancellationToken.None,
        _lineageId,
        MeshAssetOrigin.Canonical);
      return new MshBuildResult<DynamicMeshAsset>(
        true,
        (DynamicMeshAsset)decoded.Asset,
        validationDiagnostics.Concat(decoded.Diagnostics));
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
    Canonicalized = 3
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
  public sealed class MshEditResult<T> where T : class
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
      IEnumerable<OperationDiagnostic>? diagnostics = null)
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
    internal StaticRenderObjectId Id { get; }

    internal SourceObjectId SourceObjectId { get; }

    internal IReadOnlyList<CanonicalStaticVertex> Vertices { get; }

    internal IReadOnlyList<CanonicalTriangle> Triangles { get; }

    internal IReadOnlyList<byte> TexturePathBytes { get; private set; }

    internal StaticRenderObjectAddition(
      StaticRenderObjectId id,
      SourceObjectId sourceObjectId,
      IReadOnlyList<CanonicalStaticVertex> vertices,
      IReadOnlyList<CanonicalTriangle> triangles)
    {
      Id = id;
      SourceObjectId = sourceObjectId;
      Vertices = vertices;
      Triangles = triangles;
      TexturePathBytes = Array.Empty<byte>();
    }

    internal void SetTexturePathBytes(IEnumerable<byte> texturePathBytes)
    {
      TexturePathBytes = Array.AsReadOnly(texturePathBytes.ToArray());
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

  /// <summary>Accumulates one atomic set of static edits and commits at most once.</summary>
  public sealed class StaticMeshEditSession
  {
    private readonly StaticMeshAsset _source;
    private readonly Dictionary<StaticRenderObjectId, CanonicalTriangle[]> _replacementTriangles = new();
    private readonly Dictionary<StaticRenderObjectId, CanonicalStaticVertex[]> _replacementVertices = new();
    private readonly Dictionary<StaticRenderObjectId, Vector3> _replacementPivots = new();
    private readonly Dictionary<StaticRenderObjectId, byte[]> _replacementTexturePathBytes = new();
    private readonly Dictionary<StaticRenderObjectId, StaticAnimationReplacement>
      _replacementAnimations = new();
    private readonly Dictionary<int, byte[]> _replacementAttachmentRecords = new();
    private readonly Dictionary<int, byte[]> _replacementCannonRenderPositions = new();
    private readonly HashSet<StaticRenderObjectId> _removedRenderObjects = new();
    private readonly HashSet<SourceObjectId> _allocatedSourceObjects = new();
    private readonly List<StaticRenderObjectAddition> _additions = new();
    private StaticSourceObject? _editedRootSourceObject;
    private IReadOnlyList<StaticRenderObjectId>? _editedSequence;
    private StaticRenderObjectId _resultId;
    private CanonicalTriangle[]? _triangles;
    private CanonicalStaticVertex[]? _vertices;
    private bool _committed;
    private bool _invalidSequence;
    private bool _removed;
    private bool _replacementAdded;
    private int? _nextLocalId;
    private int? _nextSourceObjectLocalId;
    private AnimationClassBytes? _replacementAnimationLengths;

    internal StaticMeshEditSession(StaticMeshAsset source)
    {
      _source = source;
      _resultId = source.StaticRenderObjectSequence[0].Id;
      _nextLocalId = source.NextStaticRenderObjectLocalId;
      _nextSourceObjectLocalId = source.NextSourceObjectLocalId;
    }

    /// <summary>Replaces geometry while retaining the render-object identity.</summary>
    public StaticMeshEditSession ReplaceGeometry(
      StaticRenderObjectId renderObject,
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles)
    {
      EnsureOpen();
      EnsureSourceId(renderObject);
      if (_removed || _replacementAdded)
      {
        _invalidSequence = true;
      }

      _replacementVertices[renderObject] = vertices?.ToArray()
        ?? throw new ArgumentNullException(nameof(vertices));
      _replacementTriangles[renderObject] = triangles?.ToArray()
        ?? throw new ArgumentNullException(nameof(triangles));
      return this;
    }

    /// <summary>Removes the current render object before a replacement object is added.</summary>
    public StaticMeshEditSession RemoveRenderObject(StaticRenderObjectId renderObject)
    {
      EnsureOpen();
      EnsureSourceId(renderObject);
      if (_replacementAdded || !_removedRenderObjects.Add(renderObject))
      {
        _invalidSequence = true;
      }

      _removed = true;
      _vertices = null;
      _triangles = null;
      return this;
    }

    /// <summary>Adds a new render object and allocates a fresh lineage-local identity.</summary>
    public StaticRenderObjectId AddRenderObject(
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles)
    {
      EnsureOpen();
      if (!_removed || _replacementAdded)
      {
        _invalidSequence = true;
      }

      _vertices = vertices?.ToArray() ?? throw new ArgumentNullException(nameof(vertices));
      _triangles = triangles?.ToArray() ?? throw new ArgumentNullException(nameof(triangles));
      _resultId = AllocateRenderObjectId();
      _replacementAdded = true;
      return _resultId;
    }

    /// <summary>Adds a canonical material partition to an existing source object.</summary>
    public StaticRenderObjectId AddRenderObject(
      SourceObjectId sourceObject,
      IEnumerable<CanonicalStaticVertex> vertices,
      IEnumerable<CanonicalTriangle> triangles)
    {
      EnsureOpen();
      if (!GetSourceObjects(_source.RootSourceObject).Any(source => source.Id.Equals(sourceObject))
        && !_allocatedSourceObjects.Contains(sourceObject))
      {
        throw new ArgumentException("The source-object identity does not belong to this source snapshot.",
          nameof(sourceObject));
      }

      var id = AllocateRenderObjectId();
      _additions.Add(new StaticRenderObjectAddition(
        id,
        sourceObject,
        vertices?.ToArray() ?? throw new ArgumentNullException(nameof(vertices)),
        triangles?.ToArray() ?? throw new ArgumentNullException(nameof(triangles))));
      return id;
    }

    internal SourceObjectId AllocateSourceObjectId()
    {
      EnsureOpen();
      if (!_nextSourceObjectLocalId.HasValue)
      {
        throw new InvalidOperationException("No lineage-local source-object identity remains available.");
      }

      var value = _nextSourceObjectLocalId.Value;
      _nextSourceObjectLocalId = value == int.MaxValue ? null : value + 1;
      var id = new SourceObjectId(_source.LineageId, value);
      _allocatedSourceObjects.Add(id);
      return id;
    }

    internal StaticMeshEditSession ReplacePivot(StaticRenderObjectId renderObject, Vector3 pivot)
    {
      EnsureOpen();
      if (!_source.StaticRenderObjectSequence.Any(item => item.Id.Equals(renderObject))
        && !_additions.Any(item => item.Id.Equals(renderObject)))
      {
        throw new ArgumentException("The render-object identity does not belong to this edit session.",
          nameof(renderObject));
      }
      if (!IsFinite(pivot))
      {
        throw new ArgumentException("The source-object pivot must be finite.", nameof(pivot));
      }

      _replacementPivots[renderObject] = pivot;
      return this;
    }

    internal StaticMeshEditSession ReplaceTexturePathBytes(
      StaticRenderObjectId renderObject,
      IEnumerable<byte> texturePathBytes)
    {
      EnsureOpen();
      var bytes = texturePathBytes?.ToArray()
        ?? throw new ArgumentNullException(nameof(texturePathBytes));
      if (_replacementAdded && renderObject.Equals(_resultId))
      {
        _replacementTexturePathBytes[_source.StaticRenderObjectSequence[0].Id] = bytes;
        return this;
      }
      if (_source.StaticRenderObjectSequence.Any(item => item.Id.Equals(renderObject)))
      {
        _replacementTexturePathBytes[renderObject] = bytes;
        return this;
      }
      var addition = _additions.SingleOrDefault(item => item.Id.Equals(renderObject))
        ?? throw new ArgumentException(
          "The render-object identity does not belong to this edit session.",
          nameof(renderObject));
      addition.SetTexturePathBytes(bytes);
      return this;
    }

    internal StaticMeshEditSession ReplaceAnimation(
      StaticRenderObjectId renderObject,
      IEnumerable<Vector3> scaleFrames,
      IEnumerable<Vector3> translationFrames,
      IEnumerable<Matrix4x4> matrices,
      uint animationClassValue)
    {
      EnsureOpen();
      EnsureSourceId(renderObject);
      _replacementAnimations[renderObject] = new StaticAnimationReplacement(
        new StaticAnimationTracks(scaleFrames, translationFrames, matrices),
        animationClassValue);
      return this;
    }

    internal StaticMeshEditSession ReplaceAnimationLengths(AnimationClassBytes animationLengths)
    {
      EnsureOpen();
      _replacementAnimationLengths = animationLengths;
      return this;
    }

    internal StaticMeshEditSession ReplaceAttachmentRecord(
      int physicalNumber,
      IEnumerable<byte> record)
    {
      EnsureOpen();
      if (physicalNumber is < 1 or > 49)
      {
        throw new ArgumentOutOfRangeException(nameof(physicalNumber));
      }
      var bytes = record?.ToArray() ?? throw new ArgumentNullException(nameof(record));
      if (bytes.Length != 8)
      {
        throw new ArgumentException("An attachment record must contain exactly 8 bytes.", nameof(record));
      }

      _replacementAttachmentRecords[physicalNumber] = bytes;
      return this;
    }

    internal StaticMeshEditSession ReplaceCannonRenderPosition(
      int physicalNumber,
      IEnumerable<byte> record)
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
          nameof(record));
      }

      _replacementCannonRenderPositions[physicalNumber] = bytes;
      return this;
    }

    /// <summary>Sets or explicitly clears one game-authoritative TEX resource binding.</summary>
    public StaticMeshEditSession SetTextureResourceBinding(
      StaticRenderObjectId renderObject,
      string? textureResourceKey)
    {
      var bytes = textureResourceKey is null
        ? Array.Empty<byte>()
        : AuthoringValidation.EncodeCanonicalTextureResourceKey(
          textureResourceKey,
          nameof(textureResourceKey));
      return ReplaceTexturePathBytes(renderObject, bytes);
    }

    internal StaticMeshEditSession ApplyHierarchy(
      StaticSourceObject rootSourceObject,
      IReadOnlyList<StaticRenderObjectId> sequence)
    {
      EnsureOpen();
      _editedRootSourceObject = rootSourceObject
        ?? throw new ArgumentNullException(nameof(rootSourceObject));
      _editedSequence = sequence?.ToArray()
        ?? throw new ArgumentNullException(nameof(sequence));
      return this;
    }

    /// <summary>Commits this session once and returns a new immutable snapshot.</summary>
    public MshEditResult<StaticMeshAsset> Commit(MshOperationProfile? profile = null)
    {
      EnsureOpen();
      _committed = true;
      profile ??= MshOperationProfile.Default;

      if (_invalidSequence || _replacementAdded && !_removed)
      {
        return FailedEdit("StaticRenderObjectSequence",
          "The current safe edit slice requires exactly one final render object.");
      }

      if (_replacementAdded && _source.StaticRenderObjectSequence.Count != 1)
      {
        return FailedEdit("StaticRenderObjectSequence",
          "Removing static render objects requires explicit source-hierarchy authoring.");
      }

      if (_replacementAdded)
      {
        _replacementVertices[_source.StaticRenderObjectSequence[0].Id] = _vertices!;
        _replacementTriangles[_source.StaticRenderObjectSequence[0].Id] = _triangles!;
      }

      foreach (var replacement in _replacementVertices)
      {
        var recordIndex = _source.StaticRenderObjectSequence
          .Select((item, index) => (item, index))
          .Single(item => item.item.Id.Equals(replacement.Key)).index;
        var failure = AuthoringValidation.ValidateStaticForProfile(
          replacement.Value,
          _replacementTriangles[replacement.Key],
          profile,
          $"StaticRenderObjectSequence[{recordIndex}]");
        if (failure is not null)
        {
          return new MshEditResult<StaticMeshAsset>(
            false,
            null,
            new PreservationReport(Array.Empty<PreservationChange>()),
            new[] { failure });
        }
      }

      if (_removedRenderObjects.Count == _source.StaticRenderObjectSequence.Count
        && !_replacementAdded
        && _additions.Count == 0)
      {
        return FailedEdit("StaticRenderObjectSequence",
          "At least one static render object must remain.");
      }

      foreach (var sourceObject in GetSourceObjects(
        _editedRootSourceObject ?? _source.RootSourceObject))
      {
        if (sourceObject.StaticRenderObjectIds.Count == 0
          || sourceObject.StaticRenderObjectIds.All(id => _removedRenderObjects.Contains(id))
            && !_replacementAdded)
        {
          return FailedEdit("StaticRenderObjectSequence",
            "A retained source object must contain at least one static render object.");
        }
      }

      foreach (var addition in _additions)
      {
        var failure = AuthoringValidation.ValidateStaticForProfile(
          addition.Vertices,
          addition.Triangles,
          profile,
          $"StaticRenderObjectSequence[{addition.Id.Value}]");
        if (failure is not null)
        {
          return new MshEditResult<StaticMeshAsset>(
            false,
            null,
            new PreservationReport(Array.Empty<PreservationChange>()),
            new[] { failure });
        }
        if (addition.TexturePathBytes.Count > profile.MaxStaticTexturePathBytes)
        {
          return new MshEditResult<StaticMeshAsset>(
            false,
            null,
            new PreservationReport(Array.Empty<PreservationChange>()),
            new[] { AuthoringValidation.ResourceLimit(
              addition.TexturePathBytes.Count,
              profile.MaxStaticTexturePathBytes) });
        }
      }

      if (_replacementTexturePathBytes.Values.Any(bytes =>
        bytes.Length > profile.MaxStaticTexturePathBytes))
      {
        var actual = _replacementTexturePathBytes.Values.Max(bytes => bytes.Length);
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { AuthoringValidation.ResourceLimit(actual, profile.MaxStaticTexturePathBytes) });
      }

      var bytes = _replacementVertices.Count == 0
        && _replacementPivots.Count == 0
        && _replacementTexturePathBytes.Count == 0
        && _replacementAnimations.Count == 0
        && _replacementAttachmentRecords.Count == 0
        && _replacementCannonRenderPositions.Count == 0
        && !_replacementAnimationLengths.HasValue
        && _removedRenderObjects.Count == 0
        && _additions.Count == 0
        && _editedRootSourceObject is null
        ? _source.GetSerializedRepresentation()
        : MshCanonicalSerializer.RewriteStatic(
          _source,
          _replacementVertices.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<CanonicalStaticVertex>)item.Value),
          _replacementTriangles.ToDictionary(
            item => item.Key,
            item => (IReadOnlyList<CanonicalTriangle>)item.Value),
          _replacementAdded ? Array.Empty<StaticRenderObjectId>() : _removedRenderObjects,
          _additions,
          _editedRootSourceObject,
          _editedSequence,
          _replacementPivots,
          _replacementTexturePathBytes,
          _editedRootSourceObject is not null,
          _replacementAnimations,
          _replacementAnimationLengths,
          _replacementAttachmentRecords,
          _replacementCannonRenderPositions);
      if (bytes.Length > profile.MaxOutputBytes)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) });
      }

      var renderObjectLocalIds = GetResultRenderObjectIds().Select(id => id.Value).ToArray();
      if (_replacementAdded)
      {
        renderObjectLocalIds[0] = _resultId.Value;
      }

      MshDecodeResult decoded;
      try
      {
        decoded = MshV1Decoder.Decode(
          bytes,
          profile,
          CancellationToken.None,
          _source.LineageId,
          _source.Origin,
          rootSourceObjectLocalId: (_editedRootSourceObject ?? _source.RootSourceObject).Id.Value,
          staticRenderObjectLocalIds: renderObjectLocalIds,
          sourceObjectLocalIds: GetSourceObjectIds(
            _editedRootSourceObject ?? _source.RootSourceObject).Select(item => item.Value).ToArray(),
          nextStaticRenderObjectLocalId: _nextLocalId,
          nextSourceObjectLocalId: _nextSourceObjectLocalId);
      }
      catch (MshContentException ex)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { ex.Diagnostic });
      }

      return new MshEditResult<StaticMeshAsset>(
        true,
        (StaticMeshAsset)decoded.Asset,
        CreatePreservationReport((StaticMeshAsset)decoded.Asset),
        decoded.Diagnostics);
    }

    private PreservationReport CreatePreservationReport(StaticMeshAsset edited)
    {
      var changes = new List<PreservationChange>
      {
        Change("ArchiveFraming", PreservationDisposition.Retained, "Unedited"),
        Change("CommonBaseHeader", PreservationDisposition.Retained, "IndependentRepresentation"),
        Change("RootSourceObjectId", PreservationDisposition.Retained, "RetainedSourceObject")
      };
      if (_replacementAnimationLengths.HasValue)
      {
        changes.Add(Change("CommonBaseHeader.AnimationLengths",
          PreservationDisposition.Regenerated, "AnimationEdit"));
      }
      foreach (var physicalNumber in _replacementAttachmentRecords.Keys.OrderBy(value => value))
      {
        var sourceRecord = _source.CommonBaseHeader.AttachmentTable
          .Skip((physicalNumber - 1) * 8).Take(8).ToArray();
        var replacement = _replacementAttachmentRecords[physicalNumber];
        var sourceActive = BinaryPrimitives.ReadInt16LittleEndian(sourceRecord) != short.MinValue;
        var replacementActive = BinaryPrimitives.ReadInt16LittleEndian(replacement) != short.MinValue;
        changes.Add(Change(
          $"CommonBaseHeader.AttachmentTable[{physicalNumber}]",
          sourceActive == replacementActive
            ? PreservationDisposition.Regenerated
            : PreservationDisposition.Canonicalized,
          sourceActive == replacementActive
            ? "AttachmentEdit"
            : replacementActive ? "AttachmentAddition" : "AttachmentDeletion"));
      }
      foreach (var physicalNumber in _replacementCannonRenderPositions.Keys.OrderBy(value => value))
      {
        changes.Add(Change(
          $"CommonBaseHeader.CannonRenderPositions[{physicalNumber}]",
          PreservationDisposition.Regenerated,
          "CannonRenderPositionEdit"));
      }
      if (_editedRootSourceObject is not null)
      {
        changes.Add(Change("StaticRenderObjectSequence", PreservationDisposition.Regenerated,
          "HierarchyEdit"));
        changes.Add(Change("RootSourceObject", PreservationDisposition.Regenerated,
          "HierarchyEdit"));
      }
      if (_replacementAdded)
      {
        changes.Add(Change("StaticRenderObjectSequence[0]", PreservationDisposition.Invalidated,
          "RemovedRenderObject"));
        changes.Add(Change("StaticRenderObjectSequence[0]",
          PreservationDisposition.Canonicalized, "NewRenderObject"));
        changes.Add(Change("StaticRenderObjectSequence[0].TexturePathBytes",
          PreservationDisposition.Canonicalized, "NewMaterialBinding"));
      }
      else
      {
        for (var index = 0; index < _source.StaticRenderObjectSequence.Count; index++)
        {
          var record = _source.StaticRenderObjectSequence[index];
          if (_removedRenderObjects.Contains(record.Id))
          {
            changes.Add(Change($"StaticRenderObjectSequence[{index}]",
              PreservationDisposition.Invalidated, "RemovedRenderObject"));
            continue;
          }
          changes.Add(Change($"StaticRenderObjectSequence[{index}].Id",
            PreservationDisposition.Retained, "RetainedRenderObject"));
          if (_replacementVertices.ContainsKey(record.Id))
          {
            changes.Add(Change($"StaticRenderObjectSequence[{index}].RenderVertices",
              PreservationDisposition.Regenerated, "GeometryEdit"));
            changes.Add(Change($"StaticRenderObjectSequence[{index}].Triangles",
              PreservationDisposition.Regenerated, "GeometryEdit"));
            changes.Add(Change($"StaticRenderObjectSequence[{index}].VertexBlockPadding",
              PreservationDisposition.Canonicalized, "GeometryPacking"));
          }
          if (_replacementPivots.ContainsKey(record.Id))
          {
            changes.Add(Change($"StaticRenderObjectSequence[{index}].Pivot",
              PreservationDisposition.Regenerated, "TransformEdit"));
          }
          if (_replacementTexturePathBytes.ContainsKey(record.Id))
          {
            changes.Add(Change($"StaticRenderObjectSequence[{index}].TexturePathBytes",
              PreservationDisposition.Regenerated, "MaterialBindingEdit"));
          }
          else
          {
            changes.Add(Change($"StaticRenderObjectSequence[{index}].TexturePathBytes",
              PreservationDisposition.Retained, "MaterialBindingReaffirmed"));
          }
          if (_replacementAnimations.ContainsKey(record.Id))
          {
            changes.Add(Change($"StaticRenderObjectSequence[{index}].AnimationTracks.ScaleFrames",
              PreservationDisposition.Regenerated, "AnimationEdit"));
            changes.Add(Change($"StaticRenderObjectSequence[{index}].AnimationTracks.TranslationFrames",
              PreservationDisposition.Regenerated, "AnimationEdit"));
            changes.Add(Change($"StaticRenderObjectSequence[{index}].AnimationTracks.Matrices",
              PreservationDisposition.Regenerated, "AnimationEdit"));
          }
        }

        foreach (var addition in _additions)
        {
          var resultIndex = GetResultRenderObjectIds().ToList().IndexOf(addition.Id);
          changes.Add(Change($"StaticRenderObjectSequence[{resultIndex}]",
            PreservationDisposition.Canonicalized, "NewRenderObject"));
          changes.Add(Change($"StaticRenderObjectSequence[{resultIndex}].TexturePathBytes",
            PreservationDisposition.Canonicalized, "NewMaterialBinding"));
        }

        for (var resultIndex = 0;
          resultIndex < edited.StaticRenderObjectSequence.Count;
          resultIndex++)
        {
          var resultRecord = edited.StaticRenderObjectSequence[resultIndex];
          var sourceRecord = _source.StaticRenderObjectSequence.FirstOrDefault(record =>
            record.Id.Equals(resultRecord.Id));
          if (sourceRecord is not null && sourceRecord.ObjectFlags != resultRecord.ObjectFlags)
          {
            changes.Add(Change($"StaticRenderObjectSequence[{resultIndex}].ObjectFlags",
              PreservationDisposition.Regenerated, "SequenceEdit"));
          }
          if (sourceRecord is not null
            && sourceRecord.NextRecordMarker != resultRecord.NextRecordMarker)
          {
            changes.Add(Change($"StaticRenderObjectSequence[{resultIndex}].NextRecordMarker",
              PreservationDisposition.Regenerated, "SequenceEdit"));
          }
          if (sourceRecord is not null && !_replacementVertices.ContainsKey(sourceRecord.Id))
          {
            for (var vertexIndex = 0;
              vertexIndex < sourceRecord.RenderVertices.Count;
              vertexIndex++)
            {
              AddSharingChange(
                changes,
                resultIndex,
                vertexIndex,
                "NormalSharingIndex",
                sourceRecord.RenderVertices[vertexIndex].NormalSharingIndex,
                resultRecord.RenderVertices[vertexIndex].NormalSharingIndex);
              AddSharingChange(
                changes,
                resultIndex,
                vertexIndex,
                "PositionSharingIndex",
                sourceRecord.RenderVertices[vertexIndex].PositionSharingIndex,
                resultRecord.RenderVertices[vertexIndex].PositionSharingIndex);
            }
          }
        }
        if (_source.StoredTrailingHierarchyUnwindCount != edited.StoredTrailingHierarchyUnwindCount)
        {
          changes.Add(Change("StoredTrailingHierarchyUnwindCount",
            PreservationDisposition.Regenerated, "SequenceEdit"));
        }
      }

      changes.Add(Change("RootTrailingBytes", PreservationDisposition.Retained,
        "IndependentRepresentation"));
      return new PreservationReport(changes);
    }

    private static void AddSharingChange(
      ICollection<PreservationChange> changes,
      int recordIndex,
      int vertexIndex,
      string field,
      ushort sourceValue,
      ushort resultValue)
    {
      if (sourceValue == resultValue)
      {
        return;
      }

      changes.Add(Change(
        $"StaticRenderObjectSequence[{recordIndex}].RenderVertices[{vertexIndex}].{field}",
        resultValue == ushort.MaxValue
          ? PreservationDisposition.Canonicalized
          : PreservationDisposition.Regenerated,
        "GeometryDependency"));
    }

    private MshEditResult<StaticMeshAsset> FailedEdit(string path, string message)
    {
      return new MshEditResult<StaticMeshAsset>(
        false,
        null,
        new PreservationReport(Array.Empty<PreservationChange>()),
        new[] { AuthoringValidation.InvalidEdit(path, message) });
    }

    private static PreservationChange Change(
      string path,
      PreservationDisposition disposition,
      string reason)
    {
      return new PreservationChange(path, disposition, reason);
    }

    private void EnsureSourceId(StaticRenderObjectId id)
    {
      if (!_source.StaticRenderObjectSequence.Any(item => item.Id.Equals(id)))
      {
        throw new ArgumentException("The render-object identity does not belong to this source snapshot.", nameof(id));
      }
    }

    private static IEnumerable<SourceObjectId> GetSourceObjectIds(StaticSourceObject source)
    {
      yield return source.Id;
      foreach (var child in source.Children)
      {
        foreach (var id in GetSourceObjectIds(child))
        {
          yield return id;
        }
      }
    }

    private IEnumerable<StaticRenderObjectId> GetResultRenderObjectIds()
    {
      if (_replacementAdded)
      {
        yield return _resultId;
        yield break;
      }

      if (_editedSequence is not null)
      {
        foreach (var id in _editedSequence)
        {
          yield return id;
        }
        yield break;
      }

      foreach (var id in MshCanonicalSerializer.PlanStaticRenderObjectIds(
        _source,
        _removedRenderObjects,
        _additions))
      {
        yield return id;
      }
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

    private StaticRenderObjectId AllocateRenderObjectId()
    {
      if (!_nextLocalId.HasValue)
      {
        throw new InvalidOperationException("No lineage-local static render-object identity remains available.");
      }

      var value = _nextLocalId.Value;
      _nextLocalId = value == int.MaxValue ? null : value + 1;
      return new StaticRenderObjectId(_source.LineageId, value);
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

  internal static class AuthoringValidation
  {
    internal static OperationDiagnostic? ValidateStaticTree(
      CanonicalStaticSourceObject? root,
      MshOperationProfile profile)
    {
      if (root is null)
      {
        return Invalid("RootSourceObject", "A canonical root source object is required.");
      }

      var renderObjectCount = 0;
      return ValidateStaticSourceObject(root, "RootSourceObject", 1, profile, ref renderObjectCount);
    }

    internal static OperationDiagnostic? ValidateDynamic(
      CanonicalDynamicObject root,
      MshOperationProfile profile,
      out IReadOnlyList<OperationDiagnostic> diagnostics)
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
        ref stringBytes);
      if (warnings.Count > profile.MaxDiagnostics)
      {
        warnings.RemoveRange(profile.MaxDiagnostics - 1, warnings.Count - profile.MaxDiagnostics + 1);
        warnings.Add(new OperationDiagnostic(
          MshDiagnosticCodes.DiagnosticsTruncated,
          1010,
          DiagnosticSeverity.Warning,
          "$",
          "Additional diagnostics were suppressed by the operation profile."));
      }

      diagnostics = warnings.AsReadOnly();
      return failure;
    }

    internal static OperationDiagnostic? ValidateStatic(
      IReadOnlyList<CanonicalStaticVertex>? vertices,
      IReadOnlyList<CanonicalTriangle>? triangles)
    {
      if (vertices is null || triangles is null)
      {
        return Invalid("StaticRenderObject", "Canonical static geometry is required.");
      }

      if (vertices.Count == 0 || vertices.Count > 65536 || triangles.Count == 0)
      {
        return Invalid("StaticRenderObject", "Canonical static geometry counts are outside the supported format range.");
      }

      for (var index = 0; index < vertices.Count; index++)
      {
        var vertex = vertices[index];
        if (!IsFinite(vertex.Position))
        {
          return Invalid($"StaticRenderObject.RenderVertices[{index}].Position", "Position must be finite.");
        }

        if (!IsFinite(vertex.Normal))
        {
          return Invalid($"StaticRenderObject.RenderVertices[{index}].Normal", "Normal must be finite.");
        }

        if (!IsFinite(vertex.TextureCoordinate))
        {
          return Invalid($"StaticRenderObject.RenderVertices[{index}].TextureCoordinate",
            "Texture coordinate must be finite.");
        }
      }

      var maximumZ = vertices.Max(vertex => vertex.Position.Z);
      var extent = vertices.SelectMany(vertex => new[]
      {
        Math.Max(0, vertex.Position.X),
        Math.Max(0, -vertex.Position.X),
        Math.Max(0, vertex.Position.Y),
        Math.Max(0, -vertex.Position.Y)
      }).Max();
      if (maximumZ < 0 || maximumZ * 256d > ushort.MaxValue || extent * 256d > ushort.MaxValue)
      {
        return Invalid("CommonBaseHeader", "Derived footprint or horizontal extents are out of range.");
      }

      for (var index = 0; index < triangles.Count; index++)
      {
        var triangle = triangles[index];
        if (triangle.Vertex0 >= vertices.Count
          || triangle.Vertex1 >= vertices.Count
          || triangle.Vertex2 >= vertices.Count)
        {
          return Invalid(
            $"StaticRenderObject.Triangles[{index}]",
            "Triangle indices must reference active vertices.");
        }
      }

      return null;
    }

    internal static OperationDiagnostic? ValidateStaticForProfile(
      IReadOnlyList<CanonicalStaticVertex>? vertices,
      IReadOnlyList<CanonicalTriangle>? triangles,
      MshOperationProfile profile,
      string path)
    {
      var failure = ValidateStatic(vertices, triangles);
      if (failure is not null || vertices is null || triangles is null)
      {
        return failure;
      }

      if (vertices.Count > profile.MaxStaticVerticesPerObject)
      {
        return ResourceLimit(vertices.Count, profile.MaxStaticVerticesPerObject, path + ".RenderVertices");
      }

      var blockCount = (vertices.Count + 3) / 4;
      if (blockCount > profile.MaxStaticVertexBlocksPerObject)
      {
        return ResourceLimit(blockCount, profile.MaxStaticVertexBlocksPerObject, path + ".VertexBlockCount");
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
      ref int renderObjectCount)
    {
      if (depth > profile.MaxStaticHierarchyDepth)
      {
        return ResourceLimit(depth, profile.MaxStaticHierarchyDepth, path);
      }

      if (source.RenderObjects.Count == 0)
      {
        return Invalid(path + ".RenderObjects", "Every canonical source object requires a material partition.");
      }

      foreach (var renderObject in source.RenderObjects)
      {
        renderObjectCount++;
        if (renderObjectCount > profile.MaxStaticRenderObjects)
        {
          return ResourceLimit(renderObjectCount, profile.MaxStaticRenderObjects, "StaticRenderObjectSequence");
        }

        var failure = ValidateStaticForProfile(
          renderObject.RenderVertices,
          renderObject.Triangles,
          profile,
          path + ".RenderObjects");
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
              "TEX resource keys must use safe Textures\\...\\*.tex spelling.");
          }
          var byteCount = Encoding.ASCII.GetByteCount(renderObject.TextureResourceKey);
          if (byteCount > profile.MaxStaticTexturePathBytes)
          {
            return ResourceLimit(
              byteCount,
              profile.MaxStaticTexturePathBytes,
              path + ".RenderObjects.TextureResourceKey");
          }
        }
      }

      for (var index = 0; index < source.Children.Count; index++)
      {
        var failure = ValidateStaticSourceObject(
          source.Children[index],
          path + ".Children[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
          depth + 1,
          profile,
          ref renderObjectCount);
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
          parameterName);
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
        message);
    }

    internal static OperationDiagnostic InvalidEdit(string path, string message)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.InvalidEdit,
        1012,
        DiagnosticSeverity.Error,
        path,
        message);
    }

    internal static OperationDiagnostic ResourceLimit(long actual, int maximum)
    {
      return CreateResourceLimit(
        "$",
        "The serialized representation exceeds the configured operation profile.",
        actual,
        maximum);
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
      ref int stringBytes)
    {
      if (!ancestors.Add(current))
      {
        return Invalid(path, "A canonical dynamic tree cannot contain ancestor or self cycles.");
      }

      if (!seen.Add(current))
      {
        diagnostics.Add(new OperationDiagnostic(
          MshDiagnosticCodes.CompatibilityAnomaly,
          1009,
          DiagnosticSeverity.Warning,
          path,
          "A reused draft instance will be serialized as an independent dynamic object."));
      }

      if (!Enum.IsDefined(typeof(DynamicEffectType), current.EffectType))
      {
        return Invalid(path + ".EffectType", "Canonical authoring requires a recognized dynamic effect.");
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
          path + ".Children");
      }

      var recipeFailure = ValidateDynamicRecipe(current, path, depth == 1, profile, ref stringBytes);
      if (recipeFailure is not null)
      {
        return recipeFailure;
      }

      for (var index = 0; index < current.Children.Count; index++)
      {
        var failure = ValidateDynamicObject(
          current.Children[index],
          path + ".Children[" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
          depth + 1,
          profile,
          ancestors,
          seen,
          diagnostics,
          ref objectCount,
          ref stringBytes);
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
      bool isRoot,
      MshOperationProfile profile,
      ref int stringBytes)
    {
      var recipe = current.Recipe;
      if (!Enum.IsDefined(typeof(DynamicLightType), recipe.LightType))
      {
        return Invalid(path + ".Extension.LightType", "Canonical authoring requires a recognized light type.");
      }

      if (!Enum.IsDefined(typeof(DynamicAlphaTiming), recipe.AlphaTiming))
      {
        return Invalid(path + ".Extension.AlphaTimingMode", "Canonical authoring requires a recognized alpha timing mode.");
      }

      if (!IsFinite(recipe.StartEffectRectangle) || !IsFinite(recipe.EndEffectRectangle)
        || !IsFinite(recipe.EffectDepthOffset)
        || !IsFinite(recipe.RibbonHalfWidth)
        || !IsFinite(recipe.TerrainLightColor)
        || !IsFinite(recipe.VisibleEffectColor)
        || !IsFinite(recipe.VisibleTerrainLightGain)
        || !IsFinite(recipe.StartAlpha)
        || !IsFinite(recipe.EndAlpha)
        || !IsFinite(recipe.StartModelScale)
        || !IsFinite(recipe.EndModelScale)
        || !IsFinite(recipe.ChildStartTranslation)
        || !IsFinite(recipe.ChildEndTranslation))
      {
        return Invalid(path + ".Extension", "Canonical dynamic numeric inputs must be finite.");
      }

      if (isRoot && (recipe.ChildStartTranslation != Vector3.Zero
        || recipe.ChildEndTranslation != Vector3.Zero))
      {
        return Invalid(path + ".Extension.ChildTranslation",
          "A canonical root dynamic object cannot apply its own child translation.");
      }

      var usesFrames = recipe.EffectType is not DynamicEffectType.Group and not DynamicEffectType.Sphere;
      if (usesFrames
        && (recipe.FirstSourceFrame < 0 || recipe.FrameCount <= 0 || recipe.FramePeriodTicks < 0))
      {
        return Invalid(path + ".Extension.Frames", "Canonical frame values are outside the supported domain.");
      }

      var usesSpriteSheet = recipe.EffectType is DynamicEffectType.Explosion
        or DynamicEffectType.FlatExplosion
        or DynamicEffectType.Laser
        or DynamicEffectType.LaserWall
        or DynamicEffectType.Shockwave
        or DynamicEffectType.Line
        or DynamicEffectType.ElectricalCannon
        or DynamicEffectType.Lightning
        or DynamicEffectType.Smoke
        or DynamicEffectType.Keelwater;
      if (usesSpriteSheet)
      {
        if (recipe.SpriteSheetColumnCount <= 0 || recipe.SpriteSheetRowCount <= 0)
        {
          return Invalid(path + ".Extension.SpriteSheet", "Canonical sprite-sheet dimensions must be positive.");
        }

        try
        {
          if (checked(recipe.FirstSourceFrame + recipe.FrameCount)
            > checked(recipe.SpriteSheetColumnCount * recipe.SpriteSheetRowCount))
          {
            return Invalid(path + ".Extension.SpriteSheet", "Canonical frames must fit in the sprite sheet.");
          }
        }
        catch (OverflowException)
        {
          return Invalid(path + ".Extension.SpriteSheet", "Canonical sprite-sheet bounds overflow.");
        }
      }

      if ((recipe.EffectType is DynamicEffectType.Laser
          or DynamicEffectType.LaserWall
          or DynamicEffectType.ElectricalCannon
          or DynamicEffectType.Lightning)
        && recipe.RibbonHalfWidth == 0)
      {
        return Invalid(path + ".Extension.RibbonHalfWidth",
          "Canonical ribbon half-width must be nonzero and retains its sign.");
      }

      if (recipe.EffectType == DynamicEffectType.ScalableObject
        && string.IsNullOrEmpty(recipe.MeshResourceKey))
      {
        return Invalid(path + ".Extension.MeshNameBytes", "ScalableObject requires a mesh resource key.");
      }

      if (recipe.EffectType != DynamicEffectType.Group
        && string.IsNullOrEmpty(recipe.TextureResourceKey))
      {
        return Invalid(path + ".Extension.TexturePathBytes", "The selected effect requires a texture resource key.");
      }

      if (!string.IsNullOrEmpty(recipe.TextureResourceKey)
        && !IsCanonicalTextureResourceKey(recipe.TextureResourceKey))
      {
        return Invalid(path + ".Extension.TexturePathBytes", "Texture resource keys must use safe Textures\\...\\*.tex spelling.");
      }

      try
      {
        var meshBytes = MshCanonicalSerializer.EncodeDynamicString(recipe.MeshResourceKey).Length;
        var textureBytes = MshCanonicalSerializer.EncodeDynamicString(recipe.TextureResourceKey).Length;
        stringBytes = checked(stringBytes + meshBytes + textureBytes);
      }
      catch (EncoderFallbackException)
      {
        return Invalid(path + ".Extension", "Dynamic resource keys must be representable as ISO-8859-2 bytes.");
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
      if (!value.StartsWith("Textures\\", StringComparison.OrdinalIgnoreCase)
        || !value.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)
        || value.Contains('/')
        || value.Contains(':')
        || value.Contains('?')
        || value.Contains('#')
        || value.Any(character => character > 0x7F
          || char.IsControl(character)
          || character is '*' or '"' or '<' or '>' or '|'))
      {
        return false;
      }

      var segments = value.Split('\\');
      return segments.Length >= 2
        && segments[^1].Length > 4
        && segments.All(segment => segment.Length > 0
          && segment is not "." and not ".."
          && !segment.EndsWith(" ", StringComparison.Ordinal)
          && !segment.EndsWith(".", StringComparison.Ordinal));
    }

    private static bool IsFinite(EffectRectangle value)
    {
      return IsFinite(value.X0)
        && IsFinite(value.Y1)
        && IsFinite(value.X1)
        && IsFinite(value.Y0);
    }

    private static OperationDiagnostic ResourceLimit(long actual, int maximum, string path)
    {
      return CreateResourceLimit(
        path,
        "The canonical dynamic tree exceeds the configured operation profile.",
        actual,
        maximum);
    }

    private static OperationDiagnostic CreateResourceLimit(
      string path,
      string message,
      long actual,
      int maximum)
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
          ["maximum"] = maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
    }

    private static OperationDiagnostic Unsupported(string domain)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.UnsupportedDomain,
        1005,
        DiagnosticSeverity.Error,
        "StaticRenderObject",
        "The requested geometry is outside the current safe MSH slice.",
        data: new Dictionary<string, string> { ["domain"] = domain });
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
