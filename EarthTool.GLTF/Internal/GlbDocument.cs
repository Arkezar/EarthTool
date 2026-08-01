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
  internal sealed class GltfPackage
  {
    internal byte[] Json { get; }

    internal byte[] Binary { get; }

    internal string BufferFileName { get; }

    internal GltfPackage(byte[] json, byte[] binary, string bufferFileName)
    {
      Json = json;
      Binary = binary;
      BufferFileName = bufferFileName;
    }
  }

  internal static class StaticSourceObjectTraversal
  {
    internal static IEnumerable<StaticSourceObject> Flatten(StaticSourceObject source)
    {
      yield return source;
      foreach (var child in source.Children)
      {
        foreach (var descendant in Flatten(child))
        {
          yield return descendant;
        }
      }
    }
  }

  internal sealed class ParsedGlb
  {
    internal string ManifestMetadata { get; }

    internal IReadOnlyList<ParsedGltfMesh> Meshes { get; }

    internal IReadOnlyList<ParsedGltfNode> Nodes { get; }

    internal int RootNodeIndex { get; }

    internal ParsedGlb(
      string manifestMetadata,
      IReadOnlyList<ParsedGltfMesh> meshes,
      IReadOnlyList<ParsedGltfNode> nodes,
      int rootNodeIndex)
    {
      ManifestMetadata = manifestMetadata;
      Meshes = meshes;
      Nodes = nodes;
      RootNodeIndex = rootNodeIndex;
    }
  }

  internal sealed class ParsedGltfNode
  {
    internal string Metadata { get; }

    internal int MeshIndex { get; }

    internal IReadOnlyList<int> Children { get; }

    internal ParsedGltfNode(string metadata, int meshIndex, IReadOnlyList<int> children)
    {
      Metadata = metadata;
      MeshIndex = meshIndex;
      Children = children;
    }
  }

  internal sealed class ParsedGltfMesh
  {
    internal string Metadata { get; }

    internal IReadOnlyList<ParsedGltfPrimitive> Primitives { get; }

    internal ParsedGltfMesh(string metadata, IReadOnlyList<ParsedGltfPrimitive> primitives)
    {
      Metadata = metadata;
      Primitives = primitives;
    }
  }

  internal sealed class ParsedGltfPrimitive
  {
    internal IReadOnlyList<RenderVertex> Vertices { get; }

    internal IReadOnlyList<StaticTriangle> Triangles { get; }

    internal ParsedGltfPrimitive(
      IReadOnlyList<RenderVertex> vertices,
      IReadOnlyList<StaticTriangle> triangles)
    {
      Vertices = vertices;
      Triangles = triangles;
    }
  }

  internal sealed class MetadataPartition
  {
    internal int LocalId { get; }

    internal string Fingerprint { get; }

    internal MetadataPartition(int localId, string fingerprint)
    {
      LocalId = localId;
      Fingerprint = fingerprint;
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

    internal IReadOnlyList<MetadataPartition> Partitions { get; }

    internal MetadataEnvelope(
      Guid assetLineageId,
      Guid documentId,
      string scopeKind,
      int localId,
      string? sourceMsh,
      string? fingerprint,
      string? fingerprintName,
      int? fingerprintVersion,
      IReadOnlyList<MetadataPartition> partitions)
    {
      AssetLineageId = assetLineageId;
      DocumentId = documentId;
      ScopeKind = scopeKind;
      LocalId = localId;
      SourceMsh = sourceMsh;
      Fingerprint = fingerprint;
      FingerprintName = fingerprintName;
      FingerprintVersion = fingerprintVersion;
      Partitions = partitions;
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
      var package = CreatePackage(asset, baseline, false, out fingerprint);
      return Pack(package.Json, package.Binary);
    }

    internal static GltfPackage CreateSeparate(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      out NativeProjectionFingerprint fingerprint)
    {
      return CreatePackage(asset, baseline, true, out fingerprint);
    }

    internal static int GetManifestMetadataByteCount(StaticMeshAsset asset, InterchangeBaseline baseline)
    {
      var empty = CreateMetadata(baseline, "manifest", 0, string.Empty, null);
      var base64Length = checked(((asset.SerializedLength + 2) / 3) * 4);
      return checked(Encoding.UTF8.GetByteCount(empty) + base64Length);
    }

    internal static int GetMinimumOutputByteCount(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      bool glb)
    {
      long binaryLength = 0;
      foreach (var renderObject in asset.StaticRenderObjectSequence)
      {
        binaryLength = checked(binaryLength
          + (renderObject.RenderVertices.Count * 32L)
          + (renderObject.Triangles.Count * 6L));
        binaryLength = (binaryLength + 3) & ~3L;
      }

      var containerBytes = glb ? 28 : 0;
      return checked((int)(
        binaryLength
        + GetManifestMetadataByteCount(asset, baseline)
        + containerBytes));
    }

    private static GltfPackage CreatePackage(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      bool separate,
      out NativeProjectionFingerprint fingerprint)
    {
      var partitions = asset.StaticRenderObjectSequence
        .Select(item => new ProjectedPartition(
          item,
          item.RenderVertices.Select(ProjectToGltf).ToArray()))
        .ToArray();
      var binary = CreateBinary(partitions, out var layouts);
      var bufferFileName = separate ? Hash(binary) + ".bin" : null;
      fingerprint = StaticGeometryFingerprint.Create(baseline, partitions);
      var manifest = CreateMetadata(
        baseline,
        "manifest",
        0,
        Convert.ToBase64String(asset.GetSerializedRepresentation()),
        null);
      var json = CreateJson(
        asset.RootSourceObject,
        layouts,
        binary.Length,
        baseline,
        manifest,
        bufferFileName);
      return new GltfPackage(json, binary, bufferFileName ?? string.Empty);
    }

    internal static ParsedGlb Parse(byte[] glb, int maxJsonDepth)
    {
      var root = Validate(glb, maxJsonDepth, out var binaryHeader, out var binaryLength);
      using (root)
      {
        var element = root.RootElement;
        var binary = glb.AsSpan(binaryHeader + 8, binaryLength);
        return ParseDocument(element, binary);
      }
    }

    internal static ParsedGlb ParseSeparate(byte[] json, byte[] binary, int maxJsonDepth)
    {
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions
      {
        MaxDepth = maxJsonDepth,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
      });
      var declaredLength = document.RootElement.GetProperty("buffers")[0]
        .GetProperty("byteLength").GetInt32();
      if (declaredLength < 0 || declaredLength > binary.Length)
      {
        throw new InvalidDataException("The separate glTF buffer length is invalid.");
      }

      return ParseDocument(document.RootElement, binary.AsSpan(0, declaredLength));
    }

    internal static string GetSeparateBufferUri(byte[] json, int maxJsonDepth)
    {
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = maxJsonDepth });
      ValidateSupportedGraph(document.RootElement);
      var buffer = document.RootElement.GetProperty("buffers")[0];
      if (!buffer.TryGetProperty("uri", out var uri) || uri.ValueKind != JsonValueKind.String)
      {
        throw new InvalidDataException("Separate glTF requires one external buffer URI.");
      }

      return uri.GetString() ?? throw new InvalidDataException("The buffer URI cannot be null.");
    }

    internal static void ValidateSeparate(byte[] json, byte[] binary, string bufferUri)
    {
      var resources = new Dictionary<string, ArraySegment<byte>>(StringComparer.Ordinal)
      {
        ["model.gltf"] = new ArraySegment<byte>(json),
        [bufferUri] = new ArraySegment<byte>(binary)
      };
      ReadContext.CreateFromDictionary(resources)
        .WithSettingsFrom(new ReadSettings { Validation = ValidationMode.Strict })
        .ReadSchema2("model.gltf");
    }

    private static ParsedGlb ParseDocument(JsonElement root, ReadOnlySpan<byte> binary)
    {
      ValidateSupportedGraph(root);
      var manifest = GetMetadata(root.GetProperty("scenes")[0], "scene");
      var nodes = new List<ParsedGltfNode>();
      foreach (var node in root.GetProperty("nodes").EnumerateArray())
      {
        var children = node.TryGetProperty("children", out var childArray)
          ? childArray.EnumerateArray().Select(child => child.GetInt32()).ToArray()
          : Array.Empty<int>();
        nodes.Add(new ParsedGltfNode(
          GetMetadata(node, "node"),
          node.GetProperty("mesh").GetInt32(),
          Array.AsReadOnly(children)));
      }

      var meshes = new List<ParsedGltfMesh>();
      foreach (var mesh in root.GetProperty("meshes").EnumerateArray())
      {
        var primitives = new List<ParsedGltfPrimitive>();
        foreach (var primitive in mesh.GetProperty("primitives").EnumerateArray())
        {
          primitives.Add(ReadPrimitive(root, primitive, binary));
        }

        meshes.Add(new ParsedGltfMesh(
          GetMetadata(mesh, "mesh"),
          primitives.AsReadOnly()));
      }

      return new ParsedGlb(
        manifest,
        meshes.AsReadOnly(),
        nodes.AsReadOnly(),
        root.GetProperty("scenes")[0].GetProperty("nodes")[0].GetInt32());
    }

    internal static void Validate(byte[] glb, int maxJsonDepth)
    {
      using var document = Validate(glb, maxJsonDepth, out _, out _);
    }

    private static JsonDocument Validate(
      byte[] glb,
      int maxJsonDepth,
      out int binaryHeader,
      out int binaryLength)
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

      binaryHeader = 20 + jsonLength;
      binaryLength = checked((int)ReadUInt32(glb, binaryHeader));
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
      var document = JsonDocument.Parse(
        glb.AsMemory(20, jsonLength),
        documentOptions);
      try
      {
        ValidateSupportedGraph(document.RootElement);
        ModelRoot.ParseGLB(
          new ArraySegment<byte>(glb),
          new ReadSettings { Validation = ValidationMode.Strict });
        return document;
      }
      catch
      {
        document.Dispose();
        throw;
      }
    }

    internal static MetadataEnvelope ParseMetadata(string value, int maxMetadataBytes, int maxJsonDepth)
    {
      if (Encoding.UTF8.GetByteCount(value) > maxMetadataBytes)
      {
        throw new ResourceLimitException(Encoding.UTF8.GetByteCount(value), maxMetadataBytes);
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
        var partitions = new List<MetadataPartition>();
        if (root.TryGetProperty("partitions", out var partitionArray))
        {
          foreach (var partition in partitionArray.EnumerateArray())
          {
            partitions.Add(new MetadataPartition(
              partition.GetProperty("localId").GetInt32(),
              partition.GetProperty("sha256").GetString()
                ?? throw new MalformedMetadataException("Missing partition fingerprint.")));
          }
        }

        return new MetadataEnvelope(
          root.GetProperty("assetLineage").GetGuid(),
          root.GetProperty("document").GetGuid(),
          scope.GetProperty("kind").GetString() ?? throw new MalformedMetadataException("Missing scope kind."),
          scope.GetProperty("localId").GetInt32(),
          root.TryGetProperty("sourceMsh", out var sourceMsh) ? sourceMsh.GetString() : null,
          hasProjection ? projection.GetProperty("sha256").GetString() : null,
          hasProjection ? projection.GetProperty("name").GetString() : null,
          hasProjection ? projection.GetProperty("version").GetInt32() : null,
          partitions.AsReadOnly());
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

    private static byte[] CreateBinary(
      IReadOnlyList<ProjectedPartition> partitions,
      out IReadOnlyDictionary<StaticRenderObjectId, PartitionLayout> layouts)
    {
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
      var createdLayouts = new Dictionary<StaticRenderObjectId, PartitionLayout>();
      foreach (var partition in partitions)
      {
        var positionOffset = checked((int)stream.Position);
        foreach (var vertex in partition.Vertices)
        {
          writer.Write(vertex.Position.X);
          writer.Write(vertex.Position.Y);
          writer.Write(vertex.Position.Z);
        }

        var normalOffset = checked((int)stream.Position);
        foreach (var vertex in partition.Vertices)
        {
          writer.Write(vertex.Normal.X);
          writer.Write(vertex.Normal.Y);
          writer.Write(vertex.Normal.Z);
        }

        var textureOffset = checked((int)stream.Position);
        foreach (var vertex in partition.Vertices)
        {
          writer.Write(vertex.TextureCoordinate.X);
          writer.Write(vertex.TextureCoordinate.Y);
        }

        var indexOffset = checked((int)stream.Position);
        foreach (var triangle in partition.RenderObject.Triangles)
        {
          writer.Write(triangle.Vertex0);
          writer.Write(triangle.Vertex1);
          writer.Write(triangle.Vertex2);
        }

        var indexLength = checked(partition.RenderObject.Triangles.Count * 3 * sizeof(ushort));
        createdLayouts.Add(
          partition.RenderObject.Id,
          new PartitionLayout(
            partition,
            positionOffset,
            normalOffset,
            textureOffset,
            indexOffset,
            indexLength));
        while (stream.Length % 4 != 0)
        {
          writer.Write((byte)0);
        }
      }

      layouts = createdLayouts;
      return stream.ToArray();
    }

    private static byte[] CreateJson(
      StaticSourceObject rootSourceObject,
      IReadOnlyDictionary<StaticRenderObjectId, PartitionLayout> layouts,
      int binaryLength,
      InterchangeBaseline baseline,
      string manifest,
      string? bufferFileName)
    {
      var sources = StaticSourceObjectTraversal.Flatten(rootSourceObject).ToArray();
      var nodeIndices = sources
        .Select((source, index) => new { source.Id, Index = index })
        .ToDictionary(item => item.Id, item => item.Index);
      var orderedLayouts = sources
        .SelectMany(source => source.StaticRenderObjectIds)
        .Select(id => layouts[id])
        .ToArray();
      var accessorIndices = orderedLayouts
        .Select((layout, index) => new { layout.Partition.RenderObject.Id, Index = index * 4 })
        .ToDictionary(item => item.Id, item => item.Index);
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
        foreach (var source in sources)
        {
          writer.WriteStartObject();
          writer.WriteString("name", $"Source object {source.Id.Value}");
          writer.WriteNumber("mesh", nodeIndices[source.Id]);
          if (source.Children.Count > 0)
          {
            writer.WriteStartArray("children");
            foreach (var child in source.Children)
            {
              writer.WriteNumberValue(nodeIndices[child.Id]);
            }

            writer.WriteEndArray();
          }

          WriteExtras(writer, CreateMetadata(
            baseline,
            "object",
            source.Id.Value,
            null,
            null));
          writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("meshes");
        foreach (var source in sources)
        {
          writer.WriteStartObject();
          writer.WriteString("name", $"Static mesh {source.Id.Value}");
          writer.WriteStartArray("primitives");
          foreach (var renderObjectId in source.StaticRenderObjectIds)
          {
            var firstAccessor = accessorIndices[renderObjectId];
            writer.WriteStartObject();
            writer.WriteStartObject("attributes");
            writer.WriteNumber("POSITION", firstAccessor);
            writer.WriteNumber("NORMAL", firstAccessor + 1);
            writer.WriteNumber("TEXCOORD_0", firstAccessor + 2);
            writer.WriteEndObject();
            writer.WriteNumber("indices", firstAccessor + 3);
            writer.WriteNumber("mode", 4);
            writer.WriteEndObject();
          }

          writer.WriteEndArray();
          WriteExtras(writer, CreateMeshMetadata(
            baseline,
            source,
            layouts));
          writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("buffers");
        writer.WriteStartObject();
        writer.WriteNumber("byteLength", binaryLength);
        if (bufferFileName is not null)
        {
          writer.WriteString("uri", bufferFileName);
        }

        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("bufferViews");
        foreach (var layout in orderedLayouts)
        {
          var vertexCount = layout.Partition.Vertices.Count;
          WriteBufferView(writer, layout.PositionOffset, vertexCount * 12, 34962);
          WriteBufferView(writer, layout.NormalOffset, vertexCount * 12, 34962);
          WriteBufferView(writer, layout.TextureOffset, vertexCount * 8, 34962);
          WriteBufferView(writer, layout.IndexOffset, layout.IndexLength, 34963);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("accessors");
        for (var index = 0; index < orderedLayouts.Length; index++)
        {
          var layout = orderedLayouts[index];
          var firstBufferView = index * 4;
          WriteVectorAccessor(
            writer,
            firstBufferView,
            "VEC3",
            layout.Partition.Vertices.Select(vertex => vertex.Position));
          WriteAccessor(writer, firstBufferView + 1, 5126, layout.Partition.Vertices.Count, "VEC3");
          WriteAccessor(writer, firstBufferView + 2, 5126, layout.Partition.Vertices.Count, "VEC2");
          WriteAccessor(
            writer,
            firstBufferView + 3,
            5123,
            layout.Partition.RenderObject.Triangles.Count * 3,
            "SCALAR");
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
      }

      return stream.ToArray();
    }

    private static string Hash(byte[] bytes)
    {
      using var sha256 = SHA256.Create();
      return BitConverter.ToString(sha256.ComputeHash(bytes))
        .Replace("-", string.Empty)
        .ToLowerInvariant();
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

    private static string CreateMeshMetadata(
      InterchangeBaseline baseline,
      StaticSourceObject source,
      IReadOnlyDictionary<StaticRenderObjectId, PartitionLayout> layouts)
    {
      var partitions = source.StaticRenderObjectIds.Select(renderObjectId =>
      {
        var layout = layouts[renderObjectId];
        return new GeometryPartition(
          renderObjectId.Value,
          layout.Partition.Vertices,
          layout.Partition.RenderObject.Triangles);
      }).ToArray();
      var fingerprint = StaticGeometryFingerprint.CreateMesh(
        baseline,
        source.Id.Value,
        partitions);
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        writer.WriteStartObject();
        writer.WriteString("format", "earthtool.msh.gltf");
        writer.WriteNumber("version", 1);
        writer.WriteString("assetLineage", baseline.AssetLineageId);
        writer.WriteString("document", baseline.DocumentId);
        writer.WriteStartObject("scope");
        writer.WriteString("kind", "mesh");
        writer.WriteNumber("localId", source.Id.Value);
        writer.WriteEndObject();
        writer.WriteStartObject("nativeProjection");
        writer.WriteString("name", "static-geometry");
        writer.WriteNumber("version", 1);
        writer.WriteString("sha256", fingerprint.Sha256);
        writer.WriteEndObject();
        writer.WriteStartArray("partitions");
        foreach (var renderObjectId in source.StaticRenderObjectIds)
        {
          var layout = layouts[renderObjectId];
          writer.WriteStartObject();
          writer.WriteNumber("localId", renderObjectId.Value);
          writer.WriteString(
            "sha256",
            StaticGeometryFingerprint.CreatePartition(
              baseline,
              renderObjectId.Value,
              layout.Partition.Vertices,
              layout.Partition.RenderObject.Triangles));
          writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
      }

      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void ValidateSupportedGraph(JsonElement root)
    {
      var nodes = root.GetProperty("nodes");
      var meshes = root.GetProperty("meshes");
      if (root.GetProperty("scene").GetInt32() != 0
        || root.GetProperty("scenes").GetArrayLength() != 1
        || nodes.GetArrayLength() == 0
        || nodes.GetArrayLength() != meshes.GetArrayLength()
        || root.GetProperty("buffers").GetArrayLength() != 1)
      {
        throw new UnsupportedGltfDomainException("SceneGraph");
      }

      var sceneNodes = root.GetProperty("scenes")[0].GetProperty("nodes");
      if (sceneNodes.GetArrayLength() != 1
        || sceneNodes[0].GetInt32() < 0
        || sceneNodes[0].GetInt32() >= nodes.GetArrayLength())
      {
        throw new UnsupportedGltfDomainException("SceneMembership");
      }

      var seenMeshes = new HashSet<int>();
      for (var index = 0; index < nodes.GetArrayLength(); index++)
      {
        var node = nodes[index];
        if (!node.TryGetProperty("mesh", out var mesh)
          || mesh.GetInt32() < 0
          || mesh.GetInt32() >= meshes.GetArrayLength()
          || !seenMeshes.Add(mesh.GetInt32())
          || node.TryGetProperty("matrix", out _)
          || node.TryGetProperty("translation", out _)
          || node.TryGetProperty("rotation", out _)
          || node.TryGetProperty("scale", out _)
          || node.TryGetProperty("skin", out _)
          || node.TryGetProperty("camera", out _))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }

        if (node.TryGetProperty("children", out var children)
          && children.EnumerateArray().Any(child =>
            child.GetInt32() < 0 || child.GetInt32() >= nodes.GetArrayLength()))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
      }

      var supportedAttributes = new HashSet<string>(StringComparer.Ordinal)
      {
        "POSITION",
        "NORMAL",
        "TEXCOORD_0"
      };
      foreach (var mesh in meshes.EnumerateArray())
      {
        var primitives = mesh.GetProperty("primitives");
        if (primitives.GetArrayLength() == 0)
        {
          throw new UnsupportedGltfDomainException("Geometry");
        }

        foreach (var primitive in primitives.EnumerateArray())
        {
          var attributes = primitive.GetProperty("attributes");
          if (attributes.EnumerateObject().Any(attribute => !supportedAttributes.Contains(attribute.Name))
            || attributes.EnumerateObject().Count() != supportedAttributes.Count
            || primitive.TryGetProperty("targets", out _))
          {
            throw new UnsupportedGltfDomainException("PrimitiveAttributes");
          }
        }
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

    internal sealed class ProjectedPartition
    {
      internal StaticRenderObject RenderObject { get; }

      internal IReadOnlyList<RenderVertex> Vertices { get; }

      internal ProjectedPartition(
        StaticRenderObject renderObject,
        IReadOnlyList<RenderVertex> vertices)
      {
        RenderObject = renderObject;
        Vertices = vertices;
      }
    }

    private sealed class PartitionLayout
    {
      internal ProjectedPartition Partition { get; }

      internal int PositionOffset { get; }

      internal int NormalOffset { get; }

      internal int TextureOffset { get; }

      internal int IndexOffset { get; }

      internal int IndexLength { get; }

      internal PartitionLayout(
        ProjectedPartition partition,
        int positionOffset,
        int normalOffset,
        int textureOffset,
        int indexOffset,
        int indexLength)
      {
        Partition = partition;
        PositionOffset = positionOffset;
        NormalOffset = normalOffset;
        TextureOffset = textureOffset;
        IndexOffset = indexOffset;
        IndexLength = indexLength;
      }
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

    private static ParsedGltfPrimitive ReadPrimitive(
      JsonElement root,
      JsonElement primitive,
      ReadOnlySpan<byte> binary)
    {
      var attributes = primitive.GetProperty("attributes");
      var positions = ReadFloatAccessor(
        root,
        binary,
        attributes.GetProperty("POSITION").GetInt32(),
        3,
        "VEC3");
      var normals = ReadFloatAccessor(
        root,
        binary,
        attributes.GetProperty("NORMAL").GetInt32(),
        3,
        "VEC3");
      var textureCoordinates = ReadFloatAccessor(
        root,
        binary,
        attributes.GetProperty("TEXCOORD_0").GetInt32(),
        2,
        "VEC2");
      var vertexCount = positions.Length / 3;
      if (vertexCount == 0
        || normals.Length != vertexCount * 3
        || textureCoordinates.Length != vertexCount * 2)
      {
        throw new UnsupportedGltfDomainException("Geometry");
      }

      var vertices = new RenderVertex[vertexCount];
      for (var vertex = 0; vertex < vertices.Length; vertex++)
      {
        vertices[vertex] = new RenderVertex(
          new Vector3(positions[vertex * 3], positions[(vertex * 3) + 1], positions[(vertex * 3) + 2]),
          new Vector3(normals[vertex * 3], normals[(vertex * 3) + 1], normals[(vertex * 3) + 2]),
          new Vector2(textureCoordinates[vertex * 2], textureCoordinates[(vertex * 2) + 1]));
      }

      if ((primitive.TryGetProperty("mode", out var mode) ? mode.GetInt32() : 4) != 4)
      {
        throw new UnsupportedGltfDomainException("PrimitiveTopology");
      }

      var accessor = root.GetProperty("accessors")[primitive.GetProperty("indices").GetInt32()];
      var componentType = accessor.GetProperty("componentType").GetInt32();
      var indexCount = accessor.GetProperty("count").GetInt32();
      if (componentType is not (5121 or 5123 or 5125)
        || indexCount == 0
        || indexCount % 3 != 0
        || accessor.GetProperty("type").GetString() != "SCALAR"
        || accessor.TryGetProperty("sparse", out _))
      {
        throw new UnsupportedGltfDomainException("Indices");
      }

      var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
      EnsureBufferView(view);
      var componentSize = componentType == 5121 ? 1 : componentType == 5123 ? 2 : 4;
      var stride = view.TryGetProperty("byteStride", out var strideValue)
        ? strideValue.GetInt32()
        : componentSize;
      if (stride < componentSize)
      {
        throw new InvalidDataException("Index accessor stride is too small.");
      }

      var offset = checked(GetOffset(view) + GetOffset(accessor));
      EnsureRange(binary.Length, offset, indexCount, stride, componentSize);
      var indices = new ushort[indexCount];
      for (var index = 0; index < indices.Length; index++)
      {
        var componentOffset = checked(offset + index * stride);
        var value = componentType == 5121
          ? binary[componentOffset]
          : componentType == 5123
            ? ReadUInt16(binary, componentOffset)
            : ReadUInt32(binary, componentOffset);
        if (value >= vertexCount || value > ushort.MaxValue)
        {
          throw new InvalidDataException("Triangle index is outside the vertex range.");
        }

        indices[index] = (ushort)value;
      }

      var triangles = new StaticTriangle[indexCount / 3];
      for (var triangle = 0; triangle < triangles.Length; triangle++)
      {
        triangles[triangle] = new StaticTriangle(
          indices[triangle * 3],
          indices[(triangle * 3) + 1],
          indices[(triangle * 3) + 2],
          1);
      }

      return new ParsedGltfPrimitive(
        Array.AsReadOnly(vertices),
        Array.AsReadOnly(triangles));
    }

    private static float[] ReadFloatAccessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex,
      int dimensions,
      string accessorType)
    {
      var accessor = root.GetProperty("accessors")[accessorIndex];
      if (accessor.GetProperty("componentType").GetInt32() != 5126
        || accessor.GetProperty("type").GetString() != accessorType
        || accessor.TryGetProperty("normalized", out var normalized) && normalized.GetBoolean()
        || accessor.TryGetProperty("sparse", out _))
      {
        throw new UnsupportedGltfDomainException("VertexAccessor");
      }

      var count = accessor.GetProperty("count").GetInt32();
      if (count <= 0 || count > 65536)
      {
        throw new UnsupportedGltfDomainException("VertexAccessor");
      }

      var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
      EnsureBufferView(view);
      var elementSize = dimensions * sizeof(float);
      var stride = view.TryGetProperty("byteStride", out var strideValue)
        ? strideValue.GetInt32()
        : elementSize;
      if (stride < elementSize)
      {
        throw new InvalidDataException("Vertex accessor stride is too small.");
      }

      var offset = checked(GetOffset(view) + GetOffset(accessor));
      EnsureRange(binary.Length, offset, count, stride, elementSize);
      var result = new float[count * dimensions];
      for (var element = 0; element < count; element++)
      {
        for (var component = 0; component < dimensions; component++)
        {
          var resultIndex = element * dimensions + component;
          var componentOffset = checked(offset + element * stride + component * sizeof(float));
          result[resultIndex] = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(binary.Slice(componentOffset, sizeof(float))));
          if (float.IsNaN(result[resultIndex]) || float.IsInfinity(result[resultIndex]))
          {
            throw new InvalidDataException("Vertex accessor contains a non-finite value.");
          }
        }
      }

      return result;
    }

    private static void EnsureBufferView(JsonElement view)
    {
      if (view.GetProperty("buffer").GetInt32() != 0)
      {
        throw new UnsupportedGltfDomainException("Buffers");
      }
    }

    private static void EnsureRange(int bufferLength, int offset, int count, int stride, int elementSize)
    {
      var end = checked((long)offset + ((long)(count - 1) * stride) + elementSize);
      if (offset < 0 || end > bufferLength)
      {
        throw new InvalidDataException("Accessor exceeds its buffer.");
      }
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

  internal sealed class GeometryPartition
  {
    internal int LocalId { get; }

    internal IReadOnlyList<RenderVertex> Vertices { get; }

    internal IReadOnlyList<StaticTriangle> Triangles { get; }

    internal GeometryPartition(
      int localId,
      IReadOnlyList<RenderVertex> vertices,
      IReadOnlyList<StaticTriangle> triangles)
    {
      LocalId = localId;
      Vertices = vertices;
      Triangles = triangles;
    }
  }

  internal static class StaticGeometryFingerprint
  {
    internal static NativeProjectionFingerprint Create(
      InterchangeBaseline baseline,
      IReadOnlyList<GlbDocument.ProjectedPartition> partitions)
    {
      return Create(
        baseline,
        partitions.Select(partition => new GeometryPartition(
          partition.RenderObject.LocalId,
          partition.Vertices,
          partition.RenderObject.Triangles)).ToArray());
    }

    internal static NativeProjectionFingerprint Create(
      InterchangeBaseline baseline,
      IReadOnlyList<GeometryPartition> partitions)
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
        foreach (var partition in partitions.OrderBy(item => item.LocalId))
        {
          writer.Write(partition.LocalId);
          WriteString(writer, CreatePartition(
            baseline,
            partition.LocalId,
            partition.Vertices,
            partition.Triangles));
        }
      }

      return CreateFingerprint(preimage);
    }

    internal static NativeProjectionFingerprint CreateMesh(
      InterchangeBaseline baseline,
      int sourceObjectLocalId,
      IReadOnlyList<GeometryPartition> partitions)
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
        writer.Write(sourceObjectLocalId);
        foreach (var partition in partitions.OrderBy(item => item.LocalId))
        {
          writer.Write(partition.LocalId);
          WriteString(writer, CreatePartition(
            baseline,
            partition.LocalId,
            partition.Vertices,
            partition.Triangles));
        }
      }

      return CreateFingerprint(preimage);
    }

    internal static string CreatePartition(
      InterchangeBaseline baseline,
      int localId,
      IReadOnlyList<RenderVertex> vertices,
      IReadOnlyList<StaticTriangle> triangles)
    {
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        WriteString(writer, "earthtool.msh.gltf");
        writer.Write(1);
        WriteString(writer, "static-geometry-partition");
        writer.Write(1);
        writer.Write(baseline.AssetLineageId.ToByteArray());
        writer.Write(baseline.DocumentId.ToByteArray());
        writer.Write(localId);
        var triangleTokens = triangles
          .Select(triangle => CreateTriangleToken(vertices, triangle))
          .OrderBy(token => token, ByteArrayComparer.Instance)
          .ToArray();
        writer.Write(triangleTokens.Length);
        foreach (var token in triangleTokens)
        {
          writer.Write(token);
        }
      }

      return Hash(preimage.ToArray());
    }

    private static byte[] CreateTriangleToken(
      IReadOnlyList<RenderVertex> vertices,
      StaticTriangle triangle)
    {
      var corners = new[]
      {
        CreateVertexToken(vertices[triangle.Vertex0]),
        CreateVertexToken(vertices[triangle.Vertex1]),
        CreateVertexToken(vertices[triangle.Vertex2])
      };
      var rotations = new byte[3][];
      for (var rotation = 0; rotation < rotations.Length; rotation++)
      {
        var token = new byte[corners[0].Length * 3];
        for (var corner = 0; corner < 3; corner++)
        {
          corners[(corner + rotation) % 3].CopyTo(token, corner * corners[0].Length);
        }

        rotations[rotation] = token;
      }

      return rotations.OrderBy(token => token, ByteArrayComparer.Instance).First();
    }

    private static byte[] CreateVertexToken(RenderVertex vertex)
    {
      using var stream = new MemoryStream(32);
      using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
      {
        Write(writer, vertex.Position.X);
        Write(writer, vertex.Position.Y);
        Write(writer, vertex.Position.Z);
        Write(writer, vertex.Normal.X);
        Write(writer, vertex.Normal.Y);
        Write(writer, vertex.Normal.Z);
        Write(writer, vertex.TextureCoordinate.X);
        Write(writer, vertex.TextureCoordinate.Y);
      }

      return stream.ToArray();
    }

    private static NativeProjectionFingerprint CreateFingerprint(MemoryStream preimage)
    {
      return new NativeProjectionFingerprint("static-geometry", 1, Hash(preimage.ToArray()));
    }

    private static string Hash(byte[] value)
    {
      using var sha256 = SHA256.Create();
      var hash = sha256.ComputeHash(value);
      return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
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

    private sealed class ByteArrayComparer : IComparer<byte[]>
    {
      internal static ByteArrayComparer Instance { get; } = new ByteArrayComparer();

      public int Compare(byte[]? left, byte[]? right)
      {
        if (ReferenceEquals(left, right))
        {
          return 0;
        }

        if (left is null)
        {
          return -1;
        }

        if (right is null)
        {
          return 1;
        }

        for (var index = 0; index < Math.Min(left.Length, right.Length); index++)
        {
          var comparison = left[index].CompareTo(right[index]);
          if (comparison != 0)
          {
            return comparison;
          }
        }

        return left.Length.CompareTo(right.Length);
      }
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
