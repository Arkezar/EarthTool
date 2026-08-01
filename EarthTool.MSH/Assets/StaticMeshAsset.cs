#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace EarthTool.MSH.Assets
{
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
    /// <summary>Gets the preserved archive framing.</summary>
    public MeshArchiveFraming ArchiveFraming { get; }

    internal MeshAsset(MeshArchiveFraming archiveFraming)
    {
      ArchiveFraming = archiveFraming;
    }
  }

  /// <summary>Represents an immutable static MSH asset.</summary>
  public sealed class StaticMeshAsset : MeshAsset
  {
    private readonly byte[] _serializedRepresentation;

    /// <summary>Gets the authoritative static render-object sequence.</summary>
    public IReadOnlyList<StaticRenderObject> StaticRenderObjectSequence { get; }

    internal StaticMeshAsset(
      MeshArchiveFraming archiveFraming,
      IEnumerable<StaticRenderObject> staticRenderObjectSequence,
      byte[] serializedRepresentation)
      : base(archiveFraming)
    {
      StaticRenderObjectSequence = Array.AsReadOnly(
        new List<StaticRenderObject>(staticRenderObjectSequence).ToArray());
      _serializedRepresentation = (byte[])serializedRepresentation.Clone();
    }

    internal byte[] GetSerializedRepresentation()
    {
      return (byte[])_serializedRepresentation.Clone();
    }
  }

  /// <summary>Represents one immutable static render object.</summary>
  public sealed class StaticRenderObject
  {
    /// <summary>Gets the lineage-local render-object identity.</summary>
    public int LocalId { get; }

    /// <summary>Gets the ordered active render vertices.</summary>
    public IReadOnlyList<RenderVertex> RenderVertices { get; }

    /// <summary>Gets the ordered triangles.</summary>
    public IReadOnlyList<StaticTriangle> Triangles { get; }

    internal StaticRenderObject(
      int localId,
      IEnumerable<RenderVertex> renderVertices,
      IEnumerable<StaticTriangle> triangles)
    {
      LocalId = localId;
      RenderVertices = Array.AsReadOnly(new List<RenderVertex>(renderVertices).ToArray());
      Triangles = Array.AsReadOnly(new List<StaticTriangle>(triangles).ToArray());
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
