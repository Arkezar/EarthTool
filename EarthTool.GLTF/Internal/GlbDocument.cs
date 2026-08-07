#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
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
    internal IReadOnlyList<ParsedGltfMesh> Meshes { get; }

    internal IReadOnlyList<ParsedGltfNode> Nodes { get; }

    internal IReadOnlyList<ParsedGltfMaterial> Materials { get; }

    internal IReadOnlyList<ParsedGltfAnimation> Animations { get; }

    internal IReadOnlyList<ParsedGltfLight> Lights { get; }

    internal int RootNodeIndex { get; }

    internal IReadOnlyList<string> IgnoredInertPaths { get; }

    internal int[]? NewModelNodeOrder { get; set; }

    internal int[]? NewModelMaterialOrder { get; set; }

    internal int[]? NewModelLightOrder { get; set; }

    internal ParsedGlb(
      IReadOnlyList<ParsedGltfMesh> meshes,
      IReadOnlyList<ParsedGltfNode> nodes,
      IReadOnlyList<ParsedGltfMaterial> materials,
      IReadOnlyList<ParsedGltfAnimation> animations,
      IReadOnlyList<ParsedGltfLight> lights,
      int rootNodeIndex,
      IReadOnlyList<string> ignoredInertPaths)
    {
      Meshes = meshes;
      Nodes = nodes;
      Materials = materials;
      Animations = animations;
      Lights = lights;
      RootNodeIndex = rootNodeIndex;
      IgnoredInertPaths = ignoredInertPaths;
    }
  }

  internal sealed class ParsedGltfNode
  {
    internal string? Name { get; }

    internal bool IsPlacementRoot { get; }

    internal string? AuthoringMetadata { get; }

    internal int? MeshIndex { get; }

    internal int? LightIndex { get; }

    internal int? CameraIndex { get; }

    internal IReadOnlyList<int> Children { get; }

    internal Matrix4x4 LocalTransform { get; }

    internal ParsedGltfNode(
      string? name,
      bool isPlacementRoot,
      string? authoringMetadata,
      int? meshIndex,
      int? lightIndex,
      int? cameraIndex,
      IReadOnlyList<int> children,
      Matrix4x4 localTransform)
    {
      Name = name;
      IsPlacementRoot = isPlacementRoot;
      AuthoringMetadata = authoringMetadata;
      MeshIndex = meshIndex;
      LightIndex = lightIndex;
      CameraIndex = cameraIndex;
      Children = children;
      LocalTransform = localTransform;
    }
  }

  internal sealed class ParsedGltfLight
  {
    internal string? Name { get; }

    internal string Type { get; }

    internal Vector3 Color { get; }

    internal float Intensity { get; }

    internal float? Range { get; }

    internal float InnerConeAngle { get; }

    internal float OuterConeAngle { get; }

    internal ParsedGltfLight(
      string? name,
      string type,
      Vector3 color,
      float intensity,
      float? range,
      float innerConeAngle,
      float outerConeAngle)
    {
      Name = name;
      Type = type;
      Color = color;
      Intensity = intensity;
      Range = range;
      InnerConeAngle = innerConeAngle;
      OuterConeAngle = outerConeAngle;
    }
  }

  internal sealed class ParsedGltfMesh
  {
    internal IReadOnlyList<ParsedGltfPrimitive> Primitives { get; }

    internal ParsedGltfMesh(IReadOnlyList<ParsedGltfPrimitive> primitives)
    {
      Primitives = primitives;
    }
  }

  internal sealed class ParsedGltfPrimitive
  {
    internal IReadOnlyList<RenderVertex> Vertices { get; }

    internal IReadOnlyList<StaticTriangle> Triangles { get; }

    internal int? MaterialIndex { get; }

    internal bool HasTextureCoordinate { get; }

    internal ParsedGltfPrimitive(
      IReadOnlyList<RenderVertex> vertices,
      IReadOnlyList<StaticTriangle> triangles,
      int? materialIndex,
      bool hasTextureCoordinate)
    {
      Vertices = vertices;
      Triangles = triangles;
      MaterialIndex = materialIndex;
      HasTextureCoordinate = hasTextureCoordinate;
    }
  }

  internal sealed class ParsedGltfMaterial
  {
    internal bool HasBaseColorTexture { get; }

    internal string? TextureResourceKey { get; }

    internal ParsedGltfMaterial(bool hasBaseColorTexture, string? textureResourceKey = null)
    {
      HasBaseColorTexture = hasBaseColorTexture;
      TextureResourceKey = textureResourceKey;
    }
  }

  internal sealed class ParsedGltfAnimation
  {
    internal string? Name { get; }

    internal IReadOnlyList<ParsedGltfAnimationObject> Objects { get; }

    internal float EndTime => Objects.Count == 0 ? 0 : Objects.Max(item => item.EndTime);

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

  internal static class AttachmentHeadingProjection
  {
    private const float HalfHeadingStep = MathF.PI / 256;

    internal static Quaternion CreateRotation(byte heading)
    {
      var headingRadians = heading * (MathF.PI * 2 / 256f);
      var transform = Matrix4x4.CreateRotationZ(-MathF.PI / 2)
        * Matrix4x4.CreateRotationY(headingRadians);
      var rotation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(transform));
      return rotation.W < 0
        ? new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W)
        : rotation;
    }

    internal static Vector3 CreateDirection(byte heading)
    {
      var angle = heading * (MathF.PI * 2 / 256f);
      return new Vector3(MathF.Cos(angle), 0, -MathF.Sin(angle));
    }

    internal static bool TryReadHeading(Quaternion rotation, out byte heading)
    {
      heading = 0;
      if (!IsFinite(rotation) || rotation.LengthSquared() == 0)
      {
        return false;
      }

      rotation = Quaternion.Normalize(rotation);
      var direction = Vector3.TransformNormal(
        Vector3.UnitY,
        Matrix4x4.CreateFromQuaternion(rotation));
      if (!IsFinite(direction) || direction.LengthSquared() == 0)
      {
        return false;
      }
      direction = Vector3.Normalize(direction);
      if (MathF.Abs(direction.Y) > MathF.Sin(HalfHeadingStep) + 1e-5f)
      {
        return false;
      }

      var horizontal = new Vector2(direction.X, direction.Z);
      if (horizontal.LengthSquared() == 0)
      {
        return false;
      }
      horizontal = Vector2.Normalize(horizontal);
      var angle = MathF.Atan2(-horizontal.Y, horizontal.X);
      if (angle < 0)
      {
        angle += MathF.PI * 2;
      }
      heading = unchecked((byte)((int)MathF.Floor(
        (angle * 256 / (MathF.PI * 2)) + 0.5f) & 0xFF));
      var expectedDirection = CreateDirection(heading);
      var directionError = MathF.Acos(Math.Clamp(
        Vector3.Dot(direction, expectedDirection),
        -1,
        1));
      var rotationError = 2 * MathF.Acos(Math.Clamp(
        MathF.Abs(Quaternion.Dot(rotation, CreateRotation(heading))),
        -1,
        1));
      return directionError <= HalfHeadingStep + 1e-5f
        && rotationError <= HalfHeadingStep + 1e-5f;
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X)
        && float.IsFinite(value.Y)
        && float.IsFinite(value.Z);
    }

    private static bool IsFinite(Quaternion value)
    {
      return float.IsFinite(value.X)
        && float.IsFinite(value.Y)
        && float.IsFinite(value.Z)
        && float.IsFinite(value.W);
    }
  }

  internal static class GlbDocument
  {
    internal const string PlacementRootMarker = "earthtoolPlacementRoot";
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunkType = 0x4E4F534A;
    private const uint BinaryChunkType = 0x004E4942;

    internal static byte[] Create(
      StaticMeshAsset asset,
      IReadOnlyDictionary<StaticRenderObject, TexPreview> previews,
      string? sourceBaseName)
    {
      var package = CreatePackage(
        asset,
        false,
        previews,
        sourceBaseName);
      return Pack(package.Json, package.Binary);
    }

    internal static GltfPackage CreateSeparate(
      StaticMeshAsset asset,
      IReadOnlyDictionary<StaticRenderObject, TexPreview> previews,
      string? sourceBaseName)
    {
      return CreatePackage(
        asset,
        true,
        previews,
        sourceBaseName);
    }

    internal static int GetMaximumMetadataByteCount(StaticMeshAsset asset)
    {
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject).ToArray();
      var maximum = 0;
      for (var index = 0; index < sources.Length; index++)
      {
        var metadata = CreateStaticSourceAuthoringMetadata(
          asset,
          sources[index],
          index + 1,
          index == 0);
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(metadata));
      }
      foreach (var cannon in ProjectCannons(asset))
      {
        var metadata = CanonicalAuthoringMetadata.Write(
          CanonicalAuthoringOwner.Parse(GetCannonHelperName(cannon.PhysicalNumber)),
          new CannonAuthoringValues(cannon.AttachmentRecord[7]),
          GltfOperationProfile.Default);
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(metadata));
      }
      foreach (var light in ProjectStaticLights(asset))
      {
        maximum = Math.Max(
          maximum,
          Encoding.UTF8.GetByteCount(CreateStaticLightAuthoringMetadata(light)));
      }
      return maximum;
    }

    internal static OperationDiagnostic? ValidateAuthoringMetadataProfile(
      StaticMeshAsset asset,
      GltfOperationProfile profile)
    {
      var carriers = new List<AuthoringMetadataCarrier>();
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject).ToArray();
      for (var index = 0; index < sources.Length; index++)
      {
        var name = $"ET_Static_{index + 1}";
        carriers.Add(new AuthoringMetadataCarrier(
          $"nodes[{index}]",
          name,
          CreateStaticSourceAuthoringMetadata(asset, sources[index], index + 1, index == 0)));
      }
      foreach (var cannon in ProjectCannons(asset))
      {
        var name = GetCannonHelperName(cannon.PhysicalNumber);
        carriers.Add(new AuthoringMetadataCarrier(
          name,
          name,
          CanonicalAuthoringMetadata.Write(
            CanonicalAuthoringOwner.Parse(name),
            new CannonAuthoringValues(cannon.AttachmentRecord[7]),
            GltfOperationProfile.Default)));
      }
      foreach (var light in ProjectStaticLights(asset))
      {
        var name = GetStaticLightHelperName(light.Type, light.PhysicalNumber);
        carriers.Add(new AuthoringMetadataCarrier(
          name,
          name,
          CreateStaticLightAuthoringMetadata(light)));
      }

      var result = CanonicalAuthoringMetadata.Read(carriers, profile);
      return result.Succeeded
        ? null
        : result.Diagnostics.First(item => item.Severity == DiagnosticSeverity.Error);
    }

    internal static int GetMinimumOutputByteCount(
      StaticMeshAsset asset,
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

      var animations = StaticAnimationProjection.Create(asset);
      foreach (var clip in animations.Clips)
      {
        binaryLength = checked(binaryLength
          + (clip.FrameCount * sizeof(float))
          + (clip.Objects.Count * clip.FrameCount * 10L * sizeof(float)));
      }

      var containerBytes = glb ? 28 : 0;
      return checked((int)(
        binaryLength
        + GetMaximumMetadataByteCount(asset)
        + containerBytes));
    }

    private static GltfPackage CreatePackage(
      StaticMeshAsset asset,
      bool separate,
      IReadOnlyDictionary<StaticRenderObject, TexPreview> previews,
      string? sourceBaseName)
    {
      var partitions = asset.StaticRenderObjectSequence
        .Select(item => new ProjectedPartition(
          item,
          item.RenderVertices.Select(ProjectToGltf).ToArray()))
        .ToArray();
      var animations = StaticAnimationProjection.Create(asset);
      var binary = CreateBinary(
        partitions,
        animations,
        previews,
        !separate,
        out var layouts,
        out var previewLayouts,
        out var animationLayouts);
      var bufferFileName = separate ? Hash(binary) + ".bin" : null;
      var json = CreateJson(
        asset,
        layouts,
        binary.Length,
        previewLayouts,
        animationLayouts,
        bufferFileName,
        sourceBaseName);
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

    internal static ParsedGlb ParseCanonicalStatic(byte[] glb, GltfOperationProfile profile)
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

    internal static ParsedGlb ParseSeparateCanonicalStatic(
      byte[] json,
      byte[] binary,
      GltfOperationProfile profile
    )
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
      var rootNodeIndex = ResolveRootNodeIndex(root, intent, out var placementRootIndex);
      var nodes = new List<ParsedGltfNode>();
      foreach (var node in root.GetProperty("nodes").EnumerateArray())
      {
        var children = node.TryGetProperty("children", out var childArray)
          ? childArray.EnumerateArray().Select(child => child.GetInt32()).ToArray()
          : Array.Empty<int>();
        nodes.Add(new ParsedGltfNode(
          node.TryGetProperty("name", out var name) ? name.GetString() : null,
          TryGetPlacementRootMarker(node, out var isPlacementRoot) && isPlacementRoot,
          TryGetAuthoringMetadata(node),
          node.TryGetProperty("mesh", out var mesh) ? mesh.GetInt32() : null,
          TryGetLightIndex(node),
          node.TryGetProperty("camera", out var camera) ? camera.GetInt32() : null,
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

        meshes.Add(new ParsedGltfMesh(primitives.AsReadOnly()));
      }

      var materials = root.TryGetProperty("materials", out var materialArray)
        ? materialArray.EnumerateArray()
          .Select(material => new ParsedGltfMaterial(
            material.TryGetProperty("pbrMetallicRoughness", out var pbr)
              && pbr.TryGetProperty("baseColorTexture", out _),
            ReadMaterialTextureResourceKey(material)))
          .ToArray()
        : Array.Empty<ParsedGltfMaterial>();
      var animations = ReadAnimations(root, binary, placementRootIndex);
      var lights = ReadLights(root);

      return new ParsedGlb(
        meshes.AsReadOnly(),
        nodes.AsReadOnly(),
        Array.AsReadOnly(materials),
        animations,
        lights,
        rootNodeIndex,
        GetIgnoredInertPaths(root, intent, placementRootIndex));
    }

    private static int ResolveRootNodeIndex(
      JsonElement root,
      GltfImportIntent intent,
      out int? placementRootIndex)
    {
      var nodes = root.GetProperty("nodes");
      var sceneRootIndex = root.GetProperty("scenes")[0].GetProperty("nodes")[0].GetInt32();
      placementRootIndex = null;
      for (var index = 0; index < nodes.GetArrayLength(); index++)
      {
        if (!TryGetPlacementRootMarker(nodes[index], out var hasMarker))
        {
          continue;
        }
        if (!hasMarker || placementRootIndex.HasValue || index != sceneRootIndex)
        {
          throw new UnsupportedGltfDomainException("PlacementRoot");
        }
        placementRootIndex = index;
      }

      var sceneRoot = nodes[sceneRootIndex];
      if (placementRootIndex.HasValue)
      {
        if (sceneRoot.TryGetProperty("mesh", out _)
          || sceneRoot.TryGetProperty("camera", out _)
          || sceneRoot.TryGetProperty("skin", out _)
          || sceneRoot.TryGetProperty("weights", out _)
          || sceneRoot.TryGetProperty("extensions", out _)
          || TryGetLightIndex(sceneRoot).HasValue
          || !sceneRoot.TryGetProperty("children", out var children)
          || children.GetArrayLength() != 1)
        {
          throw new UnsupportedGltfDomainException("PlacementRoot");
        }
        return intent == GltfImportIntent.Edit
          ? children[0].GetInt32()
          : sceneRootIndex;
      }

      if (intent == GltfImportIntent.Edit
        && !sceneRoot.TryGetProperty("mesh", out _))
      {
        throw new UnsupportedGltfDomainException("PlacementRoot");
      }
      return sceneRootIndex;
    }

    private static bool TryGetPlacementRootMarker(JsonElement node, out bool value)
    {
      value = false;
      if (!node.TryGetProperty("extras", out var extras)
        || !extras.TryGetProperty(PlacementRootMarker, out var marker))
      {
        return false;
      }
      if (marker.ValueKind != JsonValueKind.True)
      {
        return true;
      }
      value = true;
      return true;
    }

    private static int? TryGetLightIndex(JsonElement node)
    {
      return node.TryGetProperty("extensions", out var extensions)
        && extensions.TryGetProperty("KHR_lights_punctual", out var light)
        && light.TryGetProperty("light", out var index)
        ? index.GetInt32()
        : null;
    }

    private static IReadOnlyList<ParsedGltfLight> ReadLights(JsonElement root)
    {
      if (!root.TryGetProperty("extensions", out var extensions)
        || !extensions.TryGetProperty("KHR_lights_punctual", out var punctual)
        || !punctual.TryGetProperty("lights", out var lights))
      {
        return Array.Empty<ParsedGltfLight>();
      }

      var result = new List<ParsedGltfLight>();
      foreach (var light in lights.EnumerateArray())
      {
        var color = light.TryGetProperty("color", out var colorArray)
          ? ReadFloatArray(colorArray, 3, "light.color")
          : new[] { 1f, 1f, 1f };
        var type = light.GetProperty("type").GetString()
          ?? throw new InvalidDataException("A punctual-light type cannot be null.");
        var inner = 0f;
        var outer = MathF.PI / 4;
        if (light.TryGetProperty("spot", out var spot))
        {
          inner = spot.TryGetProperty("innerConeAngle", out var innerElement)
            ? innerElement.GetSingle()
            : 0;
          outer = spot.TryGetProperty("outerConeAngle", out var outerElement)
            ? outerElement.GetSingle()
            : MathF.PI / 4;
        }
        result.Add(new ParsedGltfLight(
          light.TryGetProperty("name", out var name) ? name.GetString() : null,
          type,
          new Vector3(color[0], color[1], color[2]),
          light.TryGetProperty("intensity", out var intensity) ? intensity.GetSingle() : 1,
          light.TryGetProperty("range", out var range) ? range.GetSingle() : null,
          inner,
          outer));
      }
      return result.AsReadOnly();
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

    private static byte[] CreateBinary(
      IReadOnlyList<ProjectedPartition> partitions,
      AnimationProjectionSet animations,
      IReadOnlyDictionary<StaticRenderObject, TexPreview> previews,
      bool embedPreviews,
      out IReadOnlyDictionary<StaticRenderObject, PartitionLayout> layouts,
      out IReadOnlyDictionary<StaticRenderObject, PreviewLayout> previewLayouts,
      out IReadOnlyList<AnimationLayout> animationLayouts)
    {
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
      var createdLayouts = new Dictionary<StaticRenderObject, PartitionLayout>();
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
          partition.RenderObject,
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

      var createdPreviewLayouts = new Dictionary<StaticRenderObject, PreviewLayout>();
      var sharedPreviewLayouts = new Dictionary<string, PreviewLayout>(StringComparer.Ordinal);
      foreach (var partition in partitions.Where(partition => previews.ContainsKey(
        partition.RenderObject)))
      {
        var preview = previews[partition.RenderObject];
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
        createdPreviewLayouts.Add(partition.RenderObject, previewLayout);
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
      IReadOnlyDictionary<StaticRenderObject, PartitionLayout> layouts,
      int binaryLength,
      IReadOnlyDictionary<StaticRenderObject, PreviewLayout> previewLayouts,
      IReadOnlyList<AnimationLayout> animationLayouts,
      string? bufferFileName,
      string? sourceBaseName)
    {
      var rootSourceObject = asset.RootSourceObject;
      var sources = StaticSourceObjectTraversal.Flatten(rootSourceObject).ToArray();
      var attachments = ProjectAttachments(asset);
      var cannons = ProjectCannons(asset);
      var staticLights = ProjectStaticLights(asset);
      var parentedEmitters = Enumerable.Range(1, 4)
        .Select(number => new
        {
          PhysicalNumber = number + 4,
          Sources = GetMarkerAttachmentSourceObjects(asset, number)
        })
        .Where(item => item.Sources.Count == 1
          && attachments.Any(attachment => attachment.PhysicalNumber == item.PhysicalNumber))
        .ToDictionary(item => item.PhysicalNumber, item => item.Sources[0]);
      var sourceObjectTransforms = CreateSourceObjectTransforms(rootSourceObject, layouts);
      var placementRootIndex = sources.Length
        + attachments.Count
        + cannons.Count
        + staticLights.Count;
      var attachmentNodeIndices = attachments
        .Select((attachment, index) => new
        {
          attachment.PhysicalNumber,
          Index = sources.Length + index
        })
        .ToDictionary(item => item.PhysicalNumber, item => item.Index);
      var parentedEmitterNodeIndices = parentedEmitters.Keys
        .Select(physicalNumber => attachmentNodeIndices[physicalNumber])
        .ToHashSet();
      var nodeIndices = sources
        .Select((source, index) => (source, index))
        .ToDictionary(item => item.source, item => item.index);
      var nodeIndicesByOrdinal = sources
        .Select((source, index) => new
        {
          Ordinal = index + 1,
          Index = index,
        })
        .ToDictionary(item => item.Ordinal, item => item.Index);
      var orderedLayouts = asset.StaticRenderObjectSequence
        .Select(renderObject => layouts[renderObject])
        .ToArray();
      var accessorIndices = orderedLayouts
        .Select((layout, index) => (layout.Partition.RenderObject, Index: index * 4))
        .ToDictionary(item => item.RenderObject, item => item.Index);
      var materialIndices = orderedLayouts
        .Select((layout, index) => (layout.Partition.RenderObject, Index: index))
        .ToDictionary(item => item.RenderObject, item => item.Index);
      var orderedPreviewLayouts = orderedLayouts
        .Where(layout => previewLayouts.ContainsKey(layout.Partition.RenderObject))
        .Select(layout =>
          (
            RenderObject: layout.Partition.RenderObject,
            Layout: previewLayouts[layout.Partition.RenderObject]
          )
        )
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
          preview.RenderObject,
          Index = imageIndices[preview.Layout.ContentAddress]
        })
        .ToDictionary(item => item.RenderObject, item => item.Index);
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
        if (staticLights.Count > 0)
        {
          writer.WriteStringValue("KHR_lights_punctual");
        }
        writer.WriteEndArray();
        if (staticLights.Count > 0)
        {
          writer.WriteStartObject("extensions");
          writer.WriteStartObject("KHR_lights_punctual");
          writer.WriteStartArray("lights");
          foreach (var light in staticLights)
          {
            writer.WriteStartObject();
            writer.WriteString("name", GetStaticLightHelperName(light.Type, light.PhysicalNumber));
            writer.WriteString("type", light.Type);
            writer.WriteStartArray("color");
            writer.WriteNumberValue(light.Color.X);
            writer.WriteNumberValue(light.Color.Y);
            writer.WriteNumberValue(light.Color.Z);
            writer.WriteEndArray();
            writer.WriteNumber("intensity", light.Intensity);
            if (light.Type == "spot")
            {
              writer.WriteStartObject("spot");
              writer.WriteNumber("innerConeAngle", light.InnerConeAngle);
              writer.WriteNumber("outerConeAngle", light.OuterConeAngle);
              writer.WriteEndObject();
            }
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
          writer.WriteEndObject();
          writer.WriteEndObject();
        }
        writer.WriteNumber("scene", 0);
        writer.WriteStartArray("scenes");
        writer.WriteStartObject();
        writer.WriteStartArray("nodes");
        writer.WriteNumberValue(placementRootIndex);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("nodes");
        for (var sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
          var source = sources[sourceIndex];
          var sourceOrdinal = sourceIndex + 1;
          writer.WriteStartObject();
          writer.WriteString("name", $"ET_Static_{sourceOrdinal}");
          writer.WriteNumber("mesh", nodeIndices[source]);
          var effectivePivot = layouts[source.StaticRenderObjects[0]].Partition.RenderObject.Pivot;
          var translation = ProjectToGltf(effectivePivot);
          if (translation != Vector3.Zero)
          {
            writer.WriteStartArray("translation");
            writer.WriteNumberValue(translation.X);
            writer.WriteNumberValue(translation.Y);
            writer.WriteNumberValue(translation.Z);
            writer.WriteEndArray();
          }
          var isRoot = ReferenceEquals(source, rootSourceObject);
          var emitterChildren = parentedEmitters
            .Where(item => ReferenceEquals(item.Value, source))
            .Select(item => attachmentNodeIndices[item.Key])
            .ToArray();
          if (source.Children.Count > 0 || emitterChildren.Length > 0 || isRoot && (attachments.Count > 0
            || cannons.Count > 0
            || staticLights.Count > 0))
          {
            writer.WriteStartArray("children");
            foreach (var child in source.Children)
            {
              writer.WriteNumberValue(nodeIndices[child]);
            }
            foreach (var emitterChild in emitterChildren)
            {
              writer.WriteNumberValue(emitterChild);
            }
            if (isRoot)
            {
              var helperIndex = sources.Length;
              for (var index = 0;
                index < attachments.Count + cannons.Count + staticLights.Count;
                index++)
              {
                var nodeIndex = helperIndex + index;
                if (!parentedEmitterNodeIndices.Contains(nodeIndex))
                {
                  writer.WriteNumberValue(nodeIndex);
                }
              }
            }

            writer.WriteEndArray();
          }

          WriteAuthoringExtras(
            writer,
            CreateStaticSourceAuthoringMetadata(asset, source, sourceOrdinal, isRoot));
          writer.WriteEndObject();
        }
        foreach (var attachment in attachments)
        {
          writer.WriteStartObject();
          writer.WriteString("name", GetAttachmentHelperName(attachment.PhysicalNumber));
          if (parentedEmitters.TryGetValue(attachment.PhysicalNumber, out var sourceObject))
          {
            var relative = CreateRelativeTransform(attachment, sourceObjectTransforms[sourceObject]);
            WriteTransform(writer, relative.Translation, relative.Rotation);
          }
          else
          {
            WriteTransform(writer, attachment.Translation, attachment.Rotation);
          }
          writer.WriteEndObject();
        }
        foreach (var cannon in cannons)
        {
          writer.WriteStartObject();
          writer.WriteString("name", GetCannonHelperName(cannon.PhysicalNumber));
          WriteTransform(writer, cannon.Translation, cannon.Rotation);
          WriteAuthoringExtras(
            writer,
            CanonicalAuthoringMetadata.Write(
              CanonicalAuthoringOwner.Parse(GetCannonHelperName(cannon.PhysicalNumber)),
              new CannonAuthoringValues(cannon.AttachmentRecord[7]),
              GltfOperationProfile.Default));
          writer.WriteEndObject();
        }
        for (var lightIndex = 0; lightIndex < staticLights.Count; lightIndex++)
        {
          var light = staticLights[lightIndex];
          writer.WriteStartObject();
          writer.WriteString("name", GetStaticLightHelperName(light.Type, light.PhysicalNumber));
          WriteTransform(writer, light.Translation, light.Rotation);
          writer.WriteStartObject("extensions");
          writer.WriteStartObject("KHR_lights_punctual");
          writer.WriteNumber("light", lightIndex);
          writer.WriteEndObject();
          writer.WriteEndObject();
          WriteAuthoringExtras(
            writer,
            CreateStaticLightAuthoringMetadata(light));
          writer.WriteEndObject();
        }

        writer.WriteStartObject();
        writer.WriteString("name", sourceBaseName ?? "EarthTool Placement");
        writer.WriteStartArray("children");
        writer.WriteNumberValue(0);
        writer.WriteEndArray();
        writer.WriteStartObject("extras");
        writer.WriteBoolean(PlacementRootMarker, true);
        writer.WriteEndObject();
        writer.WriteEndObject();

        writer.WriteEndArray();
        writer.WriteStartArray("meshes");
        for (var sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
          var source = sources[sourceIndex];
          var sourceOrdinal = sourceIndex + 1;
          writer.WriteStartObject();
          writer.WriteString("name", sourceBaseName is null
            ? $"Static mesh {sourceOrdinal}"
            : $"{sourceBaseName}_{sourceOrdinal}_Mesh");
          writer.WriteStartArray("primitives");
          foreach (var renderObject in source.StaticRenderObjects)
          {
            var firstAccessor = accessorIndices[renderObject];
            writer.WriteStartObject();
            writer.WriteStartObject("attributes");
            writer.WriteNumber("POSITION", firstAccessor);
            writer.WriteNumber("NORMAL", firstAccessor + 1);
            writer.WriteNumber("TEXCOORD_0", firstAccessor + 2);
            writer.WriteEndObject();
            writer.WriteNumber("indices", firstAccessor + 3);
            writer.WriteNumber("mode", 4);
            writer.WriteNumber("material", materialIndices[renderObject]);
            writer.WriteEndObject();
          }

          writer.WriteEndArray();
          writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("materials");
        for (var layoutIndex = 0; layoutIndex < orderedLayouts.Length; layoutIndex++)
        {
          var layout = orderedLayouts[layoutIndex];
          var renderObject = layout.Partition.RenderObject;
          var renderObjectOrdinal = layoutIndex + 1;
          writer.WriteStartObject();
          writer.WriteString("name", $"TEX preview {renderObjectOrdinal}");
          writer.WriteStartObject("pbrMetallicRoughness");
          writer.WriteStartArray("baseColorFactor");
          writer.WriteNumberValue(1);
          writer.WriteNumberValue(1);
          writer.WriteNumberValue(1);
          writer.WriteNumberValue(1);
          writer.WriteEndArray();
          writer.WriteNumber("metallicFactor", 0);
          writer.WriteNumber("roughnessFactor", 1);
          if (previewIndices.TryGetValue(renderObject, out var previewIndex))
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
          if (TryCreateMaterialAuthoringMetadata(renderObject, out var materialMetadata))
          {
            writer.WriteStartObject("extras");
            writer.WriteString("earthtoolAuthoring", materialMetadata);
            writer.WriteEndObject();
          }
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
                writer.WriteNumber("node", nodeIndicesByOrdinal[item.Projection.SourceObjectOrdinal]);
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

    private static string CreateStaticSourceAuthoringMetadata(
      StaticMeshAsset asset,
      StaticSourceObject source,
      int sourceLocalId,
      bool isRoot)
    {
      var first = source.StaticRenderObjects[0];
      var roles = GltfStaticObjectRoles.None;
      if ((first.KnownFlags & StaticRenderObjectFlags.ViewerFaced) != 0)
      {
        roles |= GltfStaticObjectRoles.ViewerFaced;
      }
      if ((first.KnownFlags & StaticRenderObjectFlags.Barrel) != 0)
      {
        roles |= GltfStaticObjectRoles.Barrel;
      }
      if ((first.KnownFlags & StaticRenderObjectFlags.Rotor) != 0)
      {
        roles |= GltfStaticObjectRoles.Rotor;
      }

      CanonicalStaticFootprint? footprint = null;
      CanonicalHorizontalExtents? horizontalExtents = null;
      if (isRoot)
      {
        var header = asset.CommonBaseHeader;
        var elevationBytes = header.BoxTopElevations.ToArray();
        var elevations = Enumerable.Range(0, 16)
          .Select(index => BinaryPrimitives.ReadUInt16LittleEndian(
            elevationBytes.AsSpan((15 - index) * sizeof(ushort), sizeof(ushort))) / 256f)
          .ToArray();
        var cornerFlags = Enumerable.Range(0, 16)
          .Select(index => (byte)(header.BoxCornerPassageFlags[15 - index] & 0x0F))
          .ToArray();
        footprint = new CanonicalStaticFootprint(
          (ushort)header.BoxPresenceMask,
          elevations,
          cornerFlags);
        var extentBytes = header.HorizontalExtents.ToArray();
        horizontalExtents = new CanonicalHorizontalExtents(
          ReadExtent(extentBytes, 0),
          ReadExtent(extentBytes, 2),
          ReadExtent(extentBytes, 4),
          ReadExtent(extentBytes, 6));
      }

      return CanonicalAuthoringMetadata.Write(
        CanonicalAuthoringOwner.Parse($"ET_Static_{sourceLocalId}"),
        new StaticSourceAuthoringValues(
          footprint,
          horizontalExtents,
          roles,
          (roles & GltfStaticObjectRoles.Barrel) != 0 ? first.BarrelMaximumAngle : (byte)0),
        GltfOperationProfile.Default);
    }

    private static float ReadExtent(byte[] bytes, int offset)
    {
      return BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, sizeof(ushort))) / 256f;
    }

    private static bool TryCreateMaterialAuthoringMetadata(
      StaticRenderObject renderObject,
      out string authoringMetadata)
    {
      var bytes = renderObject.TexturePathBytes;
      if (bytes.Count == 0 || bytes.Any(value => value is 0 or > 0x7F))
      {
        authoringMetadata = string.Empty;
        return false;
      }
      var resourceKey = System.Text.Encoding.ASCII.GetString(bytes.ToArray());
      if (!EarthTool.MSH.Authoring.AuthoringValidation.IsCanonicalTextureResourceKey(resourceKey))
      {
        authoringMetadata = string.Empty;
        return false;
      }
      authoringMetadata = CanonicalAuthoringMetadata.WriteMaterial(resourceKey, GltfOperationProfile.Default);
      return true;
    }

    private static string CreateStaticLightAuthoringMetadata(ProjectedStaticLight light)
    {
      float? targetDistance = null;
      if (light.Type == "spot")
      {
        var value = ReadSingle(light.Record, 0x18);
        if (float.IsFinite(value) && value > 0)
        {
          targetDistance = value;
        }
      }
      return CanonicalAuthoringMetadata.Write(
        CanonicalAuthoringOwner.Parse(GetStaticLightHelperName(light.Type, light.PhysicalNumber)),
        new StaticLightAuthoringValues(light.Intensity, targetDistance),
        GltfOperationProfile.Default);
    }

    private static void WriteAuthoringExtras(
      Utf8JsonWriter writer,
      string authoringMetadata)
    {
      writer.WriteStartObject("extras");
      writer.WriteString("earthtoolAuthoring", authoringMetadata);
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
        || root.GetProperty("scenes").GetArrayLength() != 1)
      {
        throw new UnsupportedGltfDomainException("SceneGraph");
      }
      if (nodes.GetArrayLength() == 0
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
          || intent == GltfImportIntent.Edit && node.TryGetProperty("camera", out _))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        if (intent == GltfImportIntent.NewModel
          && node.TryGetProperty("camera", out var camera)
          && (!root.TryGetProperty("cameras", out var cameras)
            || camera.GetInt32() < 0
            || camera.GetInt32() >= cameras.GetArrayLength()))
        {
          throw new UnsupportedGltfDomainException("cameras");
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
          if (attributes.EnumerateObject().Any(attribute =>
              !supportedAttributes.Contains(attribute.Name)
              && (intent != GltfImportIntent.NewModel
                || !IsIgnoredInertAttribute(attribute.Name)))
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
        if (root.TryGetProperty(domain, out _)
          && (domain == "skins" || intent == GltfImportIntent.Edit))
        {
          throw new UnsupportedGltfDomainException(domain);
        }
      }
    }

    private static bool IsIgnoredInertAttribute(string name)
    {
      return name == "TANGENT"
        || name.StartsWith("TEXCOORD_", StringComparison.Ordinal)
          && name != "TEXCOORD_0";
    }

    private static IReadOnlyList<string> GetIgnoredInertPaths(
      JsonElement root,
      GltfImportIntent intent,
      int? placementRootIndex)
    {
      var paths = new List<string>();
      if (placementRootIndex.HasValue)
      {
        var placement = root.GetProperty("nodes")[placementRootIndex.Value];
        if (intent == GltfImportIntent.Edit
          && ReadNodeTransform(placement) != Matrix4x4.Identity)
        {
          paths.Add($"nodes[{placementRootIndex.Value}]");
        }
        if (root.TryGetProperty("animations", out var animations))
        {
          for (var animationIndex = 0; animationIndex < animations.GetArrayLength(); animationIndex++)
          {
            var channels = animations[animationIndex].GetProperty("channels");
            for (var channelIndex = 0; channelIndex < channels.GetArrayLength(); channelIndex++)
            {
              if (channels[channelIndex].GetProperty("target").GetProperty("node").GetInt32()
                == placementRootIndex.Value)
              {
                paths.Add($"animations[{animationIndex}].channels[{channelIndex}]");
              }
            }
          }
        }
      }

      if (intent != GltfImportIntent.NewModel)
      {
        return paths.AsReadOnly();
      }
      var nodes = root.GetProperty("nodes");
      for (var nodeIndex = 0; nodeIndex < nodes.GetArrayLength(); nodeIndex++)
      {
        if (nodes[nodeIndex].TryGetProperty("camera", out _))
        {
          paths.Add($"nodes[{nodeIndex}].camera");
        }
      }
      if (root.TryGetProperty("samplers", out _))
      {
        paths.Add("samplers");
      }
      var meshes = root.GetProperty("meshes");
      for (var meshIndex = 0; meshIndex < meshes.GetArrayLength(); meshIndex++)
      {
        var primitives = meshes[meshIndex].GetProperty("primitives");
        for (var primitiveIndex = 0; primitiveIndex < primitives.GetArrayLength(); primitiveIndex++)
        {
          foreach (var attribute in primitives[primitiveIndex].GetProperty("attributes").EnumerateObject())
          {
            if (IsIgnoredInertAttribute(attribute.Name))
            {
              paths.Add($"meshes[{meshIndex}].primitives[{primitiveIndex}].attributes.{attribute.Name}");
            }
          }
        }
      }
      return paths.AsReadOnly();
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
        || textures.GetArrayLength() == 0
        || images.GetArrayLength() == 0)
      {
        throw new UnsupportedGltfDomainException("TexturePreviews");
      }
      for (var index = 0; index < textures.GetArrayLength(); index++)
      {
        var texture = textures[index];
        if (texture.ValueKind != JsonValueKind.Object
          || texture.EnumerateObject().Any(property =>
            property.Name is not ("source" or "name" or "sampler"))
          || !texture.TryGetProperty("source", out var source)
          || source.GetInt32() < 0
          || source.GetInt32() >= images.GetArrayLength()
          || texture.TryGetProperty("sampler", out var sampler)
            && (sampler.ValueKind != JsonValueKind.Number
              || sampler.GetInt32() < 0
              || root.TryGetProperty("samplers", out var textureSamplers)
                && (textureSamplers.ValueKind != JsonValueKind.Array
                  || sampler.GetInt32() >= textureSamplers.GetArrayLength())))
        {
          throw new UnsupportedGltfDomainException("TexturePreviews");
        }
      }
      for (var index = 0; index < images.GetArrayLength(); index++)
      {
        var image = images[index];
        var hasBufferView = image.TryGetProperty("bufferView", out var bufferView);
        var hasUri = image.TryGetProperty("uri", out var uri);
        var mimeType = image.TryGetProperty("mimeType", out var mime)
          ? mime.GetString()
          : null;
        if (image.ValueKind != JsonValueKind.Object
          || image.EnumerateObject().Any(property =>
            property.Name is not ("name" or "bufferView" or "uri" or "mimeType"))
          || hasBufferView == hasUri
          || hasBufferView
            && (bufferView.GetInt32() < 0
              || bufferView.GetInt32() >= root.GetProperty("bufferViews").GetArrayLength())
          || hasUri
            && (uri.ValueKind != JsonValueKind.String
              || string.IsNullOrEmpty(uri.GetString()))
          || mimeType is not null and not ("image/png" or "image/jpeg")
          || hasBufferView && mimeType is null)
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

    internal static StaticRenderObjectFlags GetMarkerAttachmentFlag(int number)
    {
      return number switch
      {
        1 => StaticRenderObjectFlags.MarkerAttachment1,
        2 => StaticRenderObjectFlags.MarkerAttachment2,
        3 => StaticRenderObjectFlags.MarkerAttachment3,
        4 => StaticRenderObjectFlags.MarkerAttachment4,
        _ => throw new ArgumentOutOfRangeException(nameof(number))
      };
    }

    internal static IReadOnlyList<StaticSourceObject> GetMarkerAttachmentSourceObjects(
      StaticMeshAsset asset,
      int number,
      StaticSourceObject? root = null)
    {
      var flag = GetMarkerAttachmentFlag(number);
      return StaticSourceObjectTraversal.Flatten(root ?? asset.RootSourceObject)
        .Where(source => source.StaticRenderObjects.Any(renderObject =>
          (renderObject.KnownFlags & flag) != 0))
        .ToArray();
    }

    internal static (bool EmitterActive, int MarkerRecordCount)
      GetEmitterHierarchyState(StaticMeshAsset asset, int number)
    {
      if (number is < 1 or > 4)
      {
        throw new ArgumentOutOfRangeException(nameof(number));
      }
      var attachments = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var emitterActive = BinaryPrimitives.ReadInt16LittleEndian(
        attachments.AsSpan((number + 3) * 8, 8)) != short.MinValue;
      var flag = GetMarkerAttachmentFlag(number);
      var markerRecordCount = asset.StaticRenderObjectSequence.Count(renderObject =>
        (renderObject.KnownFlags & flag) != 0);
      return (emitterActive, markerRecordCount);
    }

    private static (Vector3 Translation, Quaternion Rotation) CreateRelativeTransform(
      ProjectedAttachment emitter,
      Matrix4x4 parentTransform)
    {
      if (!Matrix4x4.Invert(parentTransform, out var inverseParent))
      {
        throw new InvalidOperationException("An attachment parent transform must be invertible.");
      }

      var targetTranslation = emitter.Translation;
      for (var attempt = 0; attempt < 3; attempt++)
      {
        // Fixed-point values sit on a truncation boundary; stay inside the same bin after parent transforms.
        targetTranslation = new Vector3(
          MoveInsideTruncationBin(targetTranslation.X),
          MoveInsideTruncationBin(targetTranslation.Y),
          MoveInsideTruncationBin(targetTranslation.Z));
        var emitterTransform = Matrix4x4.CreateFromQuaternion(emitter.Rotation)
          * Matrix4x4.CreateTranslation(targetTranslation);
        if (Matrix4x4.Decompose(
            emitterTransform * inverseParent,
            out _,
            out var rotation,
            out var translation))
        {
          var normalizedRotation = Quaternion.Normalize(rotation);
          var effective = Matrix4x4.CreateFromQuaternion(normalizedRotation)
            * Matrix4x4.CreateTranslation(translation)
            * parentTransform;
          if (QuantizesToAttachment(effective.Translation, emitter.Record))
          {
            return (translation, normalizedRotation);
          }
        }
      }
      throw new InvalidOperationException("An attachment child transform could not preserve its record.");
    }

    private static IReadOnlyDictionary<StaticSourceObject, Matrix4x4> CreateSourceObjectTransforms(
      StaticSourceObject root,
      IReadOnlyDictionary<StaticRenderObject, PartitionLayout> layouts)
    {
      var result = new Dictionary<StaticSourceObject, Matrix4x4>();
      AddSourceObjectTransforms(root, Matrix4x4.Identity, layouts, result);
      return result;
    }

    private static void AddSourceObjectTransforms(
      StaticSourceObject source,
      Matrix4x4 parentTransform,
      IReadOnlyDictionary<StaticRenderObject, PartitionLayout> layouts,
      IDictionary<StaticSourceObject, Matrix4x4> result)
    {
      var pivot = layouts[source.StaticRenderObjects[0]].Partition.RenderObject.Pivot;
      var effective = Matrix4x4.CreateTranslation(ProjectToGltf(pivot)) * parentTransform;
      result.Add(source, effective);
      foreach (var child in source.Children)
      {
        AddSourceObjectTransforms(child, effective, layouts, result);
      }
    }

    private static float MoveInsideTruncationBin(float value)
    {
      const float bias = 1f / 1024;
      return value switch
      {
        > 0 => value + bias,
        < 0 => value - bias,
        _ => 0
      };
    }

    private static bool QuantizesToAttachment(Vector3 translation, byte[] record)
    {
      return Math.Truncate(translation.X * 256d) == BinaryPrimitives.ReadInt16LittleEndian(record)
        && Math.Truncate(translation.Z * 256d) == BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(2))
        && Math.Truncate(translation.Y * 256d) == BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(4));
    }

    private static IReadOnlyList<ProjectedAttachment> ProjectAttachments(StaticMeshAsset asset)
    {
      var table = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var result = new List<ProjectedAttachment>();
      for (var physicalNumber = 1; physicalNumber <= 49; physicalNumber++)
      {
        if (physicalNumber <= 4 || physicalNumber is >= 13 and <= 20)
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
        var rotation = AttachmentHeadingProjection.CreateRotation(record[6]);
        result.Add(new ProjectedAttachment(
          physicalNumber,
          record,
          new Vector3(x / 256f, z / 256f, storedNegativeY / 256f),
          rotation));
      }
      return result.AsReadOnly();
    }

    private static IReadOnlyList<ProjectedCannon> ProjectCannons(StaticMeshAsset asset)
    {
      var attachments = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var renderPositions = asset.CommonBaseHeader.CannonRenderPositions.ToArray();
      var result = new List<ProjectedCannon>();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        var attachmentRecord = attachments.AsSpan((physicalNumber - 1) * 8, 8).ToArray();
        if (BinaryPrimitives.ReadInt16LittleEndian(attachmentRecord) == short.MinValue)
        {
          continue;
        }
        var renderPositionRecord = renderPositions.AsSpan((physicalNumber - 1) * 12, 12).ToArray();
        var x = ReadFinitePreview(renderPositionRecord, 0);
        var storedNegativeY = ReadFinitePreview(renderPositionRecord, 4);
        var z = ReadFinitePreview(renderPositionRecord, 8);
        var rotation = AttachmentHeadingProjection.CreateRotation(attachmentRecord[6]);
        result.Add(new ProjectedCannon(
          physicalNumber,
          attachmentRecord,
          renderPositionRecord,
          new Vector3(x, z, storedNegativeY),
          rotation));
      }
      return result.AsReadOnly();
    }

    private static IReadOnlyList<ProjectedStaticLight> ProjectStaticLights(StaticMeshAsset asset)
    {
      var attachments = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var spots = asset.CommonBaseHeader.StaticSpotLights.ToArray();
      var omnis = asset.CommonBaseHeader.StaticOmniLights.ToArray();
      var result = new List<ProjectedStaticLight>();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        var spotAttachment = attachments.AsSpan((physicalNumber + 11) * 8, 8).ToArray();
        if (BinaryPrimitives.ReadInt16LittleEndian(spotAttachment) != short.MinValue)
        {
          result.Add(ProjectStaticLight(
            "spot",
            physicalNumber,
            spots.AsSpan((physicalNumber - 1) * 0x30, 0x30).ToArray(),
            spotAttachment));
        }

        var omniAttachment = attachments.AsSpan((physicalNumber + 15) * 8, 8).ToArray();
        if (BinaryPrimitives.ReadInt16LittleEndian(omniAttachment) != short.MinValue)
        {
          result.Add(ProjectStaticLight(
            "point",
            physicalNumber,
            omnis.AsSpan((physicalNumber - 1) * 0x1C, 0x1C).ToArray(),
            omniAttachment));
        }
      }
      return result.AsReadOnly();
    }

    private static ProjectedStaticLight ProjectStaticLight(
      string type,
      int physicalNumber,
      byte[] record,
      byte[] attachmentRecord)
    {
      var position = new Vector3(
        ReadNonNegativeFinitePreview(record, 0, false),
        ReadNonNegativeFinitePreview(record, 8, false),
        ReadNonNegativeFinitePreview(record, 4, false));
      var color = new Vector3(
        ReadNonNegativeFinitePreview(record, 0x0C, true),
        ReadNonNegativeFinitePreview(record, 0x10, true),
        ReadNonNegativeFinitePreview(record, 0x14, true));
      var intensityOffset = type == "spot" ? 0x2C : 0x18;
      var intensity = ReadNonNegativeFinitePreview(record, intensityOffset, true);
      var rotation = Quaternion.Identity;
      var inner = 0f;
      var outer = MathF.PI / 4;
      if (type == "spot")
      {
        var heading = record[0x1C] * MathF.PI * 2 / 256;
        var slope = ReadSingle(record, 0x28);
        if (!float.IsFinite(slope) || !float.IsFinite(slope * slope))
        {
          slope = 0;
        }
        var direction = Vector3.Normalize(new Vector3(
          MathF.Cos(heading),
          slope,
          -MathF.Sin(heading)));
        rotation = CreateDirectionRotation(direction);
        var tangent = ReadSingle(record, 0x20);
        var distance = ReadSingle(record, 0x18);
        var product = ReadSingle(record, 0x24);
        var candidateInner = MathF.Atan(tangent);
        var candidateOuter = product / distance;
        if (float.IsFinite(candidateInner)
          && float.IsFinite(candidateOuter)
          && candidateInner >= 0
          && candidateOuter >= candidateInner
          && candidateOuter <= MathF.PI / 2)
        {
          inner = candidateInner;
          outer = candidateOuter;
        }
      }
      return new ProjectedStaticLight(
        type,
        physicalNumber,
        record,
        attachmentRecord,
        position,
        rotation,
        color,
        intensity,
        inner,
        outer);
    }

    private static Quaternion CreateDirectionRotation(Vector3 direction)
    {
      var from = -Vector3.UnitZ;
      var dot = Vector3.Dot(from, direction);
      if (dot < -0.999999f)
      {
        return Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
      }
      var cross = Vector3.Cross(from, direction);
      return Quaternion.Normalize(new Quaternion(cross, 1 + dot));
    }

    private static float ReadNonNegativeFinitePreview(byte[] record, int offset, bool nonNegative)
    {
      var value = ReadSingle(record, offset);
      return float.IsFinite(value) && (!nonNegative || value >= 0) ? value : 0;
    }

    private static float ReadSingle(byte[] record, int offset)
    {
      return BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(offset)));
    }

    internal static float ReadFinitePreview(byte[] record, int offset)
    {
      var value = BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(offset)));
      return float.IsFinite(value) ? value : 0;
    }

    internal static string GetAttachmentHelperName(int physicalNumber)
    {
      if (physicalNumber is < 5 or > 49 or >= 13 and <= 20)
      {
        throw new ArgumentOutOfRangeException(nameof(physicalNumber));
      }
      var familyStart = GetAttachmentHelperFamilyStart(physicalNumber);
      var artistLabel = familyStart switch
      {
        5 => "Emitter",
        9 => "TurretMuzzle",
        21 => "UnloadPoint",
        25 => "HitPoint",
        29 => "SmokePoint",
        33 => "WT",
        37 => "Chimney",
        39 => "SmokeTrace",
        41 => "Exhaust",
        43 => "KeelTrace",
        45 => "InterfacePivot",
        46 => "CenterPivot",
        47 => "ProductionSpotStart",
        48 => "ProductionSpotEnd",
        _ => "LandingSpot"
      };
      var localNumber = physicalNumber - familyStart + 1;
      return $"ET_{artistLabel}_{localNumber}";
    }

    internal static int GetAttachmentHelperFamilyStart(int physicalNumber)
    {
      return physicalNumber switch
      {
        <= 8 => 5,
        <= 12 => 9,
        <= 24 => 21,
        <= 28 => 25,
        <= 32 => 29,
        <= 36 => 33,
        <= 38 => 37,
        <= 40 => 39,
        <= 42 => 41,
        <= 44 => 43,
        _ => physicalNumber
      };
    }

    internal static bool TryParseAttachmentHelperName(string? name, out int physicalNumber)
    {
      for (physicalNumber = 5; physicalNumber <= 49; physicalNumber++)
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

    internal static string GetCannonHelperName(int physicalNumber)
    {
      if (physicalNumber is < 1 or > 4)
      {
        throw new ArgumentOutOfRangeException(nameof(physicalNumber));
      }
      return $"ET_Turret_{physicalNumber}";
    }

    internal static bool TryParseCannonHelperName(string? name, out int physicalNumber)
    {
      for (physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        if (string.Equals(name, GetCannonHelperName(physicalNumber), StringComparison.Ordinal))
        {
          return true;
        }
      }
      physicalNumber = 0;
      return false;
    }

    internal static string GetStaticLightHelperName(string type, int physicalNumber)
    {
      return type == "spot"
        ? $"ET_SpotLight_{physicalNumber}"
        : $"ET_OmniLight_{physicalNumber}";
    }

    internal static bool TryParseStaticLightHelperName(
      string? name,
      out string type,
      out int physicalNumber)
    {
      for (physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        if (string.Equals(name, GetStaticLightHelperName("spot", physicalNumber), StringComparison.Ordinal))
        {
          type = "spot";
          return true;
        }
        if (string.Equals(name, GetStaticLightHelperName("point", physicalNumber), StringComparison.Ordinal))
        {
          type = "point";
          return true;
        }
      }
      type = string.Empty;
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

    private sealed class ProjectedCannon
    {
      internal int PhysicalNumber { get; }
      internal byte[] AttachmentRecord { get; }
      internal byte[] RenderPositionRecord { get; }
      internal Vector3 Translation { get; }
      internal Quaternion Rotation { get; }

      internal ProjectedCannon(
        int physicalNumber,
        byte[] attachmentRecord,
        byte[] renderPositionRecord,
        Vector3 translation,
        Quaternion rotation)
      {
        PhysicalNumber = physicalNumber;
        AttachmentRecord = attachmentRecord;
        RenderPositionRecord = renderPositionRecord;
        Translation = translation;
        Rotation = rotation;
      }
    }

    private sealed class ProjectedStaticLight
    {
      internal string Type { get; }
      internal int PhysicalNumber { get; }
      internal byte[] Record { get; }
      internal byte[] AttachmentRecord { get; }
      internal Vector3 Translation { get; }
      internal Quaternion Rotation { get; }
      internal Vector3 Color { get; }
      internal float Intensity { get; }
      internal float InnerConeAngle { get; }
      internal float OuterConeAngle { get; }

      internal ProjectedStaticLight(
        string type,
        int physicalNumber,
        byte[] record,
        byte[] attachmentRecord,
        Vector3 translation,
        Quaternion rotation,
        Vector3 color,
        float intensity,
        float innerConeAngle,
        float outerConeAngle)
      {
        Type = type;
        PhysicalNumber = physicalNumber;
        Record = record;
        AttachmentRecord = attachmentRecord;
        Translation = translation;
        Rotation = rotation;
        Color = color;
        Intensity = intensity;
        InnerConeAngle = innerConeAngle;
        OuterConeAngle = outerConeAngle;
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

    private static string? TryGetAuthoringMetadata(JsonElement owner)
    {
      if (!owner.TryGetProperty("extras", out var extras)
        || extras.ValueKind != JsonValueKind.Object
        || !extras.TryGetProperty("earthtoolAuthoring", out var metadata))
      {
        return null;
      }
      if (metadata.ValueKind != JsonValueKind.String)
      {
        throw new InvalidDataException("EarthTool authoring metadata must be a string.");
      }
      return metadata.GetString()
        ?? throw new InvalidDataException("EarthTool authoring metadata cannot be null.");
    }

    private static string? ReadMaterialTextureResourceKey(JsonElement material)
    {
      if (!material.TryGetProperty("extras", out var extras)
        || extras.ValueKind != JsonValueKind.Object
        || !extras.TryGetProperty("earthtoolAuthoring", out var metadata)
        || metadata.ValueKind != JsonValueKind.String)
      {
        return null;
      }
      return CanonicalAuthoringMetadata.ReadMaterialTextureResourceKey(metadata.GetString());
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
      ReadOnlySpan<byte> binary,
      int? placementRootIndex)
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
          var values = ReadFloatAccessor(
            root,
            binary,
            sampler.GetProperty("output").GetInt32(),
            path == "rotation" ? 4 : 3,
            path == "rotation" ? "VEC4" : "VEC3");
          if (placementRootIndex == nodeIndex)
          {
            if (path is not ("translation" or "rotation" or "scale"))
            {
              throw new UnsupportedGltfDomainException("animations");
            }
            continue;
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
            values);
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
        ? ReadTextureCoordinateAccessor(root, binary, textureAccessor.GetInt32())
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
        primitive.TryGetProperty("material", out var material) ? material.GetInt32() : null,
        textureAccessor.ValueKind != JsonValueKind.Undefined);
    }

    private static float[] ReadTextureCoordinateAccessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex)
    {
      var accessor = root.GetProperty("accessors")[accessorIndex];
      var componentType = accessor.GetProperty("componentType").GetInt32();
      if (componentType == 5126)
      {
        return ReadFloatAccessor(root, binary, accessorIndex, 2, "VEC2");
      }
      if (componentType is not (5121 or 5123)
        || accessor.GetProperty("type").GetString() != "VEC2"
        || !accessor.TryGetProperty("normalized", out var normalized)
        || !normalized.GetBoolean()
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
      var componentSize = componentType == 5121 ? 1 : 2;
      var elementSize = componentSize * 2;
      var stride = view.TryGetProperty("byteStride", out var strideValue)
        ? strideValue.GetInt32()
        : elementSize;
      if (stride < elementSize)
      {
        throw new InvalidDataException("Texture-coordinate accessor stride is too small.");
      }
      var offset = checked(GetOffset(view) + GetOffset(accessor));
      EnsureRange(binary.Length, offset, count, stride, elementSize);
      var result = new float[count * 2];
      var maximum = componentType == 5121 ? byte.MaxValue : ushort.MaxValue;
      for (var element = 0; element < count; element++)
      {
        for (var component = 0; component < 2; component++)
        {
          var componentOffset = checked(offset + element * stride + component * componentSize);
          var value = componentType == 5121
            ? binary[componentOffset]
            : ReadUInt16(binary, componentOffset);
          result[element * 2 + component] = (float)value / maximum;
        }
      }
      return result;
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
        || accessor.TryGetProperty("normalized", out var normalized) && normalized.GetBoolean())
      {
        throw new UnsupportedGltfDomainException("VertexAccessor");
      }

      var count = accessor.GetProperty("count").GetInt32();
      if (count <= 0 || count > 65536)
      {
        throw new UnsupportedGltfDomainException("VertexAccessor");
      }

      var elementSize = dimensions * sizeof(float);
      var result = new float[count * dimensions];
      if (accessor.TryGetProperty("bufferView", out var viewIndex))
      {
        var view = root.GetProperty("bufferViews")[viewIndex.GetInt32()];
        EnsureBufferView(view);
        var stride = view.TryGetProperty("byteStride", out var strideValue)
          ? strideValue.GetInt32()
          : elementSize;
        if (stride < elementSize)
        {
          throw new InvalidDataException("Vertex accessor stride is too small.");
        }
        var offset = checked(GetOffset(view) + GetOffset(accessor));
        EnsureRange(binary.Length, offset, count, stride, elementSize);
        for (var element = 0; element < count; element++)
        {
          for (var component = 0; component < dimensions; component++)
          {
            result[element * dimensions + component] = ReadFiniteAccessorFloat(
              binary,
              checked(offset + element * stride + component * sizeof(float)));
          }
        }
      }

      if (accessor.TryGetProperty("sparse", out var sparse))
      {
        var sparseCount = sparse.GetProperty("count").GetInt32();
        if (sparseCount <= 0 || sparseCount > count)
        {
          throw new UnsupportedGltfDomainException("VertexAccessor");
        }
        var sparseIndices = sparse.GetProperty("indices");
        var indexComponentType = sparseIndices.GetProperty("componentType").GetInt32();
        if (indexComponentType is not (5121 or 5123 or 5125))
        {
          throw new UnsupportedGltfDomainException("VertexAccessor");
        }
        var indexSize = indexComponentType == 5121 ? 1 : indexComponentType == 5123 ? 2 : 4;
        var indexView = root.GetProperty("bufferViews")[sparseIndices.GetProperty("bufferView").GetInt32()];
        EnsureBufferView(indexView);
        var indexOffset = checked(GetOffset(indexView) + GetOffset(sparseIndices));
        EnsureRange(binary.Length, indexOffset, sparseCount, indexSize, indexSize);
        var sparseValues = sparse.GetProperty("values");
        var valueView = root.GetProperty("bufferViews")[sparseValues.GetProperty("bufferView").GetInt32()];
        EnsureBufferView(valueView);
        var valueOffset = checked(GetOffset(valueView) + GetOffset(sparseValues));
        EnsureRange(binary.Length, valueOffset, sparseCount, elementSize, elementSize);
        for (var sparseIndex = 0; sparseIndex < sparseCount; sparseIndex++)
        {
          var indexOffsetValue = checked(indexOffset + sparseIndex * indexSize);
          var targetIndex = indexComponentType == 5121
            ? binary[indexOffsetValue]
            : indexComponentType == 5123
              ? ReadUInt16(binary, indexOffsetValue)
              : checked((int)ReadUInt32(binary, indexOffsetValue));
          if (targetIndex < 0 || targetIndex >= count)
          {
            throw new InvalidDataException("Sparse accessor index is outside the accessor range.");
          }
          for (var component = 0; component < dimensions; component++)
          {
            result[targetIndex * dimensions + component] = ReadFiniteAccessorFloat(
              binary,
              checked(valueOffset + sparseIndex * elementSize + component * sizeof(float)));
          }
        }
      }

      if (!accessor.TryGetProperty("bufferView", out _)
        && !accessor.TryGetProperty("sparse", out _))
      {
        throw new UnsupportedGltfDomainException("VertexAccessor");
      }

      return result;
    }

    private static float ReadFiniteAccessorFloat(ReadOnlySpan<byte> binary, int offset)
    {
      var result = BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(binary.Slice(offset, sizeof(float))));
      if (!float.IsFinite(result))
      {
        throw new InvalidDataException("Vertex accessor contains a non-finite value.");
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

  internal sealed class UnsupportedGltfDomainException : Exception
  {
    internal string Domain { get; }

    internal string? Path { get; }

    internal UnsupportedGltfDomainException(string domain, string? path = null)
      : base($"The {domain} domain is outside the one-triangle walking-skeleton profile.")
    {
      Domain = domain;
      Path = path;
    }
  }
}
