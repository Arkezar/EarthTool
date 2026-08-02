#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
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
  internal enum GltfImportIntent
  {
    Edit,
    NewModel
  }

  internal sealed class GltfPackage
  {
    internal byte[] Json { get; }

    internal byte[] Binary { get; }

    internal string BufferFileName { get; }

    internal IReadOnlyDictionary<string, byte[]> ImageSidecars { get; }

    internal GltfPackage(
      byte[] json,
      byte[] binary,
      string bufferFileName,
      IReadOnlyDictionary<string, byte[]> imageSidecars)
    {
      Json = json;
      Binary = binary;
      BufferFileName = bufferFileName;
      ImageSidecars = imageSidecars;
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
    internal string? ManifestMetadata { get; }

    internal bool HasReservedMetadata { get; }

    internal IReadOnlyList<ParsedGltfMesh> Meshes { get; }

    internal IReadOnlyList<ParsedGltfNode> Nodes { get; }

    internal IReadOnlyList<ParsedGltfMaterial> Materials { get; }

    internal IReadOnlyList<ParsedGltfAnimation> Animations { get; }

    internal int RootNodeIndex { get; }

    internal ParsedGlb(
      string? manifestMetadata,
      bool hasReservedMetadata,
      IReadOnlyList<ParsedGltfMesh> meshes,
      IReadOnlyList<ParsedGltfNode> nodes,
      IReadOnlyList<ParsedGltfMaterial> materials,
      IReadOnlyList<ParsedGltfAnimation> animations,
      int rootNodeIndex)
    {
      ManifestMetadata = manifestMetadata;
      HasReservedMetadata = hasReservedMetadata;
      Meshes = meshes;
      Nodes = nodes;
      Materials = materials;
      Animations = animations;
      RootNodeIndex = rootNodeIndex;
    }
  }

  internal sealed class ParsedGltfNode
  {
    internal string? Name { get; }

    internal string? Metadata { get; }

    internal int? MeshIndex { get; }

    internal IReadOnlyList<int> Children { get; }

    internal Matrix4x4 LocalTransform { get; }

    internal ParsedGltfNode(
      string? name,
      string? metadata,
      int? meshIndex,
      IReadOnlyList<int> children,
      Matrix4x4 localTransform)
    {
      Name = name;
      Metadata = metadata;
      MeshIndex = meshIndex;
      Children = children;
      LocalTransform = localTransform;
    }
  }

  internal sealed class ParsedGltfMesh
  {
    internal string? Metadata { get; }

    internal IReadOnlyList<ParsedGltfPrimitive> Primitives { get; }

    internal ParsedGltfMesh(string? metadata, IReadOnlyList<ParsedGltfPrimitive> primitives)
    {
      Metadata = metadata;
      Primitives = primitives;
    }
  }

  internal sealed class ParsedGltfPrimitive
  {
    internal IReadOnlyList<RenderVertex> Vertices { get; }

    internal IReadOnlyList<StaticTriangle> Triangles { get; }

    internal int? MaterialIndex { get; }

    internal ParsedGltfPrimitive(
      IReadOnlyList<RenderVertex> vertices,
      IReadOnlyList<StaticTriangle> triangles,
      int? materialIndex)
    {
      Vertices = vertices;
      Triangles = triangles;
      MaterialIndex = materialIndex;
    }
  }

  internal sealed class ParsedGltfMaterial
  {
    internal string? Metadata { get; }

    internal bool HasBaseColorTexture { get; }

    internal ParsedGltfMaterial(string? metadata, bool hasBaseColorTexture)
    {
      Metadata = metadata;
      HasBaseColorTexture = hasBaseColorTexture;
    }
  }

  internal sealed class ParsedGltfAnimation
  {
    internal string? Name { get; }

    internal IReadOnlyList<ParsedGltfAnimationObject> Objects { get; }

    internal float EndTime => Objects.Max(item => item.EndTime);

    internal ParsedGltfAnimation(string? name, IReadOnlyList<ParsedGltfAnimationObject> objects)
    {
      Name = name;
      Objects = objects;
    }
  }

  internal sealed class ParsedGltfAnimationObject
  {
    internal int NodeIndex { get; }

    private readonly ParsedGltfAnimationChannel _translation;
    private readonly ParsedGltfAnimationChannel _rotation;
    private readonly ParsedGltfAnimationChannel _scale;

    internal float EndTime => Math.Max(
      _translation.EndTime,
      Math.Max(_rotation.EndTime, _scale.EndTime));

    internal ParsedGltfAnimationObject(
      int nodeIndex,
      ParsedGltfAnimationChannel translation,
      ParsedGltfAnimationChannel rotation,
      ParsedGltfAnimationChannel scale)
    {
      NodeIndex = nodeIndex;
      _translation = translation;
      _rotation = rotation;
      _scale = scale;
    }

    internal IReadOnlyList<ProjectedAnimationFrame> SampleFrames(int frameCount)
    {
      var frames = new ProjectedAnimationFrame[frameCount];
      for (var frame = 0; frame < frameCount; frame++)
      {
        var time = frame / 24f;
        var translation = _translation.Sample(time);
        var rotation = _rotation.Sample(time);
        var scale = _scale.Sample(time);
        frames[frame] = StaticAnimationProjection.Canonicalize(
          new Vector3(translation[0], translation[1], translation[2]),
          new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]),
          new Vector3(scale[0], scale[1], scale[2]));
      }
      return Array.AsReadOnly(frames);
    }
  }

  internal sealed class ParsedGltfAnimationChannel
  {
    private readonly float[] _times;
    private readonly float[] _values;
    private readonly int _dimensions;
    private readonly string _interpolation;

    internal float EndTime => _times[_times.Length - 1];

    internal ParsedGltfAnimationChannel(
      float[] times,
      float[] values,
      int dimensions,
      string interpolation)
    {
      _times = times;
      _values = values;
      _dimensions = dimensions;
      _interpolation = interpolation;
    }

    internal float[] Sample(float time)
    {
      if (time <= _times[0])
      {
        return ReadValue(0);
      }
      if (time >= _times[_times.Length - 1])
      {
        return ReadValue(_times.Length - 1);
      }

      var right = Array.BinarySearch(_times, time);
      if (right >= 0)
      {
        return ReadValue(right);
      }
      right = ~right;
      var left = right - 1;
      if (_interpolation == "STEP")
      {
        return ReadValue(left);
      }

      var duration = _times[right] - _times[left];
      var amount = (time - _times[left]) / duration;
      if (_interpolation == "CUBICSPLINE")
      {
        return SampleCubic(left, right, duration, amount);
      }
      if (_dimensions == 4)
      {
        var first = ReadValue(left);
        var second = ReadValue(right);
        var rotation = Quaternion.Slerp(
          new Quaternion(first[0], first[1], first[2], first[3]),
          new Quaternion(second[0], second[1], second[2], second[3]),
          amount);
        return new[] { rotation.X, rotation.Y, rotation.Z, rotation.W };
      }

      var from = ReadValue(left);
      var to = ReadValue(right);
      return from.Zip(to, (first, second) => first + ((second - first) * amount)).ToArray();
    }

    private float[] ReadValue(int key)
    {
      var valueIndex = _interpolation == "CUBICSPLINE" ? (key * 3) + 1 : key;
      return _values.Skip(valueIndex * _dimensions).Take(_dimensions).ToArray();
    }

    private float[] SampleCubic(int left, int right, float duration, float amount)
    {
      var p0 = ReadValue(left);
      var p1 = ReadValue(right);
      var m0Offset = ((left * 3) + 2) * _dimensions;
      var m1Offset = (right * 3) * _dimensions;
      var amountSquared = amount * amount;
      var amountCubed = amountSquared * amount;
      var result = new float[_dimensions];
      for (var component = 0; component < result.Length; component++)
      {
        var m0 = _values[m0Offset + component] * duration;
        var m1 = _values[m1Offset + component] * duration;
        result[component] = ((2 * amountCubed) - (3 * amountSquared) + 1) * p0[component]
          + (amountCubed - (2 * amountSquared) + amount) * m0
          + ((-2 * amountCubed) + (3 * amountSquared)) * p1[component]
          + (amountCubed - amountSquared) * m1;
      }
      return result;
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

  internal sealed class MetadataAnimationClass
  {
    internal int ClassIndex { get; }

    internal IReadOnlyList<int> Objects { get; }

    internal IReadOnlyList<int> NativeObjects { get; }

    internal string? Fingerprint { get; }

    internal MetadataAnimationClass(
      int classIndex,
      IReadOnlyList<int> objects,
      IReadOnlyList<int> nativeObjects,
      string? fingerprint)
    {
      ClassIndex = classIndex;
      Objects = objects;
      NativeObjects = nativeObjects;
      Fingerprint = fingerprint;
    }
  }

  internal sealed class MetadataAnimationProjection
  {
    internal uint AnimationClassValue { get; }

    internal int ClassIndex { get; }

    internal byte DeclaredLength { get; }

    internal bool IsNative { get; }

    internal bool HasSourceTracks { get; }

    internal string? Fingerprint { get; }

    internal IReadOnlyList<byte> ScaleFrames { get; }

    internal IReadOnlyList<byte> TranslationFrames { get; }

    internal IReadOnlyList<byte> Matrices { get; }

    internal MetadataAnimationProjection(
      uint animationClassValue,
      int classIndex,
      byte declaredLength,
      bool isNative,
      bool hasSourceTracks,
      string? fingerprint,
      IReadOnlyList<byte> scaleFrames,
      IReadOnlyList<byte> translationFrames,
      IReadOnlyList<byte> matrices)
    {
      AnimationClassValue = animationClassValue;
      ClassIndex = classIndex;
      DeclaredLength = declaredLength;
      IsNative = isNative;
      HasSourceTracks = hasSourceTracks;
      Fingerprint = fingerprint;
      ScaleFrames = scaleFrames;
      TranslationFrames = translationFrames;
      Matrices = matrices;
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

    internal IReadOnlyList<int> StaticRenderObjectLocalIds { get; }

    internal IReadOnlyList<int> SourceObjectLocalIds { get; }

    internal IReadOnlyList<int> StaticRenderObjectInventory { get; }

    internal IReadOnlyList<int> SourceObjectInventory { get; }

    internal int? NextStaticRenderObjectLocalId { get; }

    internal int? NextSourceObjectLocalId { get; }

    internal IReadOnlyList<byte>? TextureBinding { get; }

    internal AnimationClassBytes? AnimationLengths { get; }

    internal AnimationClassBytes? AnimationFrameIndices { get; }

    internal IReadOnlyList<MetadataAnimationClass> AnimationClasses { get; }

    internal MetadataAnimationProjection? AnimationProjection { get; }

    internal int? AttachmentPhysicalNumber { get; }

    internal IReadOnlyList<byte>? AttachmentRecord { get; }

    internal int? CannonRenderPositionNumber { get; }

    internal IReadOnlyList<byte>? CannonRenderPosition { get; }

    internal MetadataEnvelope(
      Guid assetLineageId,
      Guid documentId,
      string scopeKind,
      int localId,
      string? sourceMsh,
      string? fingerprint,
      string? fingerprintName,
      int? fingerprintVersion,
      IReadOnlyList<MetadataPartition> partitions,
      IReadOnlyList<int> staticRenderObjectLocalIds,
      IReadOnlyList<int> sourceObjectLocalIds,
      IReadOnlyList<int> staticRenderObjectInventory,
      IReadOnlyList<int> sourceObjectInventory,
      int? nextStaticRenderObjectLocalId,
      int? nextSourceObjectLocalId,
      IReadOnlyList<byte>? textureBinding,
      AnimationClassBytes? animationLengths,
      AnimationClassBytes? animationFrameIndices,
      IReadOnlyList<MetadataAnimationClass> animationClasses,
      MetadataAnimationProjection? animationProjection,
      int? attachmentPhysicalNumber,
      IReadOnlyList<byte>? attachmentRecord,
      int? cannonRenderPositionNumber,
      IReadOnlyList<byte>? cannonRenderPosition)
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
      StaticRenderObjectLocalIds = staticRenderObjectLocalIds;
      SourceObjectLocalIds = sourceObjectLocalIds;
      StaticRenderObjectInventory = staticRenderObjectInventory;
      SourceObjectInventory = sourceObjectInventory;
      NextStaticRenderObjectLocalId = nextStaticRenderObjectLocalId;
      NextSourceObjectLocalId = nextSourceObjectLocalId;
      TextureBinding = textureBinding;
      AnimationLengths = animationLengths;
      AnimationFrameIndices = animationFrameIndices;
      AnimationClasses = animationClasses;
      AnimationProjection = animationProjection;
      AttachmentPhysicalNumber = attachmentPhysicalNumber;
      AttachmentRecord = attachmentRecord;
      CannonRenderPositionNumber = cannonRenderPositionNumber;
      CannonRenderPosition = cannonRenderPosition;
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
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      out NativeProjectionFingerprint fingerprint)
    {
      var package = CreatePackage(asset, baseline, false, previews, out fingerprint);
      return Pack(package.Json, package.Binary);
    }

    internal static GltfPackage CreateSeparate(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      out NativeProjectionFingerprint fingerprint)
    {
      return CreatePackage(asset, baseline, true, previews, out fingerprint);
    }

    internal static int GetMaximumMetadataByteCount(StaticMeshAsset asset, InterchangeBaseline baseline)
    {
      var animations = StaticAnimationProjection.Create(asset, baseline);
      var empty = CreateMetadata(baseline, "manifest", 0, string.Empty, null, asset, animations);
      var base64Length = checked(((asset.SerializedLength + 2) / 3) * 4);
      var maximum = checked(Encoding.UTF8.GetByteCount(empty) + base64Length);
      foreach (var source in StaticSourceObjectTraversal.Flatten(asset.RootSourceObject))
      {
        var metadata = CreateMetadata(
          baseline,
          "object",
          source.Id.Value,
          null,
          null,
          animationProjection: animations.Objects.SingleOrDefault(item =>
            item.SourceObjectLocalId == source.Id.Value));
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(metadata));
      }
      foreach (var attachment in ProjectAttachments(asset))
      {
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(
          CreateAttachmentMetadata(baseline, attachment)));
      }
      foreach (var cannon in ProjectCannonRenderPositions(asset))
      {
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(
          CreateCannonRenderPositionMetadata(baseline, cannon)));
      }
      return maximum;
    }

    internal static int GetMinimumOutputByteCount(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      bool glb)
    {
      long binaryLength = 0;
      foreach (var renderObject in asset.StaticRenderObjectSequence)
      {
        var indexSize = renderObject.Triangles.Any(triangle =>
          triangle.Vertex0 == ushort.MaxValue
          || triangle.Vertex1 == ushort.MaxValue
          || triangle.Vertex2 == ushort.MaxValue) ? 4L : 2L;
        binaryLength = checked(binaryLength
          + (renderObject.RenderVertices.Count * 32L)
          + (renderObject.Triangles.Count * 3L * indexSize));
        binaryLength = (binaryLength + 3) & ~3L;
      }

      var animations = StaticAnimationProjection.Create(asset, baseline);
      foreach (var clip in animations.Clips)
      {
        binaryLength = checked(binaryLength
          + (clip.FrameCount * sizeof(float))
          + (clip.Objects.Count * clip.FrameCount * 10L * sizeof(float)));
      }

      var containerBytes = glb ? 28 : 0;
      return checked((int)(
        binaryLength
        + GetMaximumMetadataByteCount(asset, baseline)
        + containerBytes));
    }

    private static GltfPackage CreatePackage(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      bool separate,
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      out NativeProjectionFingerprint fingerprint)
    {
      var partitions = asset.StaticRenderObjectSequence
        .Select(item => new ProjectedPartition(
          item,
          item.RenderVertices.Select(ProjectToGltf).ToArray()))
        .ToArray();
      var animations = StaticAnimationProjection.Create(asset, baseline);
      var binary = CreateBinary(
        partitions,
        animations,
        previews,
        !separate,
        out var layouts,
        out var previewLayouts,
        out var animationLayouts);
      var bufferFileName = separate ? Hash(binary) + ".bin" : null;
      fingerprint = StaticGeometryFingerprint.Create(baseline, partitions);
      var manifest = CreateMetadata(
        baseline,
        "manifest",
        0,
        Convert.ToBase64String(asset.GetSerializedRepresentation()),
        null,
        asset,
        animations);
      var json = CreateJson(
        asset,
        layouts,
        binary.Length,
        baseline,
        manifest,
        previewLayouts,
        animations,
        animationLayouts,
        bufferFileName);
      var imageSidecars = separate
        ? previews.Values
          .GroupBy(preview => preview.ContentAddress, StringComparer.Ordinal)
          .ToDictionary(
            group => group.Key + ".png",
            group => group.First().Png,
            StringComparer.Ordinal)
        : new Dictionary<string, byte[]>(StringComparer.Ordinal);
      return new GltfPackage(json, binary, bufferFileName ?? string.Empty, imageSidecars);
    }

    internal static ParsedGlb Parse(byte[] glb, GltfOperationProfile profile)
    {
      return Parse(glb, profile, GltfImportIntent.Edit);
    }

    internal static ParsedGlb ParseNewModel(byte[] glb, GltfOperationProfile profile)
    {
      return Parse(glb, profile, GltfImportIntent.NewModel);
    }

    private static ParsedGlb Parse(
      byte[] glb,
      GltfOperationProfile profile,
      GltfImportIntent intent)
    {
      var root = Validate(glb, profile, intent, out var binaryHeader, out var binaryLength);
      using (root)
      {
        var binary = glb.AsSpan(binaryHeader + 8, binaryLength);
        return ParseDocument(root.RootElement, binary, profile, intent);
      }
    }

    internal static ParsedGlb ParseSeparate(
      byte[] json,
      byte[] binary,
      GltfOperationProfile profile)
    {
      return ParseSeparate(json, binary, profile, GltfImportIntent.Edit);
    }

    internal static ParsedGlb ParseSeparateNewModel(
      byte[] json,
      byte[] binary,
      GltfOperationProfile profile)
    {
      return ParseSeparate(json, binary, profile, GltfImportIntent.NewModel);
    }

    private static ParsedGlb ParseSeparate(
      byte[] json,
      byte[] binary,
      GltfOperationProfile profile,
      GltfImportIntent intent)
    {
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions
      {
        MaxDepth = profile.MaxJsonDepth,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
      });
      var declaredLength = document.RootElement.GetProperty("buffers")[0]
        .GetProperty("byteLength").GetInt32();
      if (declaredLength < 0 || declaredLength > binary.Length)
      {
        throw new InvalidDataException("The separate glTF buffer length is invalid.");
      }

      return ParseDocument(document.RootElement, binary.AsSpan(0, declaredLength), profile, intent);
    }

    internal static string GetSeparateBufferUri(byte[] json, GltfOperationProfile profile)
    {
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth });
      var buffer = document.RootElement.GetProperty("buffers")[0];
      if (!buffer.TryGetProperty("uri", out var uri) || uri.ValueKind != JsonValueKind.String)
      {
        throw new InvalidDataException("Separate glTF requires one external buffer URI.");
      }

      return uri.GetString() ?? throw new InvalidDataException("The buffer URI cannot be null.");
    }

    internal static IReadOnlyList<string> GetSeparateImageUris(
      byte[] json,
      GltfOperationProfile profile)
    {
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth });
      if (!document.RootElement.TryGetProperty("images", out var images))
      {
        return Array.Empty<string>();
      }
      return Array.AsReadOnly(images.EnumerateArray()
        .Where(image => image.TryGetProperty("uri", out _))
        .Select(image => image.GetProperty("uri").GetString()
          ?? throw new InvalidDataException("An image URI cannot be null."))
        .Where(uri => !uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.Ordinal)
        .ToArray());
    }

    internal static void ValidateSeparate(
      byte[] json,
      byte[] binary,
      string bufferUri,
      IReadOnlyDictionary<string, byte[]> imageResources)
    {
      var resources = new Dictionary<string, ArraySegment<byte>>(StringComparer.Ordinal)
      {
        ["model.gltf"] = new ArraySegment<byte>(json),
        [bufferUri] = new ArraySegment<byte>(binary)
      };
      foreach (var image in imageResources)
      {
        resources.Add(image.Key, new ArraySegment<byte>(image.Value));
      }
      ReadContext.CreateFromDictionary(resources)
        .WithSettingsFrom(new ReadSettings { Validation = ValidationMode.Strict })
        .ReadSchema2("model.gltf");
    }

    private static ParsedGlb ParseDocument(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      GltfOperationProfile profile,
      GltfImportIntent intent)
    {
      ValidateSupportedGraph(root, profile, intent);
      var manifest = intent == GltfImportIntent.Edit
        ? GetMetadata(root.GetProperty("scenes")[0], "scene")
        : TryGetMetadata(root.GetProperty("scenes")[0]);
      var nodes = new List<ParsedGltfNode>();
      foreach (var node in root.GetProperty("nodes").EnumerateArray())
      {
        var children = node.TryGetProperty("children", out var childArray)
          ? childArray.EnumerateArray().Select(child => child.GetInt32()).ToArray()
          : Array.Empty<int>();
        nodes.Add(new ParsedGltfNode(
          node.TryGetProperty("name", out var name) ? name.GetString() : null,
          TryGetMetadata(node),
          node.TryGetProperty("mesh", out var mesh) ? mesh.GetInt32() : null,
          Array.AsReadOnly(children),
          ReadNodeTransform(node)));
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
          intent == GltfImportIntent.Edit ? GetMetadata(mesh, "mesh") : TryGetMetadata(mesh),
          primitives.AsReadOnly()));
      }

      var materials = root.TryGetProperty("materials", out var materialArray)
        ? materialArray.EnumerateArray()
          .Select(material => new ParsedGltfMaterial(
            intent == GltfImportIntent.Edit
              ? GetMetadata(material, "material")
              : TryGetMetadata(material),
            material.TryGetProperty("pbrMetallicRoughness", out var pbr)
              && pbr.TryGetProperty("baseColorTexture", out _)))
          .ToArray()
        : Array.Empty<ParsedGltfMaterial>();
      var animations = ReadAnimations(root, binary);

      return new ParsedGlb(
        manifest,
        HasReservedMetadata(root),
        meshes.AsReadOnly(),
        nodes.AsReadOnly(),
        Array.AsReadOnly(materials),
        animations,
        root.GetProperty("scenes")[0].GetProperty("nodes")[0].GetInt32());
    }

    internal static void Validate(byte[] glb, GltfOperationProfile profile)
    {
      using var document = Validate(glb, profile, GltfImportIntent.Edit, out _, out _);
    }

    private static JsonDocument Validate(
      byte[] glb,
      GltfOperationProfile profile,
      GltfImportIntent intent,
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
        MaxDepth = profile.MaxJsonDepth,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
      };
      var document = JsonDocument.Parse(
        glb.AsMemory(20, jsonLength),
        documentOptions);
      try
      {
        ValidateSupportedGraph(document.RootElement, profile, intent);
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

        var staticRenderObjectLocalIds = ReadIntegerArray(root, "staticRenderObjectLocalIds");
        var sourceObjectLocalIds = ReadIntegerArray(root, "sourceObjectLocalIds");
        var staticRenderObjectInventory = ReadIntegerArray(root, "staticRenderObjectInventory");
        var sourceObjectInventory = ReadIntegerArray(root, "sourceObjectInventory");
        AnimationClassBytes? animationLengths = null;
        AnimationClassBytes? animationFrameIndices = null;
        var animationClasses = new List<MetadataAnimationClass>();
        MetadataAnimationProjection? animationProjection = null;
        if (root.TryGetProperty("staticAnimation", out var staticAnimation))
        {
          if (staticAnimation.TryGetProperty("lengths", out var lengths))
          {
            animationLengths = ReadAnimationBytes(lengths, "staticAnimation.lengths");
            animationFrameIndices = ReadAnimationBytes(
              staticAnimation.GetProperty("frameIndices"),
              "staticAnimation.frameIndices");
            foreach (var item in staticAnimation.GetProperty("classes").EnumerateArray())
            {
              animationClasses.Add(new MetadataAnimationClass(
                item.GetProperty("class").GetInt32(),
                ReadIntegerArray(item, "objects"),
                ReadIntegerArray(item, "nativeObjects"),
                item.TryGetProperty("sha256", out var animationFingerprint)
                  ? animationFingerprint.GetString()
                  : null));
            }
          }
          else
          {
            var status = staticAnimation.GetProperty("status").GetString();
            animationProjection = new MetadataAnimationProjection(
              staticAnimation.GetProperty("animationClassValue").GetUInt32(),
              staticAnimation.GetProperty("class").GetInt32(),
              staticAnimation.GetProperty("declaredLength").GetByte(),
              status == "native",
              status != "absent",
              staticAnimation.TryGetProperty("sha256", out var animationFingerprint)
                ? animationFingerprint.GetString()
                : null,
              ReadBase64(staticAnimation, "scaleFrames"),
              ReadBase64(staticAnimation, "translationFrames"),
              ReadBase64(staticAnimation, "matrices"));
            if (status is not ("native" or "metadataOnly" or "absent"))
            {
              throw new MalformedMetadataException("Unsupported static animation status.");
            }
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
          partitions.AsReadOnly(),
          staticRenderObjectLocalIds,
          sourceObjectLocalIds,
          staticRenderObjectInventory,
          sourceObjectInventory,
          root.TryGetProperty("nextStaticRenderObjectLocalId", out var nextRenderObjectId)
            ? nextRenderObjectId.GetInt32()
            : null,
          root.TryGetProperty("nextSourceObjectLocalId", out var nextSourceObjectId)
            ? nextSourceObjectId.GetInt32()
            : null,
          root.TryGetProperty("textureBinding", out var textureBinding)
            ? Array.AsReadOnly(Convert.FromBase64String(
              textureBinding.GetString()
                ?? throw new MalformedMetadataException("Missing TEX resource binding.")))
            : null,
          animationLengths,
          animationFrameIndices,
          animationClasses.AsReadOnly(),
          animationProjection,
          root.TryGetProperty("attachment", out var attachment)
            ? attachment.GetProperty("physicalNumber").GetInt32()
            : null,
          root.TryGetProperty("attachment", out attachment)
            ? ReadBase64(attachment, "record")
            : null,
          root.TryGetProperty("cannonRenderPosition", out var cannonRenderPosition)
            ? cannonRenderPosition.GetProperty("physicalNumber").GetInt32()
            : null,
          root.TryGetProperty("cannonRenderPosition", out cannonRenderPosition)
            ? ReadBase64(cannonRenderPosition, "record")
            : null);
      }
      catch (UnsupportedMetadataVersionException)
      {
        throw;
      }
      catch (MalformedMetadataException)
      {
        throw;
      }
      catch (Exception ex) when (ex is JsonException
        || ex is InvalidOperationException
        || ex is KeyNotFoundException
        || ex is FormatException)
      {
        throw new MalformedMetadataException("Malformed EarthTool metadata.", ex);
      }
    }

    private static IReadOnlyList<int> ReadIntegerArray(JsonElement root, string propertyName)
    {
      if (!root.TryGetProperty(propertyName, out var array))
      {
        return Array.Empty<int>();
      }

      if (array.ValueKind != JsonValueKind.Array)
      {
        throw new MalformedMetadataException($"{propertyName} must be an array.");
      }

      return Array.AsReadOnly(array.EnumerateArray().Select(item => item.GetInt32()).ToArray());
    }

    private static AnimationClassBytes ReadAnimationBytes(JsonElement array, string propertyName)
    {
      if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() != 4)
      {
        throw new MalformedMetadataException($"{propertyName} must contain four bytes.");
      }
      var values = array.EnumerateArray().Select(item => item.GetByte()).ToArray();
      return new AnimationClassBytes(values[0], values[1], values[2], values[3]);
    }

    private static IReadOnlyList<byte> ReadBase64(JsonElement owner, string propertyName)
    {
      return Array.AsReadOnly(Convert.FromBase64String(
        owner.GetProperty(propertyName).GetString()
          ?? throw new MalformedMetadataException($"Missing {propertyName} animation data.")));
    }

    private static byte[] CreateBinary(
      IReadOnlyList<ProjectedPartition> partitions,
      AnimationProjectionSet animations,
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      bool embedPreviews,
      out IReadOnlyDictionary<StaticRenderObjectId, PartitionLayout> layouts,
      out IReadOnlyDictionary<StaticRenderObjectId, PreviewLayout> previewLayouts,
      out IReadOnlyList<AnimationLayout> animationLayouts)
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
        var indexComponentType = partition.RenderObject.Triangles.Any(triangle =>
          triangle.Vertex0 == ushort.MaxValue
          || triangle.Vertex1 == ushort.MaxValue
          || triangle.Vertex2 == ushort.MaxValue) ? 5125 : 5123;
        foreach (var triangle in partition.RenderObject.Triangles)
        {
          if (indexComponentType == 5125)
          {
            writer.Write((uint)triangle.Vertex0);
            writer.Write((uint)triangle.Vertex1);
            writer.Write((uint)triangle.Vertex2);
          }
          else
          {
            writer.Write(triangle.Vertex0);
            writer.Write(triangle.Vertex1);
            writer.Write(triangle.Vertex2);
          }
        }

        var indexLength = checked(partition.RenderObject.Triangles.Count * 3
          * (indexComponentType == 5125 ? sizeof(uint) : sizeof(ushort)));
        createdLayouts.Add(
          partition.RenderObject.Id,
          new PartitionLayout(
            partition,
            positionOffset,
            normalOffset,
            textureOffset,
            indexOffset,
            indexLength,
            indexComponentType));
        while (stream.Length % 4 != 0)
        {
          writer.Write((byte)0);
        }
      }

      var createdPreviewLayouts = new Dictionary<StaticRenderObjectId, PreviewLayout>();
      var sharedPreviewLayouts = new Dictionary<string, PreviewLayout>(StringComparer.Ordinal);
      foreach (var partition in partitions.Where(partition => previews.ContainsKey(
        partition.RenderObject.Id)))
      {
        var preview = previews[partition.RenderObject.Id];
        if (!sharedPreviewLayouts.TryGetValue(preview.ContentAddress, out var previewLayout))
        {
          if (embedPreviews)
          {
            while (stream.Length % 4 != 0)
            {
              writer.Write((byte)0);
            }
            var offset = checked((int)stream.Position);
            writer.Write(preview.Png);
            previewLayout = new PreviewLayout(offset, preview.Png.Length, null, preview.ContentAddress);
          }
          else
          {
            previewLayout = new PreviewLayout(
              0,
              preview.Png.Length,
              preview.ContentAddress + ".png",
              preview.ContentAddress);
          }
          sharedPreviewLayouts.Add(preview.ContentAddress, previewLayout);
        }
        createdPreviewLayouts.Add(partition.RenderObject.Id, previewLayout);
      }

      var createdAnimationLayouts = new List<AnimationLayout>();
      foreach (var clip in animations.Clips)
      {
        Align(writer, stream);
        var timeOffset = checked((int)stream.Position);
        for (var frame = 0; frame < clip.FrameCount; frame++)
        {
          writer.Write(frame / 24f);
        }

        var objectLayouts = new List<AnimationObjectLayout>();
        foreach (var item in clip.Objects)
        {
          var translationOffset = checked((int)stream.Position);
          foreach (var frame in item.Frames)
          {
            Write(writer, frame.Translation);
          }
          var rotationOffset = checked((int)stream.Position);
          foreach (var frame in item.Frames)
          {
            writer.Write(frame.Rotation.X);
            writer.Write(frame.Rotation.Y);
            writer.Write(frame.Rotation.Z);
            writer.Write(frame.Rotation.W);
          }
          var scaleOffset = checked((int)stream.Position);
          foreach (var frame in item.Frames)
          {
            Write(writer, frame.Scale);
          }
          objectLayouts.Add(new AnimationObjectLayout(
            item,
            translationOffset,
            rotationOffset,
            scaleOffset));
        }
        createdAnimationLayouts.Add(new AnimationLayout(
          clip,
          timeOffset,
          objectLayouts.AsReadOnly()));
      }
      while (stream.Length % 4 != 0)
      {
        writer.Write((byte)0);
      }

      layouts = createdLayouts;
      previewLayouts = createdPreviewLayouts;
      animationLayouts = createdAnimationLayouts.AsReadOnly();
      return stream.ToArray();
    }

    private static byte[] CreateJson(
      StaticMeshAsset asset,
      IReadOnlyDictionary<StaticRenderObjectId, PartitionLayout> layouts,
      int binaryLength,
      InterchangeBaseline baseline,
      string manifest,
      IReadOnlyDictionary<StaticRenderObjectId, PreviewLayout> previewLayouts,
      AnimationProjectionSet animations,
      IReadOnlyList<AnimationLayout> animationLayouts,
      string? bufferFileName)
    {
      var rootSourceObject = asset.RootSourceObject;
      var sources = StaticSourceObjectTraversal.Flatten(rootSourceObject).ToArray();
      var attachments = ProjectAttachments(asset);
      var cannonRenderPositions = ProjectCannonRenderPositions(asset);
      var nodeIndices = sources
        .Select((source, index) => new { source.Id, Index = index })
        .ToDictionary(item => item.Id, item => item.Index);
      var nodeIndicesByLocalId = sources
        .Select((source, index) => new { LocalId = source.Id.Value, Index = index })
        .ToDictionary(item => item.LocalId, item => item.Index);
      var orderedLayouts = sources
        .SelectMany(source => source.StaticRenderObjectIds)
        .Select(id => layouts[id])
        .ToArray();
      var accessorIndices = orderedLayouts
        .Select((layout, index) => new { layout.Partition.RenderObject.Id, Index = index * 4 })
        .ToDictionary(item => item.Id, item => item.Index);
      var materialIndices = orderedLayouts
        .Select((layout, index) => new { layout.Partition.RenderObject.Id, Index = index })
        .ToDictionary(item => item.Id, item => item.Index);
      var orderedPreviewLayouts = orderedLayouts
        .Where(layout => previewLayouts.ContainsKey(layout.Partition.RenderObject.Id))
        .Select(layout => (RenderObjectId: layout.Partition.RenderObject.Id,
          Layout: previewLayouts[layout.Partition.RenderObject.Id]))
        .ToArray();
      var uniquePreviewLayouts = orderedPreviewLayouts
        .GroupBy(preview => preview.Layout.ContentAddress, StringComparer.Ordinal)
        .Select(group => group.First().Layout)
        .ToArray();
      var imageIndices = uniquePreviewLayouts
        .Select((layout, index) => new { layout.ContentAddress, Index = index })
        .ToDictionary(item => item.ContentAddress, item => item.Index, StringComparer.Ordinal);
      var previewIndices = orderedPreviewLayouts
        .Select(preview => new
        {
          preview.RenderObjectId,
          Index = imageIndices[preview.Layout.ContentAddress]
        })
        .ToDictionary(item => item.RenderObjectId, item => item.Index);
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        writer.WriteStartObject();
        writer.WriteStartObject("asset");
        writer.WriteString("version", "2.0");
        writer.WriteString("generator", "EarthTool");
        writer.WriteEndObject();
        writer.WriteStartArray("extensionsUsed");
        writer.WriteStringValue("KHR_materials_unlit");
        writer.WriteEndArray();
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
          var effectivePivot = layouts[source.StaticRenderObjectIds[0]].Partition.RenderObject.Pivot;
          var translation = ProjectToGltf(effectivePivot);
          if (translation != Vector3.Zero)
          {
            writer.WriteStartArray("translation");
            writer.WriteNumberValue(translation.X);
            writer.WriteNumberValue(translation.Y);
            writer.WriteNumberValue(translation.Z);
            writer.WriteEndArray();
          }
          var isRoot = source.Id.Equals(rootSourceObject.Id);
          if (source.Children.Count > 0 || isRoot && (attachments.Count > 0 || cannonRenderPositions.Count > 0))
          {
            writer.WriteStartArray("children");
            foreach (var child in source.Children)
            {
              writer.WriteNumberValue(nodeIndices[child.Id]);
            }
            if (isRoot)
            {
              var helperIndex = sources.Length;
              for (var index = 0; index < attachments.Count + cannonRenderPositions.Count; index++)
              {
                writer.WriteNumberValue(helperIndex + index);
              }
            }

            writer.WriteEndArray();
          }

          WriteExtras(writer, CreateMetadata(
            baseline,
            "object",
            source.Id.Value,
            null,
            null,
            animationProjection: animations.Objects.SingleOrDefault(item =>
              item.SourceObjectLocalId == source.Id.Value)));
          writer.WriteEndObject();
        }
        foreach (var attachment in attachments)
        {
          writer.WriteStartObject();
          writer.WriteString("name", GetAttachmentHelperName(attachment.PhysicalNumber));
          WriteTransform(writer, attachment.Translation, attachment.Rotation);
          WriteExtras(writer, CreateAttachmentMetadata(baseline, attachment));
          writer.WriteEndObject();
        }
        foreach (var cannon in cannonRenderPositions)
        {
          writer.WriteStartObject();
          writer.WriteString("name", $"ET_CannonRenderPosition_{cannon.PhysicalNumber}");
          WriteTransform(writer, cannon.Translation, Quaternion.Identity);
          WriteExtras(writer, CreateCannonRenderPositionMetadata(baseline, cannon));
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
            writer.WriteNumber("material", materialIndices[renderObjectId]);
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
        writer.WriteStartArray("materials");
        foreach (var layout in orderedLayouts)
        {
          var renderObject = layout.Partition.RenderObject;
          writer.WriteStartObject();
          writer.WriteString("name", $"TEX preview {renderObject.LocalId}");
          writer.WriteStartObject("pbrMetallicRoughness");
          writer.WriteStartArray("baseColorFactor");
          writer.WriteNumberValue(1);
          writer.WriteNumberValue(1);
          writer.WriteNumberValue(1);
          writer.WriteNumberValue(1);
          writer.WriteEndArray();
          writer.WriteNumber("metallicFactor", 0);
          writer.WriteNumber("roughnessFactor", 1);
          if (previewIndices.TryGetValue(renderObject.Id, out var previewIndex))
          {
            writer.WriteStartObject("baseColorTexture");
            writer.WriteNumber("index", previewIndex);
            writer.WriteEndObject();
          }
          writer.WriteEndObject();
          writer.WriteStartObject("extensions");
          writer.WriteStartObject("KHR_materials_unlit");
          writer.WriteEndObject();
          writer.WriteEndObject();
          WriteExtras(writer, CreateMaterialMetadata(baseline, renderObject));
          writer.WriteEndObject();
        }
        writer.WriteEndArray();
        if (uniquePreviewLayouts.Length > 0)
        {
          writer.WriteStartArray("textures");
          for (var index = 0; index < uniquePreviewLayouts.Length; index++)
          {
            writer.WriteStartObject();
            writer.WriteNumber("source", index);
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
          writer.WriteStartArray("images");
          for (var index = 0; index < uniquePreviewLayouts.Length; index++)
          {
            var preview = uniquePreviewLayouts[index];
            writer.WriteStartObject();
            writer.WriteString("name", $"Decoded TEX preview {index + 1}");
            if (preview.Uri is null)
            {
              writer.WriteNumber("bufferView", orderedLayouts.Length * 4 + index);
            }
            else
            {
              writer.WriteString("uri", preview.Uri);
            }
            writer.WriteString("mimeType", "image/png");
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
        }
        if (animationLayouts.Count > 0)
        {
          var firstAnimationAccessor = orderedLayouts.Length * 4;
          writer.WriteStartArray("animations");
          foreach (var animation in animationLayouts)
          {
            writer.WriteStartObject();
            writer.WriteString("name", animation.Clip.Name);
            writer.WriteStartArray("samplers");
            var timeAccessor = firstAnimationAccessor++;
            foreach (var item in animation.Objects)
            {
              for (var path = 0; path < 3; path++)
              {
                writer.WriteStartObject();
                writer.WriteNumber("input", timeAccessor);
                writer.WriteNumber("output", firstAnimationAccessor++);
                writer.WriteString("interpolation", "LINEAR");
                writer.WriteEndObject();
              }
            }
            writer.WriteEndArray();
            writer.WriteStartArray("channels");
            var sampler = 0;
            foreach (var item in animation.Objects)
            {
              foreach (var path in new[] { "translation", "rotation", "scale" })
              {
                writer.WriteStartObject();
                writer.WriteNumber("sampler", sampler++);
                writer.WriteStartObject("target");
                writer.WriteNumber("node", nodeIndicesByLocalId[item.Projection.SourceObjectLocalId]);
                writer.WriteString("path", path);
                writer.WriteEndObject();
                writer.WriteEndObject();
              }
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
        }
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
        foreach (var preview in uniquePreviewLayouts.Where(preview => preview.Uri is null))
        {
          WriteBufferView(writer, preview.Offset, preview.Length, null);
        }
        foreach (var animation in animationLayouts)
        {
          WriteBufferView(writer, animation.TimeOffset, animation.Clip.FrameCount * sizeof(float), null);
          foreach (var item in animation.Objects)
          {
            WriteBufferView(writer, item.TranslationOffset, animation.Clip.FrameCount * 12, null);
            WriteBufferView(writer, item.RotationOffset, animation.Clip.FrameCount * 16, null);
            WriteBufferView(writer, item.ScaleOffset, animation.Clip.FrameCount * 12, null);
          }
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
            layout.IndexComponentType,
            layout.Partition.RenderObject.Triangles.Count * 3,
            "SCALAR");
        }
        var firstAnimationBufferView = orderedLayouts.Length * 4
          + uniquePreviewLayouts.Count(preview => preview.Uri is null);
        foreach (var animation in animationLayouts)
        {
          WriteScalarAccessor(
            writer,
            firstAnimationBufferView++,
            animation.Clip.FrameCount,
            0,
            (animation.Clip.FrameCount - 1) / 24f);
          foreach (var item in animation.Objects)
          {
            WriteAccessor(writer, firstAnimationBufferView++, 5126, animation.Clip.FrameCount, "VEC3");
            WriteAccessor(writer, firstAnimationBufferView++, 5126, animation.Clip.FrameCount, "VEC4");
            WriteAccessor(writer, firstAnimationBufferView++, 5126, animation.Clip.FrameCount, "VEC3");
          }
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

    private static void WriteTransform(Utf8JsonWriter writer, Vector3 translation, Quaternion rotation)
    {
      if (translation != Vector3.Zero)
      {
        writer.WriteStartArray("translation");
        writer.WriteNumberValue(translation.X);
        writer.WriteNumberValue(translation.Y);
        writer.WriteNumberValue(translation.Z);
        writer.WriteEndArray();
      }
      if (rotation != Quaternion.Identity)
      {
        writer.WriteStartArray("rotation");
        writer.WriteNumberValue(rotation.X);
        writer.WriteNumberValue(rotation.Y);
        writer.WriteNumberValue(rotation.Z);
        writer.WriteNumberValue(rotation.W);
        writer.WriteEndArray();
      }
    }

    private static void WriteBufferView(Utf8JsonWriter writer, int offset, int length, int? target)
    {
      writer.WriteStartObject();
      writer.WriteNumber("buffer", 0);
      writer.WriteNumber("byteOffset", offset);
      writer.WriteNumber("byteLength", length);
      if (target.HasValue)
      {
        writer.WriteNumber("target", target.Value);
      }
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

    private static void WriteScalarAccessor(
      Utf8JsonWriter writer,
      int bufferView,
      int count,
      float minimum,
      float maximum)
    {
      writer.WriteStartObject();
      writer.WriteNumber("bufferView", bufferView);
      writer.WriteNumber("componentType", 5126);
      writer.WriteNumber("count", count);
      writer.WriteString("type", "SCALAR");
      writer.WriteStartArray("min");
      writer.WriteNumberValue(minimum);
      writer.WriteEndArray();
      writer.WriteStartArray("max");
      writer.WriteNumberValue(maximum);
      writer.WriteEndArray();
      writer.WriteEndObject();
    }

    private static void Write(BinaryWriter writer, Vector3 value)
    {
      writer.Write(value.X);
      writer.Write(value.Y);
      writer.Write(value.Z);
    }

    private static void Align(BinaryWriter writer, MemoryStream stream)
    {
      while (stream.Length % 4 != 0)
      {
        writer.Write((byte)0);
      }
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
      string? fingerprint,
      StaticMeshAsset? sourceAsset = null,
      AnimationProjectionSet? animations = null,
      ProjectedAnimationObject? animationProjection = null)
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

        if (sourceAsset is not null)
        {
          writer.WriteStartArray("staticRenderObjectLocalIds");
          foreach (var record in sourceAsset.StaticRenderObjectSequence)
          {
            writer.WriteNumberValue(record.LocalId);
          }
          writer.WriteEndArray();
          writer.WriteStartArray("sourceObjectLocalIds");
          foreach (var source in StaticSourceObjectTraversal.Flatten(sourceAsset.RootSourceObject))
          {
            writer.WriteNumberValue(source.Id.Value);
          }
          writer.WriteEndArray();
          writer.WriteStartArray("staticRenderObjectInventory");
          foreach (var record in sourceAsset.StaticRenderObjectSequence.OrderBy(record => record.LocalId))
          {
            writer.WriteNumberValue(record.LocalId);
          }
          writer.WriteEndArray();
          writer.WriteStartArray("sourceObjectInventory");
          foreach (var source in StaticSourceObjectTraversal.Flatten(sourceAsset.RootSourceObject)
            .OrderBy(source => source.Id.Value))
          {
            writer.WriteNumberValue(source.Id.Value);
          }
          writer.WriteEndArray();
          if (sourceAsset.NextStaticRenderObjectLocalId.HasValue)
          {
            writer.WriteNumber(
              "nextStaticRenderObjectLocalId",
              sourceAsset.NextStaticRenderObjectLocalId.Value);
          }
          if (sourceAsset.NextSourceObjectLocalId.HasValue)
          {
            writer.WriteNumber("nextSourceObjectLocalId", sourceAsset.NextSourceObjectLocalId.Value);
          }
          if (animations is not null)
          {
            writer.WriteStartObject("staticAnimation");
            WriteAnimationBytes(writer, "lengths", sourceAsset.CommonBaseHeader.AnimationLengths);
            WriteAnimationBytes(writer, "frameIndices", sourceAsset.CommonBaseHeader.AnimationFrameIndices);
            writer.WriteStartArray("classes");
            foreach (var group in animations.Objects.Where(item => item.HasSourceTracks)
              .GroupBy(item => item.ClassIndex).OrderBy(group => group.Key))
            {
              var clip = animations.Clips.SingleOrDefault(item => item.ClassIndex == group.Key);
              writer.WriteStartObject();
              writer.WriteNumber("class", group.Key);
              writer.WriteStartArray("objects");
              foreach (var item in group.OrderBy(item => item.SourceObjectLocalId))
              {
                writer.WriteNumberValue(item.SourceObjectLocalId);
              }
              writer.WriteEndArray();
              writer.WriteStartArray("nativeObjects");
              foreach (var item in group.Where(item => item.IsNative)
                .OrderBy(item => item.SourceObjectLocalId))
              {
                writer.WriteNumberValue(item.SourceObjectLocalId);
              }
              writer.WriteEndArray();
              if (clip is not null)
              {
                writer.WriteString("sha256", clip.Fingerprint);
              }
              writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
          }
        }

        if (animationProjection is not null)
        {
          writer.WriteStartObject("staticAnimation");
          writer.WriteNumber("animationClassValue", animationProjection.AnimationClassValue);
          writer.WriteNumber("class", animationProjection.ClassIndex);
          writer.WriteNumber("declaredLength", animationProjection.DeclaredLength);
          writer.WriteString(
            "status",
            animationProjection.IsNative
              ? "native"
              : animationProjection.HasSourceTracks ? "metadataOnly" : "absent");
          writer.WriteString(
            "scaleFrames",
            Convert.ToBase64String(StaticAnimationProjection.SerializeScaleFrames(
              animationProjection.SourceTracks)));
          writer.WriteString(
            "translationFrames",
            Convert.ToBase64String(StaticAnimationProjection.SerializeTranslationFrames(
              animationProjection.SourceTracks)));
          writer.WriteString(
            "matrices",
            Convert.ToBase64String(StaticAnimationProjection.SerializeMatrices(
              animationProjection.SourceTracks)));
          if (animationProjection.Fingerprint is not null)
          {
            writer.WriteString("sha256", animationProjection.Fingerprint);
          }
          writer.WriteEndObject();
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

    private static void WriteAnimationBytes(
      Utf8JsonWriter writer,
      string propertyName,
      AnimationClassBytes value)
    {
      writer.WriteStartArray(propertyName);
      writer.WriteNumberValue(value.A);
      writer.WriteNumberValue(value.B);
      writer.WriteNumberValue(value.C);
      writer.WriteNumberValue(value.D);
      writer.WriteEndArray();
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

    private static string CreateMaterialMetadata(
      InterchangeBaseline baseline,
      StaticRenderObject renderObject)
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
        writer.WriteString("kind", "material");
        writer.WriteNumber("localId", renderObject.LocalId);
        writer.WriteEndObject();
        writer.WriteString(
          "textureBinding",
          Convert.ToBase64String(renderObject.TexturePathBytes.ToArray()));
        writer.WriteEndObject();
      }

      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateAttachmentMetadata(
      InterchangeBaseline baseline,
      ProjectedAttachment attachment)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataHeader(writer, baseline, "object", attachment.PhysicalNumber);
        writer.WriteStartObject("nativeProjection");
        writer.WriteString("name", "attachment.pose");
        writer.WriteNumber("version", 1);
        writer.WriteString("sha256", CreateAttachmentPoseFingerprint(
          baseline,
          attachment.PhysicalNumber,
          attachment.Record));
        writer.WriteEndObject();
        writer.WriteStartObject("attachment");
        writer.WriteNumber("physicalNumber", attachment.PhysicalNumber);
        writer.WriteString("record", Convert.ToBase64String(attachment.Record));
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateCannonRenderPositionMetadata(
      InterchangeBaseline baseline,
      ProjectedCannonRenderPosition cannon)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataHeader(writer, baseline, "object", cannon.PhysicalNumber);
        writer.WriteStartObject("nativeProjection");
        writer.WriteString("name", "cannonRenderPosition.position");
        writer.WriteNumber("version", 1);
        writer.WriteString("sha256", CreateCannonRenderPositionFingerprint(
          baseline,
          cannon.PhysicalNumber,
          cannon.Record));
        writer.WriteEndObject();
        writer.WriteStartObject("cannonRenderPosition");
        writer.WriteNumber("physicalNumber", cannon.PhysicalNumber);
        writer.WriteString("record", Convert.ToBase64String(cannon.Record));
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static string CreateAttachmentPoseFingerprint(
      InterchangeBaseline baseline,
      int physicalNumber,
      IReadOnlyList<byte> record)
    {
      var bytes = record.ToArray();
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        WriteFingerprintHeader(writer, baseline, "attachment.pose", "object", physicalNumber);
        writer.Write(physicalNumber);
        writer.Write(BinaryPrimitives.ReadInt16LittleEndian(bytes));
        writer.Write(BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(2)));
        writer.Write(BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(4)));
        writer.Write(bytes[6]);
      }
      return Hash(preimage.ToArray());
    }

    internal static string CreateCannonRenderPositionFingerprint(
      InterchangeBaseline baseline,
      int physicalNumber,
      IReadOnlyList<byte> record)
    {
      var bytes = record.ToArray();
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        WriteFingerprintHeader(
          writer,
          baseline,
          "cannonRenderPosition.position",
          "object",
          physicalNumber);
        writer.Write(physicalNumber);
        WriteCanonicalFloat(writer, ReadFinitePreview(bytes, 0));
        WriteCanonicalFloat(writer, ReadFinitePreview(bytes, 8));
        WriteCanonicalFloat(writer, ReadFinitePreview(bytes, 4));
      }
      return Hash(preimage.ToArray());
    }

    private static void WriteFingerprintHeader(
      BinaryWriter writer,
      InterchangeBaseline baseline,
      string projection,
      string scopeKind,
      int localId)
    {
      WriteFingerprintString(writer, "earthtool.msh.gltf");
      writer.Write(1);
      WriteFingerprintString(writer, projection);
      writer.Write(1);
      writer.Write(baseline.AssetLineageId.ToByteArray());
      writer.Write(baseline.DocumentId.ToByteArray());
      WriteFingerprintString(writer, scopeKind);
      writer.Write(localId);
    }

    private static void WriteFingerprintString(BinaryWriter writer, string value)
    {
      var bytes = Encoding.UTF8.GetBytes(value);
      writer.Write(bytes.Length);
      writer.Write(bytes);
    }

    private static void WriteCanonicalFloat(BinaryWriter writer, float value)
    {
      writer.Write(value == 0 ? 0 : value);
    }

    private static void WriteMetadataHeader(
      Utf8JsonWriter writer,
      InterchangeBaseline baseline,
      string scopeKind,
      int localId)
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
    }

    private static void ValidateSupportedGraph(
      JsonElement root,
      GltfOperationProfile profile,
      GltfImportIntent intent)
    {
      var nodes = root.GetProperty("nodes");
      var meshes = root.GetProperty("meshes");
      var materialCount = root.TryGetProperty("materials", out var materials)
        ? materials.GetArrayLength()
        : 0;
      if (nodes.GetArrayLength() > profile.MaxNodes)
      {
        throw new ResourceLimitException(nodes.GetArrayLength(), profile.MaxNodes);
      }
      if (root.GetProperty("scene").GetInt32() != 0
        || root.GetProperty("scenes").GetArrayLength() != 1
        || nodes.GetArrayLength() == 0
        || meshes.GetArrayLength() == 0
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

      var referencedMeshes = new HashSet<int>();
      var parentCounts = new int[nodes.GetArrayLength()];
      for (var index = 0; index < nodes.GetArrayLength(); index++)
      {
        var node = nodes[index];
        if (node.TryGetProperty("mesh", out var mesh)
          && (mesh.GetInt32() < 0
            || mesh.GetInt32() >= meshes.GetArrayLength()))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        if (mesh.ValueKind != JsonValueKind.Undefined)
        {
          referencedMeshes.Add(mesh.GetInt32());
        }
        if (node.TryGetProperty("matrix", out _)
          && (node.TryGetProperty("translation", out _)
            || node.TryGetProperty("rotation", out _)
            || node.TryGetProperty("scale", out _)))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        if (node.TryGetProperty("skin", out _)
          || node.TryGetProperty("camera", out _))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }

        if (node.TryGetProperty("children", out var children))
        {
          foreach (var child in children.EnumerateArray())
          {
            var childIndex = child.GetInt32();
            if (childIndex < 0
              || childIndex >= nodes.GetArrayLength()
              || childIndex == index
              || ++parentCounts[childIndex] > 1)
            {
              throw new UnsupportedGltfDomainException("TransformOrHierarchy");
            }
          }
        }
      }

      if (referencedMeshes.Count != meshes.GetArrayLength()
        || parentCounts[sceneNodes[0].GetInt32()] != 0
        || parentCounts.Where((_, index) => index != sceneNodes[0].GetInt32())
          .Any(count => count != 1))
      {
        throw new UnsupportedGltfDomainException("SceneMembership");
      }

      var reachable = new HashSet<int>();
      var pending = new Stack<(int NodeIndex, int Depth)>();
      pending.Push((sceneNodes[0].GetInt32(), 1));
      while (pending.Count > 0)
      {
        var current = pending.Pop();
        if (current.Depth > profile.MaxHierarchyDepth)
        {
          throw new ResourceLimitException(current.Depth, profile.MaxHierarchyDepth);
        }
        if (!reachable.Add(current.NodeIndex))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        if (nodes[current.NodeIndex].TryGetProperty("children", out var children))
        {
          foreach (var child in children.EnumerateArray())
          {
            pending.Push((child.GetInt32(), current.Depth + 1));
          }
        }
      }
      if (reachable.Count != nodes.GetArrayLength())
      {
        throw new UnsupportedGltfDomainException("SceneMembership");
      }

      var primitiveCounts = meshes.EnumerateArray()
        .Select(mesh => mesh.GetProperty("primitives").GetArrayLength())
        .ToArray();
      long expandedPartitionCount = 0;
      foreach (var node in nodes.EnumerateArray())
      {
        if (node.TryGetProperty("mesh", out var mesh))
        {
          expandedPartitionCount = checked(expandedPartitionCount + primitiveCounts[mesh.GetInt32()]);
        }
      }
      if (expandedPartitionCount > MshOperationProfile.Default.MaxStaticRenderObjects)
      {
        throw new ResourceLimitException(
          expandedPartitionCount,
          MshOperationProfile.Default.MaxStaticRenderObjects);
      }

      var supportedAttributes = new HashSet<string>(StringComparer.Ordinal)
      {
        "POSITION",
        "NORMAL",
        "TEXCOORD_0"
      };
      var accessors = root.GetProperty("accessors");
      var geometryByMesh = new List<IReadOnlyList<(int VertexCount, int TriangleCount)>>();
      foreach (var mesh in meshes.EnumerateArray())
      {
        var primitives = mesh.GetProperty("primitives");
        if (primitives.GetArrayLength() == 0)
        {
          throw new UnsupportedGltfDomainException("Geometry");
        }

        var geometry = new List<(int VertexCount, int TriangleCount)>();
        foreach (var primitive in primitives.EnumerateArray())
        {
          if (primitive.TryGetProperty("material", out var material)
            && (material.GetInt32() < 0 || material.GetInt32() >= materialCount))
          {
            throw new UnsupportedGltfDomainException("materials");
          }
          var attributes = primitive.GetProperty("attributes");
          if (attributes.EnumerateObject().Any(attribute => !supportedAttributes.Contains(attribute.Name))
            || !attributes.TryGetProperty("POSITION", out _)
            || !attributes.TryGetProperty("NORMAL", out _)
            || intent == GltfImportIntent.Edit && !attributes.TryGetProperty("TEXCOORD_0", out _)
            || primitive.TryGetProperty("targets", out _))
          {
            throw new UnsupportedGltfDomainException("PrimitiveAttributes");
          }

          foreach (var attribute in attributes.EnumerateObject())
          {
            var count = accessors[attribute.Value.GetInt32()]
              .GetProperty("count").GetInt32();
            if (count > profile.MaxActiveRenderVertices)
            {
              throw new ResourceLimitException(count, profile.MaxActiveRenderVertices);
            }
          }

          var vertexCount = accessors[attributes.GetProperty("POSITION").GetInt32()]
            .GetProperty("count").GetInt32();
          var indexCount = primitive.TryGetProperty("indices", out var indices)
            ? accessors[indices.GetInt32()].GetProperty("count").GetInt32()
            : vertexCount;
          var triangleCount = indexCount / 3;
          if (triangleCount > MshOperationProfile.Default.MaxStaticTrianglesPerObject)
          {
            throw new ResourceLimitException(
              triangleCount,
              MshOperationProfile.Default.MaxStaticTrianglesPerObject);
          }
          geometry.Add((vertexCount, triangleCount));
        }
        geometryByMesh.Add(geometry.AsReadOnly());
      }

      long serializedLength;
      try
      {
        serializedLength = EarthTool.MSH.Internal.MshCanonicalSerializer.GetCanonicalStaticSerializedLength(
          nodes.EnumerateArray()
            .Where(node => node.TryGetProperty("mesh", out _))
            .SelectMany(node => geometryByMesh[node.GetProperty("mesh").GetInt32()]));
      }
      catch (OverflowException)
      {
        throw new ResourceLimitException(long.MaxValue, profile.MaxOutputBytes);
      }
      if (serializedLength > profile.MaxOutputBytes)
      {
        throw new ResourceLimitException(serializedLength, profile.MaxOutputBytes);
      }

      ValidateMaterials(root);
      ValidateTexturePreviews(root);

      foreach (var domain in new[] { "skins", "cameras", "samplers" })
      {
        if (root.TryGetProperty(domain, out _))
        {
          throw new UnsupportedGltfDomainException(domain);
        }
      }
    }

    private static void ValidateMaterials(JsonElement root)
    {
      if (!root.TryGetProperty("materials", out var materials))
      {
        return;
      }
      if (!root.TryGetProperty("extensionsUsed", out var extensionsUsed)
        || !extensionsUsed.EnumerateArray().Any(extension =>
          extension.ValueKind == JsonValueKind.String
          && extension.GetString() == "KHR_materials_unlit"))
      {
        throw new UnsupportedGltfDomainException("materials");
      }

      var allowedMaterialProperties = new HashSet<string>(StringComparer.Ordinal)
      {
        "name",
        "pbrMetallicRoughness",
        "extensions",
        "extras"
      };
      var allowedPbrProperties = new HashSet<string>(StringComparer.Ordinal)
      {
        "baseColorFactor",
        "baseColorTexture",
        "metallicFactor",
        "roughnessFactor"
      };
      foreach (var material in materials.EnumerateArray())
      {
        if (material.EnumerateObject().Any(property =>
            !allowedMaterialProperties.Contains(property.Name))
          || !material.TryGetProperty("extensions", out var extensions)
          || extensions.ValueKind != JsonValueKind.Object
          || extensions.EnumerateObject().Count() != 1
          || !extensions.TryGetProperty("KHR_materials_unlit", out var unlit)
          || unlit.ValueKind != JsonValueKind.Object
          || unlit.EnumerateObject().Any())
        {
          throw new UnsupportedGltfDomainException("materials");
        }

        if (!material.TryGetProperty("pbrMetallicRoughness", out var pbr))
        {
          continue;
        }
        if (pbr.ValueKind != JsonValueKind.Object
          || pbr.EnumerateObject().Any(property => !allowedPbrProperties.Contains(property.Name))
          || pbr.TryGetProperty("metallicFactor", out var metallic)
            && metallic.GetSingle() != 0)
        {
          throw new UnsupportedGltfDomainException("materials");
        }
        if (pbr.TryGetProperty("baseColorFactor", out var baseColor)
          && (baseColor.ValueKind != JsonValueKind.Array
            || baseColor.GetArrayLength() != 4
            || baseColor.EnumerateArray().Any(value => value.GetSingle() != 1)))
        {
          throw new UnsupportedGltfDomainException("materials");
        }
        if (pbr.TryGetProperty("baseColorTexture", out var baseColorTexture)
          && (baseColorTexture.ValueKind != JsonValueKind.Object
            || baseColorTexture.EnumerateObject().Any(property => property.Name != "index")
            || !baseColorTexture.TryGetProperty("index", out _)))
        {
          throw new UnsupportedGltfDomainException("materials");
        }
      }
    }

    private static void ValidateTexturePreviews(JsonElement root)
    {
      var hasTextures = root.TryGetProperty("textures", out var textures);
      var hasImages = root.TryGetProperty("images", out var images);
      if (hasTextures != hasImages)
      {
        throw new UnsupportedGltfDomainException("TexturePreviews");
      }
      if (!hasTextures)
      {
        return;
      }
      if (textures.ValueKind != JsonValueKind.Array
        || images.ValueKind != JsonValueKind.Array
        || textures.GetArrayLength() != images.GetArrayLength())
      {
        throw new UnsupportedGltfDomainException("TexturePreviews");
      }
      for (var index = 0; index < textures.GetArrayLength(); index++)
      {
        var texture = textures[index];
        var image = images[index];
        if (texture.ValueKind != JsonValueKind.Object
          || texture.EnumerateObject().Any(property => property.Name is not ("source" or "name"))
          || texture.GetProperty("source").GetInt32() != index
          || image.ValueKind != JsonValueKind.Object
          || image.EnumerateObject().Any(property =>
            property.Name is not ("name" or "bufferView" or "uri" or "mimeType"))
          || image.GetProperty("mimeType").GetString() != "image/png"
          || image.TryGetProperty("bufferView", out var bufferView)
            == image.TryGetProperty("uri", out var uri)
          || bufferView.ValueKind != JsonValueKind.Undefined
            && (bufferView.GetInt32() < 0
              || bufferView.GetInt32() >= root.GetProperty("bufferViews").GetArrayLength())
          || uri.ValueKind != JsonValueKind.Undefined
            && (uri.ValueKind != JsonValueKind.String
              || string.IsNullOrEmpty(uri.GetString())))
        {
          throw new UnsupportedGltfDomainException("TexturePreviews");
        }
      }
      foreach (var material in root.GetProperty("materials").EnumerateArray())
      {
        if (material.TryGetProperty("pbrMetallicRoughness", out var pbr)
          && pbr.TryGetProperty("baseColorTexture", out var baseColorTexture)
          && (baseColorTexture.GetProperty("index").GetInt32() < 0
            || baseColorTexture.GetProperty("index").GetInt32() >= textures.GetArrayLength()))
        {
          throw new UnsupportedGltfDomainException("TexturePreviews");
        }
      }
    }

    internal static RenderVertex ProjectToGltf(RenderVertex vertex)
    {
      return new RenderVertex(
        new Vector3(vertex.Position.X, vertex.Position.Z, -vertex.Position.Y),
        new Vector3(vertex.Normal.X, vertex.Normal.Z, -vertex.Normal.Y),
        vertex.TextureCoordinate);
    }

    internal static Vector3 ProjectToGltf(Vector3 value)
    {
      return new Vector3(value.X, value.Z, -value.Y);
    }

    private static IReadOnlyList<ProjectedAttachment> ProjectAttachments(StaticMeshAsset asset)
    {
      var table = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var result = new List<ProjectedAttachment>();
      for (var physicalNumber = 1; physicalNumber <= 49; physicalNumber++)
      {
        if (physicalNumber is >= 13 and <= 20)
        {
          continue;
        }
        var offset = (physicalNumber - 1) * 8;
        var record = table.AsSpan(offset, 8).ToArray();
        var x = BinaryPrimitives.ReadInt16LittleEndian(record);
        if (x == short.MinValue)
        {
          continue;
        }
        var storedNegativeY = BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(2));
        var z = BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(4));
        var headingRadians = record[6] * (MathF.PI * 2 / 256f);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, headingRadians - (MathF.PI / 2));
        if (rotation.W < 0)
        {
          rotation = new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W);
        }
        result.Add(new ProjectedAttachment(
          physicalNumber,
          record,
          new Vector3(x / 256f, z / 256f, storedNegativeY / 256f),
          rotation));
      }
      return result.AsReadOnly();
    }

    private static IReadOnlyList<ProjectedCannonRenderPosition> ProjectCannonRenderPositions(
      StaticMeshAsset asset)
    {
      var records = asset.CommonBaseHeader.CannonRenderPositions.ToArray();
      var result = new ProjectedCannonRenderPosition[4];
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        var record = records.AsSpan((physicalNumber - 1) * 12, 12).ToArray();
        var x = ReadFinitePreview(record, 0);
        var storedNegativeY = ReadFinitePreview(record, 4);
        var z = ReadFinitePreview(record, 8);
        result[physicalNumber - 1] = new ProjectedCannonRenderPosition(
          physicalNumber,
          record,
          new Vector3(x, z, storedNegativeY));
      }
      return Array.AsReadOnly(result);
    }

    internal static float ReadFinitePreview(byte[] record, int offset)
    {
      var value = BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(offset)));
      return float.IsFinite(value) ? value : 0;
    }

    internal static string GetAttachmentHelperName(int physicalNumber)
    {
      var (range, localNumber) = physicalNumber switch
      {
        <= 4 => ("Cannon", physicalNumber),
        <= 8 => ("Marker", physicalNumber - 4),
        <= 12 => ("SS", physicalNumber - 8),
        <= 16 => ("SpotLight", physicalNumber - 12),
        <= 20 => ("OmniLight", physicalNumber - 16),
        <= 24 => ("Transport", physicalNumber - 20),
        <= 28 => ("HT", physicalNumber - 24),
        <= 32 => ("SmokeEffect", physicalNumber - 28),
        <= 36 => ("WT", physicalNumber - 32),
        <= 38 => ("CH", physicalNumber - 36),
        <= 40 => ("ST", physicalNumber - 38),
        <= 42 => ("SE", physicalNumber - 40),
        <= 44 => ("SK", physicalNumber - 42),
        45 => ("ChildAlignment", 1),
        46 => ("Center", 1),
        47 => ("Production", 1),
        48 => ("Movement", 1),
        _ => ("Landing", 1)
      };
      return $"ET_Attachment_{physicalNumber:00}_{range}_{localNumber}";
    }

    internal static bool TryParseAttachmentHelperName(string? name, out int physicalNumber)
    {
      for (physicalNumber = 1; physicalNumber <= 49; physicalNumber++)
      {
        if (physicalNumber is not (>= 13 and <= 20)
          && string.Equals(name, GetAttachmentHelperName(physicalNumber), StringComparison.Ordinal))
        {
          return true;
        }
      }
      physicalNumber = 0;
      return false;
    }

    internal static bool TryParseCannonRenderPositionHelperName(string? name, out int physicalNumber)
    {
      for (physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        if (string.Equals(name, $"ET_CannonRenderPosition_{physicalNumber}", StringComparison.Ordinal))
        {
          return true;
        }
      }
      physicalNumber = 0;
      return false;
    }

    private sealed class ProjectedAttachment
    {
      internal int PhysicalNumber { get; }
      internal byte[] Record { get; }
      internal Vector3 Translation { get; }
      internal Quaternion Rotation { get; }

      internal ProjectedAttachment(
        int physicalNumber,
        byte[] record,
        Vector3 translation,
        Quaternion rotation)
      {
        PhysicalNumber = physicalNumber;
        Record = record;
        Translation = translation;
        Rotation = rotation;
      }
    }

    private sealed class ProjectedCannonRenderPosition
    {
      internal int PhysicalNumber { get; }
      internal byte[] Record { get; }
      internal Vector3 Translation { get; }

      internal ProjectedCannonRenderPosition(int physicalNumber, byte[] record, Vector3 translation)
      {
        PhysicalNumber = physicalNumber;
        Record = record;
        Translation = translation;
      }
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

      internal int IndexComponentType { get; }

      internal PartitionLayout(
        ProjectedPartition partition,
        int positionOffset,
        int normalOffset,
        int textureOffset,
        int indexOffset,
        int indexLength,
        int indexComponentType)
      {
        Partition = partition;
        PositionOffset = positionOffset;
        NormalOffset = normalOffset;
        TextureOffset = textureOffset;
        IndexOffset = indexOffset;
        IndexLength = indexLength;
        IndexComponentType = indexComponentType;
      }
    }

    private sealed class PreviewLayout
    {
      internal int Offset { get; }

      internal int Length { get; }

      internal string? Uri { get; }

      internal string ContentAddress { get; }

      internal PreviewLayout(int offset, int length, string? uri, string contentAddress)
      {
        Offset = offset;
        Length = length;
        Uri = uri;
        ContentAddress = contentAddress;
      }
    }

    private sealed class AnimationLayout
    {
      internal ProjectedAnimationClip Clip { get; }

      internal int TimeOffset { get; }

      internal IReadOnlyList<AnimationObjectLayout> Objects { get; }

      internal AnimationLayout(
        ProjectedAnimationClip clip,
        int timeOffset,
        IReadOnlyList<AnimationObjectLayout> objects)
      {
        Clip = clip;
        TimeOffset = timeOffset;
        Objects = objects;
      }
    }

    private sealed class AnimationObjectLayout
    {
      internal ProjectedAnimationObject Projection { get; }

      internal int TranslationOffset { get; }

      internal int RotationOffset { get; }

      internal int ScaleOffset { get; }

      internal AnimationObjectLayout(
        ProjectedAnimationObject projection,
        int translationOffset,
        int rotationOffset,
        int scaleOffset)
      {
        Projection = projection;
        TranslationOffset = translationOffset;
        RotationOffset = rotationOffset;
        ScaleOffset = scaleOffset;
      }
    }

    private static string GetMetadata(JsonElement owner, string ownerName)
    {
      return TryGetMetadata(owner) ?? throw new MissingMetadataException(ownerName);
    }

    private static string? TryGetMetadata(JsonElement owner)
    {
      if (!owner.TryGetProperty("extras", out var extras)
        || !extras.TryGetProperty("earthtool", out var metadata))
      {
        return null;
      }

      if (metadata.ValueKind != JsonValueKind.String)
      {
        throw new InvalidDataException("EarthTool metadata must be a string.");
      }

      return metadata.GetString() ?? throw new InvalidDataException("EarthTool metadata cannot be null.");
    }

    private static bool HasReservedMetadata(JsonElement element)
    {
      if (element.ValueKind == JsonValueKind.Object)
      {
        if (element.TryGetProperty("extras", out var extras)
          && extras.ValueKind == JsonValueKind.Object
          && extras.TryGetProperty("earthtool", out _))
        {
          return true;
        }

        return element.EnumerateObject().Any(property => HasReservedMetadata(property.Value));
      }

      return element.ValueKind == JsonValueKind.Array
        && element.EnumerateArray().Any(HasReservedMetadata);
    }

    private static Matrix4x4 ReadNodeTransform(JsonElement node)
    {
      if (node.TryGetProperty("matrix", out var matrix))
      {
        var values = ReadFloatArray(matrix, 16, "matrix");
        // glTF's column-major array maps to these fields because System.Numerics
        // composes row vectors and stores translation in M41-M43 (array slots 12-14).
        return ValidateNodeTransform(new Matrix4x4(
          values[0], values[1], values[2], values[3],
          values[4], values[5], values[6], values[7],
          values[8], values[9], values[10], values[11],
          values[12], values[13], values[14], values[15]));
      }

      var translation = node.TryGetProperty("translation", out var translationElement)
        ? ReadVector3(translationElement, "translation")
        : Vector3.Zero;
      var scale = node.TryGetProperty("scale", out var scaleElement)
        ? ReadVector3(scaleElement, "scale")
        : Vector3.One;
      var rotation = Quaternion.Identity;
      if (node.TryGetProperty("rotation", out var rotationElement))
      {
        var values = ReadFloatArray(rotationElement, 4, "rotation");
        rotation = new Quaternion(values[0], values[1], values[2], values[3]);
        var lengthSquared = rotation.LengthSquared();
        if (!float.IsFinite(lengthSquared) || lengthSquared == 0)
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        rotation = Quaternion.Normalize(rotation);
      }

      // System.Numerics row-vector composition applies scale, then rotation, then translation.
      return ValidateNodeTransform(
        Matrix4x4.CreateScale(scale)
        * Matrix4x4.CreateFromQuaternion(rotation)
        * Matrix4x4.CreateTranslation(translation));
    }

    private static Matrix4x4 ValidateNodeTransform(Matrix4x4 transform)
    {
      if (!IsFinite(transform)
        || transform.M14 != 0
        || transform.M24 != 0
        || transform.M34 != 0
        || transform.M44 != 1)
      {
        throw new UnsupportedGltfDomainException("TransformOrHierarchy");
      }
      return transform;
    }

    private static Vector3 ReadVector3(JsonElement value, string propertyName)
    {
      var values = ReadFloatArray(value, 3, propertyName);
      return new Vector3(values[0], values[1], values[2]);
    }

    private static float[] ReadFloatArray(JsonElement value, int count, string propertyName)
    {
      if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != count)
      {
        throw new InvalidDataException($"Node {propertyName} must contain {count} numbers.");
      }
      var values = value.EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (values.Any(item => !float.IsFinite(item)))
      {
        throw new UnsupportedGltfDomainException("TransformOrHierarchy");
      }
      return values;
    }

    private static bool IsFinite(Matrix4x4 value)
    {
      return new[]
      {
        value.M11, value.M12, value.M13, value.M14,
        value.M21, value.M22, value.M23, value.M24,
        value.M31, value.M32, value.M33, value.M34,
        value.M41, value.M42, value.M43, value.M44
      }.All(float.IsFinite);
    }

    private static IReadOnlyList<ParsedGltfAnimation> ReadAnimations(
      JsonElement root,
      ReadOnlySpan<byte> binary)
    {
      if (!root.TryGetProperty("animations", out var animations))
      {
        return Array.Empty<ParsedGltfAnimation>();
      }
      var result = new List<ParsedGltfAnimation>();
      foreach (var animation in animations.EnumerateArray())
      {
        var samplers = animation.GetProperty("samplers");
        var builders = new Dictionary<int, ParsedAnimationBuilder>();
        foreach (var channel in animation.GetProperty("channels").EnumerateArray())
        {
          var target = channel.GetProperty("target");
          var nodeIndex = target.GetProperty("node").GetInt32();
          if (nodeIndex < 0 || nodeIndex >= root.GetProperty("nodes").GetArrayLength())
          {
            throw new UnsupportedGltfDomainException("animations");
          }
          var path = target.GetProperty("path").GetString();
          var samplerIndex = channel.GetProperty("sampler").GetInt32();
          if (samplerIndex < 0 || samplerIndex >= samplers.GetArrayLength())
          {
            throw new UnsupportedGltfDomainException("animations");
          }
          var sampler = samplers[samplerIndex];
          var interpolation = sampler.TryGetProperty("interpolation", out var interpolationValue)
            ? interpolationValue.GetString()
            : "LINEAR";
          if (interpolation is not ("LINEAR" or "STEP" or "CUBICSPLINE"))
          {
            throw new UnsupportedGltfDomainException("animations");
          }
          var times = ReadFloatAccessor(
            root,
            binary,
            sampler.GetProperty("input").GetInt32(),
            1,
            "SCALAR");
          if (times[0] < 0
            || times.Zip(times.Skip(1), (left, right) => left < right).Any(valid => !valid))
          {
            throw new UnsupportedGltfDomainException("animations");
          }
          if (!builders.TryGetValue(nodeIndex, out var builder))
          {
            builder = new ParsedAnimationBuilder(
              nodeIndex,
              ReadNodeTransform(root.GetProperty("nodes")[nodeIndex]));
            builders.Add(nodeIndex, builder);
          }
          builder.Add(
            path,
            times,
            interpolation!,
            ReadFloatAccessor(
              root,
              binary,
              sampler.GetProperty("output").GetInt32(),
              path == "rotation" ? 4 : 3,
              path == "rotation" ? "VEC4" : "VEC3"));
        }
        if (builders.Count == 0)
        {
          throw new UnsupportedGltfDomainException("animations");
        }
        result.Add(new ParsedGltfAnimation(
          animation.TryGetProperty("name", out var name) ? name.GetString() : null,
          Array.AsReadOnly(builders.OrderBy(item => item.Key)
            .Select(item => item.Value.Build()).ToArray())));
      }
      return result.AsReadOnly();
    }

    private sealed class ParsedAnimationBuilder
    {
      private readonly int _nodeIndex;
      private ParsedGltfAnimationChannel? _translationChannel;
      private ParsedGltfAnimationChannel? _rotationChannel;
      private ParsedGltfAnimationChannel? _scaleChannel;
      private readonly ProjectedAnimationFrame _rest;

      internal ParsedAnimationBuilder(int nodeIndex, Matrix4x4 localTransform)
      {
        _nodeIndex = nodeIndex;
        try
        {
          _rest = StaticAnimationProjection.Canonicalize(localTransform);
        }
        catch (InvalidDataException)
        {
          throw new UnsupportedGltfDomainException("animations");
        }
      }

      internal void Add(string? path, float[] times, string interpolation, float[] values)
      {
        var dimensions = path == "rotation" ? 4 : 3;
        var expectedLength = checked(times.Length * dimensions
          * (interpolation == "CUBICSPLINE" ? 3 : 1));
        if (values.Length != expectedLength)
        {
          throw new UnsupportedGltfDomainException("animations");
        }
        var channel = new ParsedGltfAnimationChannel(times, values, dimensions, interpolation);
        switch (path)
        {
          case "translation" when _translationChannel is null:
            _translationChannel = channel;
            break;
          case "rotation" when _rotationChannel is null:
            _rotationChannel = channel;
            break;
          case "scale" when _scaleChannel is null:
            _scaleChannel = channel;
            break;
          default:
            throw new UnsupportedGltfDomainException("animations");
        }
      }

      internal ParsedGltfAnimationObject Build()
      {
        return new ParsedGltfAnimationObject(
          _nodeIndex,
          _translationChannel ?? Constant(_rest.Translation),
          _rotationChannel ?? Constant(_rest.Rotation),
          _scaleChannel ?? Constant(_rest.Scale));
      }

      private static ParsedGltfAnimationChannel Constant(Vector3 value)
      {
        return new ParsedGltfAnimationChannel(
          new[] { 0f },
          new[] { value.X, value.Y, value.Z },
          3,
          "STEP");
      }

      private static ParsedGltfAnimationChannel Constant(Quaternion value)
      {
        return new ParsedGltfAnimationChannel(
          new[] { 0f },
          new[] { value.X, value.Y, value.Z, value.W },
          4,
          "STEP");
      }
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
      var vertexCount = positions.Length / 3;
      var textureCoordinates = attributes.TryGetProperty("TEXCOORD_0", out var textureAccessor)
        ? ReadFloatAccessor(root, binary, textureAccessor.GetInt32(), 2, "VEC2")
        : new float[vertexCount * 2];
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

      ushort[] indices;
      if (!primitive.TryGetProperty("indices", out var indexAccessorIndex))
      {
        if (vertexCount % 3 != 0)
        {
          throw new UnsupportedGltfDomainException("Indices");
        }

        indices = Enumerable.Range(0, vertexCount).Select(index => (ushort)index).ToArray();
      }
      else
      {
        var accessor = root.GetProperty("accessors")[indexAccessorIndex.GetInt32()];
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
        indices = new ushort[indexCount];
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
      }

      var triangles = new StaticTriangle[indices.Length / 3];
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
        Array.AsReadOnly(triangles),
        primitive.TryGetProperty("material", out var material) ? material.GetInt32() : null);
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
    internal int LocalId { get; private set; }

    internal IReadOnlyList<RenderVertex> Vertices { get; }

    internal IReadOnlyList<StaticTriangle> Triangles { get; }

    internal int? MaterialIndex { get; }

    internal GeometryPartition(
      int localId,
      IReadOnlyList<RenderVertex> vertices,
      IReadOnlyList<StaticTriangle> triangles,
      int? materialIndex = null)
    {
      LocalId = localId;
      Vertices = vertices;
      Triangles = triangles;
      MaterialIndex = materialIndex;
    }

    internal void AssignLocalId(int localId)
    {
      if (LocalId >= 0)
      {
        throw new InvalidOperationException("The geometry partition already has an identity.");
      }

      LocalId = localId;
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

    internal static string CreateSurfaceKey(
      IReadOnlyList<RenderVertex> vertices,
      IReadOnlyList<StaticTriangle> triangles)
    {
      return CreateSurfaceKey(new[] { new GeometryPartition(0, vertices, triangles) });
    }

    internal static string CreateSurfaceKey(IReadOnlyList<GeometryPartition> partitions)
    {
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        var triangleTokens = partitions
          .SelectMany(partition => partition.Triangles.Select(triangle =>
            CreateTriangleToken(partition.Vertices, triangle)))
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
