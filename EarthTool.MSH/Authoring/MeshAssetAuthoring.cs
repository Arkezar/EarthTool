#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
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
    private CanonicalTriangle[]? _triangles;
    private CanonicalStaticVertex[]? _vertices;

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

    /// <summary>Sets the one render object supported by the current static authoring slice.</summary>
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

      _vertices = vertices.ToArray();
      _triangles = triangles.ToArray();
      return this;
    }

    /// <summary>Builds one immutable canonical snapshot.</summary>
    public MshBuildResult<StaticMeshAsset> Build(MshOperationProfile? profile = null)
    {
      profile ??= MshOperationProfile.Default;
      var failure = AuthoringValidation.ValidateStatic(_vertices, _triangles);
      if (failure is not null)
      {
        return new MshBuildResult<StaticMeshAsset>(false, null, new[] { failure });
      }

      try
      {
        var bytes = MshCanonicalSerializer.CreateStatic(
          _creationGuid,
          _animationLengths,
          _vertices!,
          _triangles!);
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
    }
  }

  /// <summary>Builds the childless Group supported by the current dynamic authoring slice.</summary>
  public sealed class DynamicMeshBuilder
  {
    private readonly Guid _creationGuid;
    private readonly MeshAssetLineageId _lineageId;

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

    /// <summary>Builds one immutable canonical childless Group snapshot.</summary>
    public MshBuildResult<DynamicMeshAsset> Build(MshOperationProfile? profile = null)
    {
      profile ??= MshOperationProfile.Default;
      var bytes = MshCanonicalSerializer.CreateDynamic(_creationGuid);
      if (bytes.Length > profile.MaxOutputBytes)
      {
        return new MshBuildResult<DynamicMeshAsset>(false, null,
          new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) });
      }

      var decoded = MshV1Decoder.Decode(
        bytes,
        profile,
        CancellationToken.None,
        _lineageId,
        MeshAssetOrigin.Canonical);
      return new MshBuildResult<DynamicMeshAsset>(
        true,
        (DynamicMeshAsset)decoded.Asset,
        decoded.Diagnostics);
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

  /// <summary>Accumulates one atomic set of static edits and commits at most once.</summary>
  public sealed class StaticMeshEditSession
  {
    private readonly StaticMeshAsset _source;
    private StaticRenderObjectId _resultId;
    private CanonicalTriangle[]? _triangles;
    private CanonicalStaticVertex[]? _vertices;
    private bool _committed;
    private bool _invalidSequence;
    private bool _removed;
    private bool _replacementAdded;
    private int _nextLocalId;

    internal StaticMeshEditSession(StaticMeshAsset source)
    {
      _source = source;
      _resultId = source.StaticRenderObjectSequence[0].Id;
      _nextLocalId = source.StaticRenderObjectSequence.Max(item => item.Id.Value) + 1;
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

      _vertices = vertices?.ToArray() ?? throw new ArgumentNullException(nameof(vertices));
      _triangles = triangles?.ToArray() ?? throw new ArgumentNullException(nameof(triangles));
      return this;
    }

    /// <summary>Removes the current render object before a replacement object is added.</summary>
    public StaticMeshEditSession RemoveRenderObject(StaticRenderObjectId renderObject)
    {
      EnsureOpen();
      EnsureSourceId(renderObject);
      if (_removed || _replacementAdded)
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
      _resultId = new StaticRenderObjectId(
        _source.LineageId,
        checked(_nextLocalId++));
      _replacementAdded = true;
      return _resultId;
    }

    /// <summary>Commits this session once and returns a new immutable snapshot.</summary>
    public MshEditResult<StaticMeshAsset> Commit(MshOperationProfile? profile = null)
    {
      EnsureOpen();
      _committed = true;
      profile ??= MshOperationProfile.Default;

      if (_invalidSequence || _removed != _replacementAdded)
      {
        return FailedEdit("StaticRenderObjectSequence",
          "The current safe edit slice requires exactly one final render object.");
      }

      var vertices = _vertices ?? _source.StaticRenderObjectSequence[0].RenderVertices
        .Select(vertex => new CanonicalStaticVertex(vertex.Position, vertex.Normal, vertex.TextureCoordinate))
        .ToArray();
      var triangles = _triangles ?? _source.StaticRenderObjectSequence[0].Triangles
        .Select(triangle => new CanonicalTriangle(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2))
        .ToArray();
      var failure = AuthoringValidation.ValidateStatic(vertices, triangles);
      if (failure is not null)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { failure });
      }

      var bytes = _vertices is null
        ? _source.GetSerializedRepresentation()
        : MshCanonicalSerializer.RewriteStatic(_source, vertices, triangles);
      if (bytes.Length > profile.MaxOutputBytes)
      {
        return new MshEditResult<StaticMeshAsset>(
          false,
          null,
          new PreservationReport(Array.Empty<PreservationChange>()),
          new[] { AuthoringValidation.ResourceLimit(bytes.Length, profile.MaxOutputBytes) });
      }

      var decoded = MshV1Decoder.Decode(
        bytes,
        profile,
        CancellationToken.None,
        _source.LineageId,
        _source.Origin,
        _resultId.Value,
        _source.RootSourceObjectId.Value);
      return new MshEditResult<StaticMeshAsset>(
        true,
        (StaticMeshAsset)decoded.Asset,
        CreatePreservationReport(),
        decoded.Diagnostics);
    }

    private PreservationReport CreatePreservationReport()
    {
      var changes = new List<PreservationChange>
      {
        Change("ArchiveFraming", PreservationDisposition.Retained, "Unedited"),
        Change("CommonBaseHeader", PreservationDisposition.Retained, "IndependentRepresentation"),
        Change("RootSourceObjectId", PreservationDisposition.Retained, "RetainedSourceObject")
      };
      if (_removed)
      {
        changes.Add(Change("StaticRenderObjectSequence[0]", PreservationDisposition.Invalidated,
          "RemovedRenderObject"));
        changes.Add(Change("StaticRenderObjectSequence[0]",
          PreservationDisposition.Canonicalized, "NewRenderObject"));
      }
      else
      {
        changes.Add(Change("StaticRenderObjectSequence[0].Id", PreservationDisposition.Retained,
          "RetainedRenderObject"));
        if (_vertices is not null)
        {
          changes.Add(Change("StaticRenderObjectSequence[0].RenderVertices",
            PreservationDisposition.Regenerated, "GeometryEdit"));
          changes.Add(Change("StaticRenderObjectSequence[0].Triangles",
            PreservationDisposition.Regenerated, "GeometryEdit"));
          changes.Add(Change("StaticRenderObjectSequence[0].VertexBlockPadding",
            PreservationDisposition.Canonicalized, "GeometryPacking"));
        }
      }

      changes.Add(Change("RootTrailingBytes", PreservationDisposition.Retained,
        "IndependentRepresentation"));
      return new PreservationReport(changes);
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
    internal static OperationDiagnostic? ValidateStatic(
      IReadOnlyList<CanonicalStaticVertex>? vertices,
      IReadOnlyList<CanonicalTriangle>? triangles)
    {
      if (vertices is null || triangles is null)
      {
        return Invalid("StaticRenderObject", "Canonical static geometry is required.");
      }

      if (vertices.Count != 3 || triangles.Count != 1)
      {
        return Unsupported("Geometry");
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

      var triangle = triangles[0];
      if (triangle.Vertex0 >= vertices.Count
        || triangle.Vertex1 >= vertices.Count
        || triangle.Vertex2 >= vertices.Count)
      {
        return Invalid("StaticRenderObject.Triangles[0]", "Triangle indices must reference active vertices.");
      }

      return null;
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
      return new OperationDiagnostic(
        MshDiagnosticCodes.ResourceLimitExceeded,
        1004,
        DiagnosticSeverity.Error,
        "$",
        "The serialized representation exceeds the configured operation profile.",
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
