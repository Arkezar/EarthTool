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
      if (_rootSourceObject is null)
      {
        return new MshBuildResult<StaticMeshAsset>(
          false,
          null,
          new[]
          {
            AuthoringValidation.Invalid(
              "RootSourceObject",
              "A canonical root source object is required."
            ),
          }
        );
      }

      var renderObjects = CanonicalStaticRenderObjectSequenceEncoder
        .GetCanonicalSequence(_rootSourceObject);
      var resourceBindings = renderObjects
        .Select((renderObject, ordinal) => (renderObject, ordinal))
        .ToDictionary(item => item.ordinal, item => item.renderObject.TextureResourceKey);

      return CanonicalStaticMeshAssembler.Assemble(
        new CanonicalStaticMeshAssemblyInput(
          _creationGuid,
          new CanonicalStaticBaseHeaderInput(
            _animationLengths,
            _rootSourceObject.RenderObjects.SelectMany(renderObject =>
              renderObject.RenderVertices
            ),
            _footprint,
            _horizontalExtents
          ),
          _rootSourceObject,
          textureResourceBindings: resourceBindings
        ),
        profile
      );
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
      IReadOnlyList<CanonicalStaticVertex> vertices,
      CanonicalStaticFootprint? footprint,
      CanonicalHorizontalExtents? horizontalExtents
    )
    {
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
