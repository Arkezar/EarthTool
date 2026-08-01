#nullable enable

using EarthTool.MSH.Assets;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EarthTool.GLTF.Internal
{
  internal sealed class ParsedGlb
  {
    internal string ManifestMetadata { get; }

    internal string MeshMetadata { get; }

    internal IReadOnlyList<RenderVertex> Vertices { get; }

    internal StaticTriangle Triangle { get; }

    internal ParsedGlb(
      string manifestMetadata,
      string meshMetadata,
      IReadOnlyList<RenderVertex> vertices,
      StaticTriangle triangle)
    {
      ManifestMetadata = manifestMetadata;
      MeshMetadata = meshMetadata;
      Vertices = vertices;
      Triangle = triangle;
    }
  }

  internal sealed class MetadataEnvelope
  {
    internal Guid AssetLineageId { get; }

    internal Guid DocumentId { get; }

    internal string ScopeKind { get; }

    internal int LocalId { get; }

    internal string? SourceMsh { get; }

    internal string? Fingerprint { get; }

    internal string? FingerprintName { get; }

    internal int? FingerprintVersion { get; }

    internal MetadataEnvelope(
      Guid assetLineageId,
      Guid documentId,
      string scopeKind,
      int localId,
      string? sourceMsh,
      string? fingerprint,
      string? fingerprintName,
      int? fingerprintVersion)
    {
      AssetLineageId = assetLineageId;
      DocumentId = documentId;
      ScopeKind = scopeKind;
      LocalId = localId;
      SourceMsh = sourceMsh;
      Fingerprint = fingerprint;
      FingerprintName = fingerprintName;
      FingerprintVersion = fingerprintVersion;
    }
  }

  internal static class GlbDocument
  {
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunkType = 0x4E4F534A;
    private const uint BinaryChunkType = 0x004E4942;

    internal static byte[] Create(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      out NativeProjectionFingerprint fingerprint)
    {
      var renderObject = asset.StaticRenderObjectSequence.Single();
      var projectedVertices = renderObject.RenderVertices.Select(ProjectToGltf).ToArray();
      var triangle = renderObject.Triangles.Single();
      var binary = CreateBinary(projectedVertices, triangle);
      fingerprint = StaticGeometryFingerprint.Create(
        baseline,
        renderObject.LocalId,
        projectedVertices,
        triangle);
      var manifest = CreateMetadata(
        baseline,
        "manifest",
        0,
        Convert.ToBase64String(asset.GetSerializedRepresentation()),
        null);
      var meshMetadata = CreateMetadata(baseline, "mesh", renderObject.LocalId, null, fingerprint.Sha256);
      var json = CreateJson(projectedVertices, manifest, meshMetadata);
      return Pack(json, binary);
    }

    internal static ParsedGlb Parse(byte[] glb, int maxJsonDepth)
    {
      if (glb.Length < 28
        || ReadUInt32(glb, 0) != GlbMagic
        || ReadUInt32(glb, 4) != 2
        || ReadUInt32(glb, 8) != glb.Length)
      {
        throw new InvalidDataException("Invalid GLB header.");
      }

      var jsonLength = checked((int)ReadUInt32(glb, 12));
      if (ReadUInt32(glb, 16) != JsonChunkType || 20 + jsonLength + 8 > glb.Length)
      {
        throw new InvalidDataException("Invalid GLB JSON chunk.");
      }

      var binaryHeader = 20 + jsonLength;
      var binaryLength = checked((int)ReadUInt32(glb, binaryHeader));
      if (ReadUInt32(glb, binaryHeader + 4) != BinaryChunkType
        || binaryHeader + 8 + binaryLength != glb.Length)
      {
        throw new InvalidDataException("Invalid GLB binary chunk.");
      }

      var documentOptions = new JsonDocumentOptions
      {
        MaxDepth = maxJsonDepth,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
      };
      using var document = JsonDocument.Parse(
        glb.AsMemory(20, jsonLength),
        documentOptions);
      var root = document.RootElement;
      ValidateSupportedGraph(root);
      ModelRoot.ParseGLB(
        new ArraySegment<byte>(glb),
        new ReadSettings { Validation = ValidationMode.Strict });
      var manifest = GetMetadata(root.GetProperty("scenes")[0], "scene");
      var meshMetadata = GetMetadata(root.GetProperty("meshes")[0], "mesh");
      var binary = glb.AsSpan(binaryHeader + 8, binaryLength);
      var vertices = ReadVertices(root, binary);
      var triangle = ReadTriangle(root, binary, vertices.Count);
      return new ParsedGlb(manifest, meshMetadata, vertices, triangle);
    }

    internal static MetadataEnvelope ParseMetadata(string value, int maxMetadataBytes, int maxJsonDepth)
    {
      if (Encoding.UTF8.GetByteCount(value) > maxMetadataBytes)
      {
        throw new InvalidDataException("EarthTool metadata exceeds the configured limit.");
      }

      try
      {
        using var document = JsonDocument.Parse(value, new JsonDocumentOptions { MaxDepth = maxJsonDepth });
        var root = document.RootElement;
        if (root.GetProperty("format").GetString() != "earthtool.msh.gltf")
        {
          throw new MalformedMetadataException("Unsupported EarthTool metadata format.");
        }

        if (root.GetProperty("version").GetInt32() != 1)
        {
          throw new UnsupportedMetadataVersionException();
        }

        var scope = root.GetProperty("scope");
        var hasProjection = root.TryGetProperty("nativeProjection", out var projection);
        return new MetadataEnvelope(
          root.GetProperty("assetLineage").GetGuid(),
          root.GetProperty("document").GetGuid(),
          scope.GetProperty("kind").GetString() ?? throw new MalformedMetadataException("Missing scope kind."),
          scope.GetProperty("localId").GetInt32(),
          root.TryGetProperty("sourceMsh", out var sourceMsh) ? sourceMsh.GetString() : null,
          hasProjection ? projection.GetProperty("sha256").GetString() : null,
          hasProjection ? projection.GetProperty("name").GetString() : null,
          hasProjection ? projection.GetProperty("version").GetInt32() : null);
      }
      catch (UnsupportedMetadataVersionException)
      {
        throw;
      }
      catch (MalformedMetadataException)
      {
        throw;
      }
      catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
      {
        throw new MalformedMetadataException("Malformed EarthTool metadata.", ex);
      }
    }

    private static byte[] CreateBinary(IReadOnlyList<RenderVertex> vertices, StaticTriangle triangle)
    {
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
      foreach (var vertex in vertices)
      {
        writer.Write(vertex.Position.X);
        writer.Write(vertex.Position.Y);
        writer.Write(vertex.Position.Z);
      }

      foreach (var vertex in vertices)
      {
        writer.Write(vertex.Normal.X);
        writer.Write(vertex.Normal.Y);
        writer.Write(vertex.Normal.Z);
      }

      foreach (var vertex in vertices)
      {
        writer.Write(vertex.TextureCoordinate.X);
        writer.Write(vertex.TextureCoordinate.Y);
      }

      writer.Write(triangle.Vertex0);
      writer.Write(triangle.Vertex1);
      writer.Write(triangle.Vertex2);
      while (stream.Length % 4 != 0)
      {
        writer.Write((byte)0);
      }

      return stream.ToArray();
    }

    private static byte[] CreateJson(
      IReadOnlyList<RenderVertex> vertices,
      string manifest,
      string meshMetadata)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        writer.WriteStartObject();
        writer.WriteStartObject("asset");
        writer.WriteString("version", "2.0");
        writer.WriteString("generator", "EarthTool");
        writer.WriteEndObject();
        writer.WriteNumber("scene", 0);
        writer.WriteStartArray("scenes");
        writer.WriteStartObject();
        writer.WriteStartArray("nodes");
        writer.WriteNumberValue(0);
        writer.WriteEndArray();
        WriteExtras(writer, manifest);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("nodes");
        writer.WriteStartObject();
        writer.WriteString("name", "Static object 1");
        writer.WriteNumber("mesh", 0);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("meshes");
        writer.WriteStartObject();
        writer.WriteString("name", "Static mesh 1");
        writer.WriteStartArray("primitives");
        writer.WriteStartObject();
        writer.WriteStartObject("attributes");
        writer.WriteNumber("POSITION", 0);
        writer.WriteNumber("NORMAL", 1);
        writer.WriteNumber("TEXCOORD_0", 2);
        writer.WriteEndObject();
        writer.WriteNumber("indices", 3);
        writer.WriteNumber("mode", 4);
        writer.WriteEndObject();
        writer.WriteEndArray();
        WriteExtras(writer, meshMetadata);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("buffers");
        writer.WriteStartObject();
        writer.WriteNumber("byteLength", 102);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("bufferViews");
        WriteBufferView(writer, 0, 36, 34962);
        WriteBufferView(writer, 36, 36, 34962);
        WriteBufferView(writer, 72, 24, 34962);
        WriteBufferView(writer, 96, 6, 34963);
        writer.WriteEndArray();
        writer.WriteStartArray("accessors");
        WriteVectorAccessor(writer, 0, "VEC3", vertices.Select(vertex => vertex.Position));
        WriteAccessor(writer, 1, 5126, 3, "VEC3");
        WriteAccessor(writer, 2, 5126, 3, "VEC2");
        WriteAccessor(writer, 3, 5123, 3, "SCALAR");
        writer.WriteEndArray();
        writer.WriteEndObject();
      }

      return stream.ToArray();
    }

    private static void WriteExtras(Utf8JsonWriter writer, string metadata)
    {
      writer.WriteStartObject("extras");
      writer.WriteString("earthtool", metadata);
      writer.WriteEndObject();
    }

    private static void WriteBufferView(Utf8JsonWriter writer, int offset, int length, int target)
    {
      writer.WriteStartObject();
      writer.WriteNumber("buffer", 0);
      writer.WriteNumber("byteOffset", offset);
      writer.WriteNumber("byteLength", length);
      writer.WriteNumber("target", target);
      writer.WriteEndObject();
    }

    private static void WriteVectorAccessor(
      Utf8JsonWriter writer,
      int bufferView,
      string type,
      IEnumerable<Vector3> values)
    {
      var vectors = values.ToArray();
      writer.WriteStartObject();
      writer.WriteNumber("bufferView", bufferView);
      writer.WriteNumber("componentType", 5126);
      writer.WriteNumber("count", vectors.Length);
      writer.WriteString("type", type);
      writer.WriteStartArray("min");
      writer.WriteNumberValue(vectors.Min(value => value.X));
      writer.WriteNumberValue(vectors.Min(value => value.Y));
      writer.WriteNumberValue(vectors.Min(value => value.Z));
      writer.WriteEndArray();
      writer.WriteStartArray("max");
      writer.WriteNumberValue(vectors.Max(value => value.X));
      writer.WriteNumberValue(vectors.Max(value => value.Y));
      writer.WriteNumberValue(vectors.Max(value => value.Z));
      writer.WriteEndArray();
      writer.WriteEndObject();
    }

    private static void WriteAccessor(Utf8JsonWriter writer, int bufferView, int componentType, int count, string type)
    {
      writer.WriteStartObject();
      writer.WriteNumber("bufferView", bufferView);
      writer.WriteNumber("componentType", componentType);
      writer.WriteNumber("count", count);
      writer.WriteString("type", type);
      writer.WriteEndObject();
    }

    private static byte[] Pack(byte[] json, byte[] binary)
    {
      var paddedJsonLength = (json.Length + 3) & ~3;
      var totalLength = 12 + 8 + paddedJsonLength + 8 + binary.Length;
      var glb = new byte[totalLength];
      WriteUInt32(glb, 0, GlbMagic);
      WriteUInt32(glb, 4, 2);
      WriteUInt32(glb, 8, checked((uint)totalLength));
      WriteUInt32(glb, 12, checked((uint)paddedJsonLength));
      WriteUInt32(glb, 16, JsonChunkType);
      json.CopyTo(glb, 20);
      for (var offset = 20 + json.Length; offset < 20 + paddedJsonLength; offset++)
      {
        glb[offset] = 0x20;
      }

      var binaryHeader = 20 + paddedJsonLength;
      WriteUInt32(glb, binaryHeader, checked((uint)binary.Length));
      WriteUInt32(glb, binaryHeader + 4, BinaryChunkType);
      binary.CopyTo(glb, binaryHeader + 8);
      return glb;
    }

    private static string CreateMetadata(
      InterchangeBaseline baseline,
      string scopeKind,
      int localId,
      string? sourceMsh,
      string? fingerprint)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        writer.WriteStartObject();
        writer.WriteString("format", "earthtool.msh.gltf");
        writer.WriteNumber("version", 1);
        writer.WriteString("assetLineage", baseline.AssetLineageId);
        writer.WriteString("document", baseline.DocumentId);
        writer.WriteStartObject("scope");
        writer.WriteString("kind", scopeKind);
        writer.WriteNumber("localId", localId);
        writer.WriteEndObject();
        if (sourceMsh is not null)
        {
          writer.WriteString("sourceMsh", sourceMsh);
        }

        if (fingerprint is not null)
        {
          writer.WriteStartObject("nativeProjection");
          writer.WriteString("name", "static-geometry");
          writer.WriteNumber("version", 1);
          writer.WriteString("sha256", fingerprint);
          writer.WriteEndObject();
        }

        writer.WriteEndObject();
      }

      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void ValidateSupportedGraph(JsonElement root)
    {
      if (root.GetProperty("scene").GetInt32() != 0
        || root.GetProperty("scenes").GetArrayLength() != 1
        || root.GetProperty("nodes").GetArrayLength() != 1
        || root.GetProperty("meshes").GetArrayLength() != 1
        || root.GetProperty("buffers").GetArrayLength() != 1
        || root.GetProperty("meshes")[0].GetProperty("primitives").GetArrayLength() != 1)
      {
        throw new UnsupportedGltfDomainException("SceneGraph");
      }

      var sceneNodes = root.GetProperty("scenes")[0].GetProperty("nodes");
      if (sceneNodes.GetArrayLength() != 1 || sceneNodes[0].GetInt32() != 0)
      {
        throw new UnsupportedGltfDomainException("SceneMembership");
      }

      var node = root.GetProperty("nodes")[0];
      if (!node.TryGetProperty("mesh", out var mesh) || mesh.GetInt32() != 0
        || node.TryGetProperty("children", out _)
        || node.TryGetProperty("matrix", out _)
        || node.TryGetProperty("translation", out _)
        || node.TryGetProperty("rotation", out _)
        || node.TryGetProperty("scale", out _)
        || node.TryGetProperty("skin", out _)
        || node.TryGetProperty("camera", out _))
      {
        throw new UnsupportedGltfDomainException("TransformOrHierarchy");
      }

      var primitive = root.GetProperty("meshes")[0].GetProperty("primitives")[0];
      var attributes = primitive.GetProperty("attributes");
      var supportedAttributes = new HashSet<string>(StringComparer.Ordinal)
      {
        "POSITION",
        "NORMAL",
        "TEXCOORD_0"
      };
      if (attributes.EnumerateObject().Any(attribute => !supportedAttributes.Contains(attribute.Name))
        || attributes.EnumerateObject().Count() != supportedAttributes.Count
        || primitive.TryGetProperty("targets", out _))
      {
        throw new UnsupportedGltfDomainException("PrimitiveAttributes");
      }

      foreach (var domain in new[] { "animations", "materials", "textures", "images", "skins", "cameras" })
      {
        if (root.TryGetProperty(domain, out _))
        {
          throw new UnsupportedGltfDomainException(domain);
        }
      }
    }

    private static RenderVertex ProjectToGltf(RenderVertex vertex)
    {
      return new RenderVertex(
        new Vector3(vertex.Position.X, vertex.Position.Z, -vertex.Position.Y),
        new Vector3(vertex.Normal.X, vertex.Normal.Z, -vertex.Normal.Y),
        vertex.TextureCoordinate);
    }

    private static string GetMetadata(JsonElement owner, string ownerName)
    {
      if (!owner.TryGetProperty("extras", out var extras)
        || !extras.TryGetProperty("earthtool", out var metadata))
      {
        throw new MissingMetadataException(ownerName);
      }

      if (metadata.ValueKind != JsonValueKind.String)
      {
        throw new InvalidDataException("EarthTool metadata must be a string.");
      }

      return metadata.GetString() ?? throw new InvalidDataException("EarthTool metadata cannot be null.");
    }

    private static IReadOnlyList<RenderVertex> ReadVertices(JsonElement root, ReadOnlySpan<byte> binary)
    {
      var primitive = root.GetProperty("meshes")[0].GetProperty("primitives")[0];
      var attributes = primitive.GetProperty("attributes");
      var positions = ReadFloatAccessor(root, binary, attributes.GetProperty("POSITION").GetInt32(), 3);
      var normals = ReadFloatAccessor(root, binary, attributes.GetProperty("NORMAL").GetInt32(), 3);
      var textureCoordinates = ReadFloatAccessor(root, binary, attributes.GetProperty("TEXCOORD_0").GetInt32(), 2);
      if (positions.Length != 9 || normals.Length != 9 || textureCoordinates.Length != 6)
      {
        throw new UnsupportedGltfDomainException("Geometry");
      }

      var vertices = new RenderVertex[3];
      for (var vertex = 0; vertex < vertices.Length; vertex++)
      {
        vertices[vertex] = new RenderVertex(
          new Vector3(positions[vertex * 3], positions[(vertex * 3) + 1], positions[(vertex * 3) + 2]),
          new Vector3(normals[vertex * 3], normals[(vertex * 3) + 1], normals[(vertex * 3) + 2]),
          new Vector2(textureCoordinates[vertex * 2], textureCoordinates[(vertex * 2) + 1]));
      }

      return Array.AsReadOnly(vertices);
    }

    private static StaticTriangle ReadTriangle(JsonElement root, ReadOnlySpan<byte> binary, int vertexCount)
    {
      var primitive = root.GetProperty("meshes")[0].GetProperty("primitives")[0];
      if (primitive.GetProperty("mode").GetInt32() != 4)
      {
        throw new UnsupportedGltfDomainException("PrimitiveTopology");
      }

      var accessor = root.GetProperty("accessors")[primitive.GetProperty("indices").GetInt32()];
      if (accessor.GetProperty("componentType").GetInt32() != 5123
        || accessor.GetProperty("count").GetInt32() != 3
        || accessor.GetProperty("type").GetString() != "SCALAR")
      {
        throw new UnsupportedGltfDomainException("Indices");
      }

      var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
      var offset = GetOffset(view) + GetOffset(accessor);
      var result = new StaticTriangle(
        ReadUInt16(binary, offset),
        ReadUInt16(binary, offset + 2),
        ReadUInt16(binary, offset + 4),
        1);
      if (result.Vertex0 >= vertexCount || result.Vertex1 >= vertexCount || result.Vertex2 >= vertexCount)
      {
        throw new InvalidDataException("Triangle index is outside the vertex range.");
      }

      return result;
    }

    private static float[] ReadFloatAccessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex,
      int dimensions)
    {
      var accessor = root.GetProperty("accessors")[accessorIndex];
      if (accessor.GetProperty("componentType").GetInt32() != 5126
        || accessor.GetProperty("count").GetInt32() != 3)
      {
        throw new UnsupportedGltfDomainException("VertexAccessor");
      }

      var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
      if (view.TryGetProperty("byteStride", out _))
      {
        throw new UnsupportedGltfDomainException("InterleavedVertexAccessor");
      }

      var offset = GetOffset(view) + GetOffset(accessor);
      var result = new float[3 * dimensions];
      for (var index = 0; index < result.Length; index++)
      {
        result[index] = BitConverter.Int32BitsToSingle(
          BinaryPrimitives.ReadInt32LittleEndian(binary.Slice(offset + (index * 4), 4)));
        if (float.IsNaN(result[index]) || float.IsInfinity(result[index]))
        {
          throw new InvalidDataException("Vertex accessor contains a non-finite value.");
        }
      }

      return result;
    }

    private static int GetOffset(JsonElement element)
    {
      return element.TryGetProperty("byteOffset", out var value) ? value.GetInt32() : 0;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    }

    private static void WriteUInt32(Span<byte> data, int offset, uint value)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(offset, 4), value);
    }
  }

  internal static class StaticGeometryFingerprint
  {
    internal static NativeProjectionFingerprint Create(
      InterchangeBaseline baseline,
      int localId,
      IReadOnlyList<RenderVertex> vertices,
      StaticTriangle triangle)
    {
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        WriteString(writer, "earthtool.msh.gltf");
        writer.Write(1);
        WriteString(writer, "static-geometry");
        writer.Write(1);
        writer.Write(baseline.AssetLineageId.ToByteArray());
        writer.Write(baseline.DocumentId.ToByteArray());
        WriteString(writer, "mesh");
        writer.Write(localId);
        foreach (var index in new[] { triangle.Vertex0, triangle.Vertex1, triangle.Vertex2 })
        {
          var vertex = vertices[index];
          Write(writer, vertex.Position.X);
          Write(writer, vertex.Position.Y);
          Write(writer, vertex.Position.Z);
          Write(writer, vertex.Normal.X);
          Write(writer, vertex.Normal.Y);
          Write(writer, vertex.Normal.Z);
          Write(writer, vertex.TextureCoordinate.X);
          Write(writer, vertex.TextureCoordinate.Y);
        }
      }

      using var sha256 = SHA256.Create();
      var hash = sha256.ComputeHash(preimage.ToArray());
      var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
      return new NativeProjectionFingerprint("static-geometry", 1, hex);
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
      var bytes = Encoding.UTF8.GetBytes(value);
      writer.Write(bytes.Length);
      writer.Write(bytes);
    }

    private static void Write(BinaryWriter writer, float value)
    {
      writer.Write(value == 0 ? 0 : value);
    }
  }

  internal sealed class MissingMetadataException : Exception
  {
    internal MissingMetadataException(string owner)
      : base($"Missing EarthTool metadata on {owner}.")
    {
    }
  }

  internal sealed class UnsupportedMetadataVersionException : Exception
  {
  }

  internal sealed class MalformedMetadataException : Exception
  {
    internal MalformedMetadataException(string message, Exception? innerException = null)
      : base(message, innerException)
    {
    }
  }

  internal sealed class UnsupportedGltfDomainException : Exception
  {
    internal string Domain { get; }

    internal UnsupportedGltfDomainException(string domain)
      : base($"The {domain} domain is outside the one-triangle walking-skeleton profile.")
    {
      Domain = domain;
    }
  }
}
