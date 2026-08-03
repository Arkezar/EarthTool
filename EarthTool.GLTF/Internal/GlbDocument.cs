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

    internal IReadOnlyList<ParsedGltfLight> Lights { get; }

    internal int RootNodeIndex { get; }

    internal MetadataConflictCollector MetadataConflicts { get; }

    internal IReadOnlyList<string> IgnoredInertPaths { get; }

    internal ISet<string> DiscardedMetadataScopes { get; } = new HashSet<string>(StringComparer.Ordinal);

    internal ISet<string> AcceptedDeletionScopes { get; } = new HashSet<string>(StringComparer.Ordinal);

    internal int[]? NewModelNodeOrder { get; set; }

    internal int[]? NewModelMaterialOrder { get; set; }

    internal int[]? NewModelLightOrder { get; set; }

    internal ParsedGlb(
      string? manifestMetadata,
      bool hasReservedMetadata,
      IReadOnlyList<ParsedGltfMesh> meshes,
      IReadOnlyList<ParsedGltfNode> nodes,
      IReadOnlyList<ParsedGltfMaterial> materials,
      IReadOnlyList<ParsedGltfAnimation> animations,
      IReadOnlyList<ParsedGltfLight> lights,
      int rootNodeIndex,
      MetadataConflictCollector metadataConflicts,
      IReadOnlyList<string> ignoredInertPaths)
    {
      ManifestMetadata = manifestMetadata;
      HasReservedMetadata = hasReservedMetadata;
      Meshes = meshes;
      Nodes = nodes;
      Materials = materials;
      Animations = animations;
      Lights = lights;
      RootNodeIndex = rootNodeIndex;
      MetadataConflicts = metadataConflicts;
      IgnoredInertPaths = ignoredInertPaths;
    }
  }

  internal sealed class ParsedGltfNode
  {
    internal string? Name { get; }

    internal bool IsPlacementRoot { get; }

    internal string? Metadata { get; }

    internal int? MeshIndex { get; }

    internal int? LightIndex { get; }

    internal int? CameraIndex { get; }

    internal IReadOnlyList<int> Children { get; }

    internal Matrix4x4 LocalTransform { get; }

    internal ParsedGltfNode(
      string? name,
      bool isPlacementRoot,
      string? metadata,
      int? meshIndex,
      int? lightIndex,
      int? cameraIndex,
      IReadOnlyList<int> children,
      Matrix4x4 localTransform)
    {
      Name = name;
      IsPlacementRoot = isPlacementRoot;
      Metadata = metadata;
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

    internal string? Metadata { get; }

    internal string Type { get; }

    internal Vector3 Color { get; }

    internal float Intensity { get; }

    internal float? Range { get; }

    internal float InnerConeAngle { get; }

    internal float OuterConeAngle { get; }

    internal ParsedGltfLight(
      string? name,
      string? metadata,
      string type,
      Vector3 color,
      float intensity,
      float? range,
      float innerConeAngle,
      float outerConeAngle)
    {
      Name = name;
      Metadata = metadata;
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

  internal sealed class MetadataSourceProvenance
  {
    internal int ByteLength { get; }

    internal string Sha256 { get; }

    internal MetadataSourceProvenance(int byteLength, string sha256)
    {
      ByteLength = byteLength;
      Sha256 = sha256;
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

    internal int? CannonPhysicalNumber { get; }

    internal IReadOnlyList<byte>? CannonAttachmentRecord { get; }

    internal IReadOnlyList<byte>? CannonRenderPositionRecord { get; }

    internal string? StaticLightType { get; }

    internal int? StaticLightPhysicalNumber { get; }

    internal int? StaticLightDefinitionLocalId { get; }

    internal IReadOnlyList<byte>? StaticLightRecord { get; }

    internal IReadOnlyList<byte>? StaticLightAttachmentRecord { get; }

    internal IReadOnlyDictionary<string, string> Guards { get; }

    internal IReadOnlyDictionary<string, (string Projection, int Version)> GuardProjections { get; }

    internal IReadOnlyDictionary<string, IReadOnlyList<int>> ScopeInventory { get; }

    internal IReadOnlyDictionary<string, int> ScopeNextIds { get; }

    internal MetadataSourceProvenance? SourceProvenance { get; }

    internal IReadOnlyDictionary<string, string> UnknownMembers { get; }

    internal int ElementCount { get; }

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
      int? cannonPhysicalNumber,
      IReadOnlyList<byte>? cannonAttachmentRecord,
      IReadOnlyList<byte>? cannonRenderPositionRecord,
      string? staticLightType,
      int? staticLightPhysicalNumber,
      int? staticLightDefinitionLocalId,
      IReadOnlyList<byte>? staticLightRecord,
      IReadOnlyList<byte>? staticLightAttachmentRecord,
      IReadOnlyDictionary<string, string> guards,
      IReadOnlyDictionary<string, (string Projection, int Version)> guardProjections,
      IReadOnlyDictionary<string, IReadOnlyList<int>> scopeInventory,
      IReadOnlyDictionary<string, int> scopeNextIds,
      MetadataSourceProvenance? sourceProvenance,
      IReadOnlyDictionary<string, string> unknownMembers,
      int elementCount)
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
      CannonPhysicalNumber = cannonPhysicalNumber;
      CannonAttachmentRecord = cannonAttachmentRecord;
      CannonRenderPositionRecord = cannonRenderPositionRecord;
      StaticLightType = staticLightType;
      StaticLightPhysicalNumber = staticLightPhysicalNumber;
      StaticLightDefinitionLocalId = staticLightDefinitionLocalId;
      StaticLightRecord = staticLightRecord;
      StaticLightAttachmentRecord = staticLightAttachmentRecord;
      Guards = guards;
      GuardProjections = guardProjections;
      ScopeInventory = scopeInventory;
      ScopeNextIds = scopeNextIds;
      SourceProvenance = sourceProvenance;
      UnknownMembers = unknownMembers;
      ElementCount = elementCount;
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
      InterchangeBaseline baseline,
      IReadOnlyDictionary<string, string> unknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds,
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      string? sourceBaseName,
      out NativeProjectionFingerprint fingerprint)
    {
      var package = CreatePackage(
        asset,
        baseline,
        unknownMetadata,
        metadataNextIds,
        false,
        previews,
        sourceBaseName,
        out fingerprint);
      return Pack(package.Json, package.Binary);
    }

    internal static GltfPackage CreateSeparate(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      IReadOnlyDictionary<string, string> unknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds,
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      string? sourceBaseName,
      out NativeProjectionFingerprint fingerprint)
    {
      return CreatePackage(
        asset,
        baseline,
        unknownMetadata,
        metadataNextIds,
        true,
        previews,
        sourceBaseName,
        out fingerprint);
    }

    internal static int GetMaximumMetadataByteCount(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      IReadOnlyDictionary<string, string> unknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds)
    {
      var animations = StaticAnimationProjection.Create(asset, baseline);
      var empty = CreateMetadata(
        baseline,
        "manifest",
        0,
        string.Empty,
        null,
        unknownMetadata,
        metadataNextIds,
        asset,
        animations);
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
          unknownMetadata,
          metadataNextIds,
          animationProjection: animations.Objects.SingleOrDefault(item =>
            item.SourceObjectLocalId == source.Id.Value));
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(metadata));
      }
      foreach (var attachment in ProjectAttachments(asset))
      {
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(
          CreateAttachmentMetadata(baseline, attachment, unknownMetadata)));
      }
      foreach (var cannon in ProjectCannons(asset))
      {
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(
          CreateCannonMetadata(baseline, cannon, unknownMetadata)));
      }
      foreach (var light in ProjectStaticLights(asset))
      {
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(
          CreateStaticLightInstanceMetadata(baseline, light, unknownMetadata)));
        maximum = Math.Max(maximum, Encoding.UTF8.GetByteCount(
          CreateStaticLightMetadata(baseline, light, unknownMetadata)));
      }
      return maximum;
    }

    internal static int GetMinimumOutputByteCount(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      IReadOnlyDictionary<string, string> unknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds,
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
        + GetMaximumMetadataByteCount(asset, baseline, unknownMetadata, metadataNextIds)
        + containerBytes));
    }

    private static GltfPackage CreatePackage(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      IReadOnlyDictionary<string, string> unknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds,
      bool separate,
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      string? sourceBaseName,
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
        EncodeBase64Url(asset.GetSerializedRepresentation()),
        null,
        unknownMetadata,
        metadataNextIds,
        asset,
        animations);
      var json = CreateJson(
        asset,
        layouts,
        binary.Length,
        baseline,
        manifest,
        unknownMetadata,
        metadataNextIds,
        previewLayouts,
        animations,
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
      var metadataConflicts = new MetadataConflictCollector(profile.MaxMetadataConflicts);
      ValidateSupportedGraph(root, profile, intent);
      ValidateMetadataGraph(root, profile, intent, metadataConflicts);
      var rootNodeIndex = ResolveRootNodeIndex(root, intent, out var placementRootIndex);
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
          TryGetPlacementRootMarker(node, out var isPlacementRoot) && isPlacementRoot,
          TryGetMetadata(node),
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

        meshes.Add(new ParsedGltfMesh(
          TryGetMetadata(mesh),
          primitives.AsReadOnly()));
      }

      var materials = root.TryGetProperty("materials", out var materialArray)
        ? materialArray.EnumerateArray()
          .Select(material => new ParsedGltfMaterial(
            TryGetMetadata(material),
            material.TryGetProperty("pbrMetallicRoughness", out var pbr)
              && pbr.TryGetProperty("baseColorTexture", out _)))
          .ToArray()
        : Array.Empty<ParsedGltfMaterial>();
      var animations = ReadAnimations(root, binary, placementRootIndex);
      var lights = ReadLights(root, intent);

      return new ParsedGlb(
        manifest,
        HasReservedMetadata(root),
        meshes.AsReadOnly(),
        nodes.AsReadOnly(),
        Array.AsReadOnly(materials),
        animations,
        lights,
        rootNodeIndex,
        metadataConflicts,
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
          || TryGetMetadata(sceneRoot) is not null
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
        && !sceneRoot.TryGetProperty("mesh", out _)
        && TryGetMetadata(sceneRoot) is null)
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

    private static void ValidateMetadataGraph(
      JsonElement root,
      GltfOperationProfile profile,
      GltfImportIntent intent,
      MetadataConflictCollector conflicts)
    {
      var allowedCarriers = new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["scenes[0]"] = "manifest"
      };
      AddAllowedCarriers(allowedCarriers, root, "nodes", "object");
      AddAllowedCarriers(allowedCarriers, root, "meshes", "mesh");
      AddAllowedCarriers(allowedCarriers, root, "materials", "material");
      if (root.TryGetProperty("extensions", out var extensions)
        && extensions.TryGetProperty("KHR_lights_punctual", out var punctual)
        && punctual.TryGetProperty("lights", out var lights))
      {
        for (var index = 0; index < lights.GetArrayLength(); index++)
        {
          allowedCarriers.Add($"extensions.KHR_lights_punctual.lights[{index}]", "light");
        }
      }

      var carriers = new List<(string Path, string Value)>();
      long metadataBytes = 0;
      CollectMetadataCarriers(
        root,
        "$",
        allowedCarriers,
        carriers,
        profile,
        ref metadataBytes);
      if (intent == GltfImportIntent.NewModel)
      {
        if (carriers.Count != 0)
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.OrphanEnvelope,
            2011,
            carriers[0].Path,
            "New-model import cannot consume a claimed EarthTool metadata lineage.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.AdoptAsNew,
            GltfMetadataConflictActions.DiscardLineage);
        }
        return;
      }

      var manifestCarrier = carriers.SingleOrDefault(carrier => carrier.Path == "scenes[0]");
      if (manifestCarrier == default)
      {
        throw new MetadataConflictException(
          GltfDiagnosticCodes.MissingManifest,
          2000,
          "scenes[0]",
          "The edit document has no EarthTool metadata manifest.",
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.DiscardLineage);
      }
      if (carriers.Count > profile.MaxMetadataEnvelopes)
      {
        throw MetadataLimit("metadata", carriers.Count, profile.MaxMetadataEnvelopes);
      }

      long totalBytes = 0;
      long totalElements = 0;
      long totalUnknownMembers = 0;
      var parsed = new List<(string Path, string CarrierKind, MetadataEnvelope Envelope)>();
      foreach (var carrier in carriers.OrderBy(item => item.Path == "scenes[0]" ? string.Empty : item.Path,
        StringComparer.Ordinal))
      {
        var bytes = Encoding.UTF8.GetByteCount(carrier.Value);
        totalBytes = checked(totalBytes + bytes);
        if (totalBytes > profile.MaxTotalMetadataBytes)
        {
          throw MetadataLimit(carrier.Path, totalBytes, profile.MaxTotalMetadataBytes);
        }
        MetadataEnvelope envelope;
        try
        {
          envelope = ParseMetadata(
            carrier.Value,
            profile,
            checked(profile.MaxMetadataElements - (int)totalElements),
            checked(profile.MaxUnknownMetadataMembers - (int)totalUnknownMembers));
        }
        catch (UnsupportedMetadataVersionException)
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.UnsupportedMetadataVersion,
            2004,
            carrier.Path,
            "The EarthTool metadata version is unsupported and remains opaque.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage);
        }
        catch (MalformedMetadataException ex)
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.MalformedMetadata,
            2003,
            carrier.Path,
            ex.Message,
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage);
        }
        catch (MetadataConflictException ex)
        {
          throw new MetadataConflictException(
            ex.Code,
            ex.EventId,
            ex.Path == "$" ? carrier.Path : carrier.Path + "." + ex.Path,
            ex.Message,
            ex.ConflictData,
            ex.Actions.ToArray());
        }
        catch (ResourceLimitException limit)
        {
          throw MetadataLimit(carrier.Path, limit.Actual, limit.Maximum);
        }
        totalElements += envelope.ElementCount;
        totalUnknownMembers += envelope.UnknownMembers.Count;
        if (totalElements > profile.MaxMetadataElements)
        {
          throw MetadataLimit(carrier.Path, totalElements, profile.MaxMetadataElements);
        }
        if (totalUnknownMembers > profile.MaxUnknownMetadataMembers)
        {
          throw MetadataLimit(
            carrier.Path,
            totalUnknownMembers,
            profile.MaxUnknownMetadataMembers);
        }
        var carrierKind = allowedCarriers[carrier.Path];
        if (envelope.ScopeKind != carrierKind)
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.KindCarrierMismatch,
            2008,
            carrier.Path,
            "The metadata envelope kind does not match its glTF carrier.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.DiscardAffectedState);
        }
        if (carrierKind == "manifest" ? envelope.LocalId != 0 : envelope.LocalId <= 0)
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.MalformedMetadata,
            2003,
            carrier.Path,
            "The metadata local ID is outside its allowed range.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage);
        }
        parsed.Add((carrier.Path, carrierKind, envelope));
      }

      var manifest = parsed.Single(item => item.Path == "scenes[0]").Envelope;
      foreach (var item in parsed.Where(item => item.Path != "scenes[0]"))
      {
        if (item.Envelope.AssetLineageId != manifest.AssetLineageId)
        {
          conflicts.Add(IdentityConflict(
            GltfDiagnosticCodes.AssetLineageMismatch,
            2006,
            item.Path,
            item.Envelope,
            "The metadata envelope belongs to a foreign asset lineage.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.AdoptAsNew,
            GltfMetadataConflictActions.DiscardLineage));
        }
        if (item.Envelope.DocumentId != manifest.DocumentId)
        {
          conflicts.Add(IdentityConflict(
            GltfDiagnosticCodes.DocumentMismatch,
            2007,
            item.Path,
            item.Envelope,
            "The metadata envelope belongs to another document branch.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.AcceptBranch));
        }
      }

      foreach (var duplicate in parsed.GroupBy(
          item => (item.Envelope.ScopeKind, item.Envelope.LocalId))
        .Where(group => group.Count() > 1))
      {
        foreach (var item in duplicate.OrderBy(value => value.Path, StringComparer.Ordinal).Skip(1))
        {
          conflicts.Add(IdentityConflict(
            GltfDiagnosticCodes.DuplicateScopeIdentity,
            2009,
            item.Path,
            item.Envelope,
            "More than one metadata envelope claims the same scope identity.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.DiscardAffectedState));
        }
      }

      try
      {
        ValidateManifestInventory(manifest, parsed, profile);
      }
      catch (MetadataConflictException conflict)
      {
        conflicts.Add(conflict);
      }
    }

    internal static void RevalidateParsedMetadataGraph(ParsedGlb parsed, GltfOperationProfile profile)
    {
      var graph = new List<(string Path, string CarrierKind, MetadataEnvelope Envelope)>();
      if (parsed.ManifestMetadata is null)
      {
        throw new MetadataConflictException(
          GltfDiagnosticCodes.MissingManifest,
          2000,
          "scenes[0]",
          "The edit document has no EarthTool metadata manifest.",
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.DiscardLineage);
      }
      AddParsedEnvelope(graph, "scenes[0]", "manifest", parsed.ManifestMetadata, profile);
      for (var index = 0; index < parsed.Nodes.Count; index++)
      {
        AddParsedEnvelope(graph, $"nodes[{index}]", "object", parsed.Nodes[index].Metadata, profile);
      }
      for (var index = 0; index < parsed.Meshes.Count; index++)
      {
        AddParsedEnvelope(graph, $"meshes[{index}]", "mesh", parsed.Meshes[index].Metadata, profile);
      }
      for (var index = 0; index < parsed.Materials.Count; index++)
      {
        AddParsedEnvelope(graph, $"materials[{index}]", "material", parsed.Materials[index].Metadata, profile);
      }
      for (var index = 0; index < parsed.Lights.Count; index++)
      {
        AddParsedEnvelope(
          graph,
          $"extensions.KHR_lights_punctual.lights[{index}]",
          "light",
          parsed.Lights[index].Metadata,
          profile);
      }

      var manifest = graph.Single(item => item.Path == "scenes[0]").Envelope;
      foreach (var item in graph.Where(item => item.Path != "scenes[0]"))
      {
        if (item.Envelope.AssetLineageId != manifest.AssetLineageId)
        {
          parsed.MetadataConflicts.Add(IdentityConflict(
            GltfDiagnosticCodes.AssetLineageMismatch,
            2006,
            item.Path,
            item.Envelope,
            "The metadata envelope belongs to a foreign asset lineage.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.AdoptAsNew,
            GltfMetadataConflictActions.DiscardLineage));
        }
        if (item.Envelope.DocumentId != manifest.DocumentId)
        {
          parsed.MetadataConflicts.Add(IdentityConflict(
            GltfDiagnosticCodes.DocumentMismatch,
            2007,
            item.Path,
            item.Envelope,
            "The metadata envelope belongs to another document branch.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.AcceptBranch));
        }
      }
      foreach (var duplicate in graph.GroupBy(item =>
        (item.Envelope.ScopeKind, item.Envelope.LocalId)).Where(group => group.Count() > 1))
      {
        foreach (var item in duplicate.OrderBy(value => value.Path, StringComparer.Ordinal).Skip(1))
        {
          parsed.MetadataConflicts.Add(IdentityConflict(
            GltfDiagnosticCodes.DuplicateScopeIdentity,
            2009,
            item.Path,
            item.Envelope,
            "More than one metadata envelope claims the same scope identity.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.DiscardAffectedState));
        }
      }
      try
      {
        ValidateManifestInventory(manifest, graph, profile);
      }
      catch (MetadataConflictException conflict)
      {
        parsed.MetadataConflicts.Add(conflict);
      }
    }

    private static void AddParsedEnvelope(
      ICollection<(string Path, string CarrierKind, MetadataEnvelope Envelope)> graph,
      string path,
      string carrierKind,
      string? value,
      GltfOperationProfile profile)
    {
      if (value is null)
      {
        return;
      }
      var envelope = ParseMetadata(value, profile);
      if (envelope.ScopeKind != carrierKind)
      {
        throw new MetadataConflictException(
          GltfDiagnosticCodes.KindCarrierMismatch,
          2008,
          path,
          "The metadata envelope kind does not match its glTF carrier.",
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.MapScope,
          GltfMetadataConflictActions.ForkScope,
          GltfMetadataConflictActions.DiscardAffectedState);
      }
      graph.Add((path, carrierKind, envelope));
    }

    private static void AddAllowedCarriers(
      IDictionary<string, string> carriers,
      JsonElement root,
      string collectionName,
      string kind)
    {
      if (!root.TryGetProperty(collectionName, out var collection))
      {
        return;
      }
      for (var index = 0; index < collection.GetArrayLength(); index++)
      {
        carriers.Add($"{collectionName}[{index}]", kind);
      }
    }

    private static void CollectMetadataCarriers(
      JsonElement element,
      string path,
      IReadOnlyDictionary<string, string> allowedCarriers,
      ICollection<(string Path, string Value)> result,
      GltfOperationProfile profile,
      ref long metadataBytes)
    {
      if (element.ValueKind == JsonValueKind.Object)
      {
        var extrasProperties = element.EnumerateObject()
          .Where(property => property.Name == "extras")
          .ToArray();
        var reserved = extrasProperties
          .Where(property => property.Value.ValueKind == JsonValueKind.Object)
          .SelectMany(property => property.Value.EnumerateObject())
          .Where(property => property.Name == "earthtool")
          .ToArray();
        if (reserved.Length > 0 && extrasProperties.Length > 1)
        {
          throw CarrierConflict(path, "The extras object containing EarthTool metadata occurs more than once.");
        }
        if (reserved.Length > 1)
        {
          throw CarrierConflict(path, "The reserved EarthTool carrier occurs more than once.");
        }
        if (reserved.Length == 1)
        {
          if (!allowedCarriers.ContainsKey(path.TrimStart('$').TrimStart('.')))
          {
            throw new MetadataConflictException(
              GltfDiagnosticCodes.OrphanEnvelope,
              2011,
              path,
              "EarthTool metadata appears on an unsupported carrier.",
              GltfMetadataConflictActions.Abort,
              GltfMetadataConflictActions.MapScope,
              GltfMetadataConflictActions.DiscardAffectedState,
              GltfMetadataConflictActions.DiscardLineage);
          }
          if (reserved[0].Value.ValueKind != JsonValueKind.String)
          {
            throw CarrierConflict(path, "EarthTool metadata must be a non-empty string envelope.");
          }
          if (result.Count >= profile.MaxMetadataEnvelopes)
          {
            throw MetadataLimit("metadata", result.Count + 1L, profile.MaxMetadataEnvelopes);
          }
          var value = reserved[0].Value.GetString();
          if (string.IsNullOrEmpty(value))
          {
            throw CarrierConflict(path, "EarthTool metadata must be a non-empty string envelope.");
          }
          var decodedBytes = Encoding.UTF8.GetByteCount(value);
          metadataBytes = checked(metadataBytes + decodedBytes);
          if (decodedBytes > profile.MaxMetadataBytes)
          {
            throw MetadataLimit(path, decodedBytes, profile.MaxMetadataBytes);
          }
          if (metadataBytes > profile.MaxTotalMetadataBytes)
          {
            throw MetadataLimit(path, metadataBytes, profile.MaxTotalMetadataBytes);
          }
          result.Add((path.TrimStart('$').TrimStart('.'), value));
        }

        foreach (var property in element.EnumerateObject())
        {
          if (property.Name == "extras")
          {
            continue;
          }
          var childPath = path == "$" ? property.Name : path + "." + property.Name;
          CollectMetadataCarriers(
            property.Value,
            childPath,
            allowedCarriers,
            result,
            profile,
            ref metadataBytes);
        }
      }
      else if (element.ValueKind == JsonValueKind.Array)
      {
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
          CollectMetadataCarriers(
            item,
            $"{path}[{index++}]",
            allowedCarriers,
            result,
            profile,
            ref metadataBytes);
        }
      }
    }

    private static MetadataConflictException CarrierConflict(string path, string message)
    {
      return new MetadataConflictException(
        GltfDiagnosticCodes.InvalidMetadataCarrier,
        2002,
        path,
        message,
        GltfMetadataConflictActions.Abort,
        GltfMetadataConflictActions.RetryWithMetadata,
        GltfMetadataConflictActions.DiscardLineage);
    }

    private static MetadataConflictException MetadataLimit(string path, long actual, int maximum)
    {
      return new MetadataConflictException(
        GltfDiagnosticCodes.MetadataResourceLimitExceeded,
        2005,
        path,
        "The metadata graph exceeds its finite operation profile.",
        new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(System.Globalization.CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)
        },
        GltfMetadataConflictActions.Abort,
        GltfMetadataConflictActions.RetryWithMetadata);
    }

    private static MetadataConflictException IdentityConflict(
      string code,
      int eventId,
      string path,
      MetadataEnvelope envelope,
      string message,
      params string[] actions)
    {
      return new MetadataConflictException(
        code,
        eventId,
        path,
        message,
        new Dictionary<string, string>
        {
          ["lineage"] = envelope.AssetLineageId.ToString("D"),
          ["document"] = envelope.DocumentId.ToString("D"),
          ["scopeKind"] = envelope.ScopeKind,
          ["localId"] = envelope.LocalId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        },
        actions);
    }

    private static void ValidateManifestInventory(
      MetadataEnvelope manifest,
      IReadOnlyList<(string Path, string CarrierKind, MetadataEnvelope Envelope)> graph,
      GltfOperationProfile profile)
    {
      if (manifest.ScopeInventory.Count != 4 || manifest.ScopeNextIds.Count != 4)
      {
        throw InvalidInventory("The manifest must declare all supported scope inventories.");
      }
      var inventoryEntries = manifest.ScopeInventory.Values.Sum(ids => (long)ids.Count);
      var totalScopes = checked(graph.Count + inventoryEntries);
      if (totalScopes > profile.MaxMetadataEnvelopes)
      {
        throw MetadataLimit("scenes[0]", totalScopes, profile.MaxMetadataEnvelopes);
      }
      foreach (var kind in new[] { "object", "mesh", "material", "light" })
      {
        var ids = manifest.ScopeInventory[kind];
        if (ids.Any(id => id <= 0)
          || ids.Zip(ids.Skip(1), (left, right) => left < right).Any(increasing => !increasing)
          || manifest.ScopeNextIds[kind] <= 0
          || ids.Count > 0 && manifest.ScopeNextIds[kind] <= ids[^1])
        {
          throw InvalidInventory("A manifest inventory is not strictly increasing or has an invalid high-water mark.");
        }
      }
      foreach (var item in graph.Where(item => item.Envelope.ScopeKind != "manifest"))
      {
        if (!manifest.ScopeInventory[item.Envelope.ScopeKind].Contains(item.Envelope.LocalId))
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.OrphanEnvelope,
            2011,
            item.Path,
            "A metadata envelope is absent from the manifest inventory.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.DiscardAffectedState);
        }
      }
    }

    private static MetadataConflictException InvalidInventory(string message)
    {
      return new MetadataConflictException(
        GltfDiagnosticCodes.InvalidManifestInventory,
        2020,
        "scenes[0]",
        message,
        GltfMetadataConflictActions.Abort,
        GltfMetadataConflictActions.RetryWithMetadata,
        GltfMetadataConflictActions.DiscardLineage);
    }

    private static int? TryGetLightIndex(JsonElement node)
    {
      return node.TryGetProperty("extensions", out var extensions)
        && extensions.TryGetProperty("KHR_lights_punctual", out var light)
        && light.TryGetProperty("light", out var index)
        ? index.GetInt32()
        : null;
    }

    private static IReadOnlyList<ParsedGltfLight> ReadLights(
      JsonElement root,
      GltfImportIntent intent)
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
          TryGetMetadata(light),
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

    internal static MetadataEnvelope ParseMetadata(string value, GltfOperationProfile profile)
    {
      return ParseMetadata(
        value,
        profile,
        profile.MaxMetadataElements,
        profile.MaxUnknownMetadataMembers);
    }

    private static MetadataEnvelope ParseMetadata(
      string value,
      GltfOperationProfile profile,
      int remainingElements,
      int remainingUnknownMembers)
    {
      var byteCount = Encoding.UTF8.GetByteCount(value);
      if (byteCount > profile.MaxMetadataBytes)
      {
        throw new ResourceLimitException(byteCount, profile.MaxMetadataBytes);
      }

      try
      {
        var json = Encoding.UTF8.GetBytes(value);
        var elementCount = ValidateJsonLimits(json, profile.MaxJsonDepth, remainingElements);
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
          MaxDepth = profile.MaxJsonDepth,
          CommentHandling = JsonCommentHandling.Disallow,
          AllowTrailingCommas = false
        });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
          throw new MalformedMetadataException("An EarthTool envelope must be a JSON object.");
        }
        var versionProperties = root.EnumerateObject().Where(property => property.Name == "version").ToArray();
        if (versionProperties.Length != 1 || versionProperties[0].Value.ValueKind != JsonValueKind.Number)
        {
          throw new MalformedMetadataException("The metadata version is missing or malformed.");
        }
        if (!versionProperties[0].Value.TryGetInt32(out var version))
        {
          throw new MalformedMetadataException("The metadata version must be an integer.");
        }
        if (version != 1)
        {
          throw new UnsupportedMetadataVersionException();
        }
        ValidateNoDuplicateMembers(root);

        if (root.GetProperty("format").ValueKind != JsonValueKind.String
          || root.GetProperty("format").GetString() != "earthtool.msh.gltf")
        {
          throw new MalformedMetadataException("Unsupported EarthTool metadata format.");
        }
        var kind = ReadRequiredString(root, "kind");
        var lineage = ReadVersion4Guid(root, "lineage");
        var documentId = ReadVersion4Guid(root, "document");
        var localId = root.GetProperty("id").GetInt32();
        if (kind is not ("manifest" or "object" or "mesh" or "material" or "light"))
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.UnknownRequiredSemantics,
            2018,
            "$",
            "The metadata scope kind is not supported.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage);
        }

        var guardObject = root.GetProperty("guards");
        if (guardObject.ValueKind != JsonValueKind.Object)
        {
          throw new MalformedMetadataException("The metadata guards member must be an object.");
        }
        if (guardObject.EnumerateObject().Count() > profile.MaxMetadataGuards)
        {
          throw new ResourceLimitException(
            guardObject.EnumerateObject().Count(),
            profile.MaxMetadataGuards);
        }
        var guards = new Dictionary<string, string>(StringComparer.Ordinal);
        var guardProjections = new Dictionary<string, (string Projection, int Version)>(StringComparer.Ordinal);
        foreach (var guard in guardObject.EnumerateObject())
        {
          if (!IsKnownGuard(kind, guard.Name))
          {
            continue;
          }
          if (guard.Value.ValueKind != JsonValueKind.Object)
          {
            throw new MalformedMetadataException("A metadata guard must be an object.");
          }
          var projection = ReadRequiredString(guard.Value, "projection");
          var projectionVersion = guard.Value.GetProperty("version").GetInt32();
          var algorithm = ReadRequiredString(guard.Value, "algorithm");
          var digest = ReadRequiredString(guard.Value, "digest");
          if (projectionVersion <= 0 || algorithm != "sha256")
          {
            throw new MetadataConflictException(
              GltfDiagnosticCodes.UnsupportedGuard,
              2015,
              "guards." + guard.Name,
              "The metadata guard projection is unsupported.",
              GltfMetadataConflictActions.Abort,
              GltfMetadataConflictActions.RetryWithMetadata,
              GltfMetadataConflictActions.DiscardAffectedState);
          }
          guards.Add(guard.Name, DecodeSha256(digest));
          guardProjections.Add(guard.Name, (projection, projectionVersion));
        }

        var payload = root.GetProperty("payload");
        if (payload.ValueKind != JsonValueKind.Object)
        {
          throw new MalformedMetadataException("The metadata payload member must be an object.");
        }
        var envelopePayload = payload;
        MetadataSourceProvenance? sourceProvenance = null;
        if (kind == "manifest")
        {
          if (!payload.TryGetProperty("origin", out var origin)
            || origin.ValueKind != JsonValueKind.Object
            || !origin.TryGetProperty("kind", out var originKind)
            || originKind.ValueKind != JsonValueKind.String
            || !payload.TryGetProperty("asset", out var assetPayload)
            || assetPayload.ValueKind != JsonValueKind.Object)
          {
            throw new MalformedMetadataException(
              "The manifest origin or asset payload is missing or malformed.");
          }
          if (originKind.GetString() is not ("mshExport" or "newModel" or "lineageFork"))
          {
            throw new MetadataConflictException(
              GltfDiagnosticCodes.UnknownRequiredSemantics,
              2018,
              "$",
              "The manifest origin kind requires unsupported semantics.",
              GltfMetadataConflictActions.Abort,
              GltfMetadataConflictActions.RetryWithMetadata,
              GltfMetadataConflictActions.DiscardLineage);
          }
          if (origin.TryGetProperty("source", out var originSource))
          {
            if (originSource.ValueKind != JsonValueKind.Object
              || !originSource.TryGetProperty("byteLength", out var sourceByteLength)
              || !sourceByteLength.TryGetInt32(out var sourceLength)
              || sourceLength < 0
              || !originSource.TryGetProperty("sha256", out var sourceSha256)
              || sourceSha256.ValueKind != JsonValueKind.String)
            {
              throw new MalformedMetadataException("The manifest source provenance is malformed.");
            }
            sourceProvenance = new MetadataSourceProvenance(
              sourceLength,
              DecodeSha256(sourceSha256.GetString()!));
          }
          envelopePayload = assetPayload;
        }
        var partitions = new List<MetadataPartition>();
        if (envelopePayload.TryGetProperty("partitions", out var partitionArray))
        {
          foreach (var partition in partitionArray.EnumerateArray())
          {
            partitions.Add(new MetadataPartition(
              partition.GetProperty("localId").GetInt32(),
              partition.GetProperty("sha256").GetString()
                ?? throw new MalformedMetadataException("Missing partition fingerprint.")));
          }
        }

        var staticRenderObjectLocalIds = ReadIntegerArray(envelopePayload, "staticRenderObjectLocalIds");
        var sourceObjectLocalIds = ReadIntegerArray(envelopePayload, "sourceObjectLocalIds");
        var staticRenderObjectInventory = ReadIntegerArray(envelopePayload, "staticRenderObjectInventory");
        var sourceObjectInventory = ReadIntegerArray(envelopePayload, "sourceObjectInventory");
        AnimationClassBytes? animationLengths = null;
        AnimationClassBytes? animationFrameIndices = null;
        var animationClasses = new List<MetadataAnimationClass>();
        MetadataAnimationProjection? animationProjection = null;
        if (envelopePayload.TryGetProperty("staticAnimation", out var staticAnimation))
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
              ReadBase64(staticAnimation, "scaleFrames", profile.MaxMetadataBytes),
              ReadBase64(staticAnimation, "translationFrames", profile.MaxMetadataBytes),
              ReadBase64(staticAnimation, "matrices", profile.MaxMetadataBytes));
            if (status is not ("native" or "metadataOnly" or "absent"))
            {
              throw new MalformedMetadataException("Unsupported static animation status.");
            }
          }
        }

        var scopeInventory = ReadScopeInventory(payload);
        var scopeNextIds = ReadScopeNextIds(payload);
        var unknownMembers = ReadUnknownMembers(root, kind, remainingUnknownMembers);
        var hasProjection = guardProjections.TryGetValue(
          "nativeProjection",
          out var nativeProjection);
        var domainGuards = new Dictionary<string, string>(guards, StringComparer.Ordinal);
        domainGuards.Remove("nativeProjection");
        var sourceMshValue = envelopePayload.TryGetProperty("sourceMsh", out var sourceMsh)
          ? sourceMsh.GetString()
          : null;
        if (sourceMshValue is not null)
        {
          ValidateBase64Length(sourceMshValue, profile.MaxMetadataBytes);
        }
        return new MetadataEnvelope(
          lineage,
          documentId,
          kind,
          localId,
          sourceMshValue,
          hasProjection ? guards["nativeProjection"] : null,
          hasProjection ? nativeProjection.Projection : null,
          hasProjection ? nativeProjection.Version : null,
          partitions.AsReadOnly(),
          staticRenderObjectLocalIds,
          sourceObjectLocalIds,
          staticRenderObjectInventory,
          sourceObjectInventory,
          envelopePayload.TryGetProperty("nextStaticRenderObjectLocalId", out var nextRenderObjectId)
            ? nextRenderObjectId.GetInt32()
            : null,
          envelopePayload.TryGetProperty("nextSourceObjectLocalId", out var nextSourceObjectId)
            ? nextSourceObjectId.GetInt32()
            : null,
          envelopePayload.TryGetProperty("textureBinding", out var textureBinding)
            ? DecodeBase64Url(
              textureBinding.GetString()
                ?? throw new MalformedMetadataException("Missing TEX resource binding."),
              profile.MaxMetadataBytes)
            : null,
          animationLengths,
          animationFrameIndices,
          animationClasses.AsReadOnly(),
          animationProjection,
          envelopePayload.TryGetProperty("attachment", out var attachment)
            ? attachment.GetProperty("physicalNumber").GetInt32()
            : null,
          envelopePayload.TryGetProperty("attachment", out attachment)
            ? ReadBase64(attachment, "record", profile.MaxMetadataBytes)
            : null,
          envelopePayload.TryGetProperty("cannon", out var cannon)
            ? cannon.GetProperty("physicalNumber").GetInt32()
            : null,
          envelopePayload.TryGetProperty("cannon", out cannon)
            ? ReadBase64(cannon, "attachmentRecord", profile.MaxMetadataBytes)
            : null,
          envelopePayload.TryGetProperty("cannon", out cannon)
            ? ReadBase64(cannon, "renderPositionRecord", profile.MaxMetadataBytes)
            : null,
          envelopePayload.TryGetProperty("staticLight", out var staticLight)
            ? staticLight.GetProperty("type").GetString()
            : envelopePayload.TryGetProperty("staticLightInstance", out var staticLightInstance)
              ? staticLightInstance.GetProperty("type").GetString()
              : null,
          envelopePayload.TryGetProperty("staticLight", out staticLight)
            ? staticLight.GetProperty("physicalNumber").GetInt32()
            : envelopePayload.TryGetProperty("staticLightInstance", out staticLightInstance)
              ? staticLightInstance.GetProperty("physicalNumber").GetInt32()
              : null,
          envelopePayload.TryGetProperty("staticLightInstance", out staticLightInstance)
            ? staticLightInstance.GetProperty("definitionLocalId").GetInt32()
            : null,
          envelopePayload.TryGetProperty("staticLight", out staticLight)
            ? ReadBase64(staticLight, "record", profile.MaxMetadataBytes)
            : null,
          envelopePayload.TryGetProperty("staticLightInstance", out staticLightInstance)
            ? ReadBase64(staticLightInstance, "attachmentRecord", profile.MaxMetadataBytes)
            : null,
          new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(domainGuards),
          new System.Collections.ObjectModel.ReadOnlyDictionary<string, (string Projection, int Version)>(
            guardProjections),
          scopeInventory,
          scopeNextIds,
          sourceProvenance,
          unknownMembers,
          elementCount);
      }
      catch (Exception ex) when (ex is UnsupportedMetadataVersionException
        || ex is MalformedMetadataException
        || ex is MetadataConflictException
        || ex is ResourceLimitException)
      {
        throw;
      }
      catch (Exception ex) when (ex is JsonException
        || ex is InvalidOperationException
        || ex is KeyNotFoundException
        || ex is FormatException
        || ex is OverflowException)
      {
        throw new MalformedMetadataException("Malformed EarthTool metadata.", ex);
      }
    }

    private static int ValidateJsonLimits(
      ReadOnlySpan<byte> json,
      int maximumDepth,
      int maximumElements)
    {
      var reader = new Utf8JsonReader(json, new JsonReaderOptions
      {
        MaxDepth = int.MaxValue,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
      });
      var elements = 0;
      while (reader.Read())
      {
        if (reader.CurrentDepth > maximumDepth)
        {
          throw new ResourceLimitException(reader.CurrentDepth, maximumDepth);
        }
        if (reader.TokenType != JsonTokenType.PropertyName
          && reader.TokenType is not (JsonTokenType.EndObject or JsonTokenType.EndArray)
          && ++elements > maximumElements)
        {
          throw new ResourceLimitException(elements, maximumElements);
        }
      }
      return elements;
    }

    private static void ValidateNoDuplicateMembers(JsonElement root)
    {
      var pending = new Stack<JsonElement>();
      pending.Push(root);
      while (pending.Count > 0)
      {
        var current = pending.Pop();
        if (current.ValueKind == JsonValueKind.Object)
        {
          var names = new HashSet<string>(StringComparer.Ordinal);
          foreach (var property in current.EnumerateObject())
          {
            if (!names.Add(property.Name))
            {
              throw new MalformedMetadataException("Duplicate JSON member names are not allowed.");
            }
            pending.Push(property.Value);
          }
        }
        else if (current.ValueKind == JsonValueKind.Array)
        {
          foreach (var item in current.EnumerateArray())
          {
            pending.Push(item);
          }
        }
      }
    }

    private static string ReadRequiredString(JsonElement owner, string name)
    {
      var value = owner.GetProperty(name);
      return value.ValueKind == JsonValueKind.String && value.GetString() is string text
        ? text
        : throw new MalformedMetadataException($"The {name} member must be a string.");
    }

    private static Guid ReadVersion4Guid(JsonElement owner, string name)
    {
      var text = ReadRequiredString(owner, name);
      if (!Guid.TryParseExact(text, "D", out var value)
        || value.ToString("D") != text
        || !GltfMetadataIdentity.IsVersion4(value))
      {
        throw new MalformedMetadataException($"The {name} member must be a lowercase version-4 UUID.");
      }
      return value;
    }

    private static string DecodeSha256(string value)
    {
      if (value.Length != 43 || value.Any(character => !((character >= 'a' && character <= 'z')
        || (character >= 'A' && character <= 'Z')
        || (character >= '0' && character <= '9')
        || character is '-' or '_')))
      {
        throw new MalformedMetadataException("A SHA-256 digest must use unpadded base64url.");
      }
      var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "=");
      if (bytes.Length != 32)
      {
        throw new MalformedMetadataException("A SHA-256 digest must contain 32 bytes.");
      }
      return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<int>> ReadScopeInventory(JsonElement payload)
    {
      if (!payload.TryGetProperty("inventory", out var inventory))
      {
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyList<int>>(
          new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal));
      }
      var result = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
      foreach (var kind in new[] { "object", "mesh", "material", "light" })
      {
        if (inventory.TryGetProperty(kind, out _))
        {
          result.Add(kind, ReadIntegerArray(inventory, kind));
        }
      }
      return new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyList<int>>(result);
    }

    private static IReadOnlyDictionary<string, int> ReadScopeNextIds(JsonElement payload)
    {
      if (!payload.TryGetProperty("nextIds", out var nextIds))
      {
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(
          new Dictionary<string, int>(StringComparer.Ordinal));
      }
      var result = new Dictionary<string, int>(StringComparer.Ordinal);
      foreach (var kind in new[] { "object", "mesh", "material", "light" })
      {
        if (nextIds.TryGetProperty(kind, out var nextId))
        {
          result.Add(kind, nextId.GetInt32());
        }
      }
      return new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(result);
    }

    private static IReadOnlyDictionary<string, string> ReadUnknownMembers(
      JsonElement root,
      string scopeKind,
      int maximum)
    {
      var result = new Dictionary<string, string>(StringComparer.Ordinal);
      CollectUnknownMembers(root, string.Empty, scopeKind, result, maximum);
      return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(result);
    }

    private static void CollectUnknownMembers(
      JsonElement element,
      string path,
      string scopeKind,
      IDictionary<string, string> result,
      int maximum)
    {
      if (element.ValueKind == JsonValueKind.Object)
      {
        foreach (var property in element.EnumerateObject())
        {
          var childPath = path + "/" + EscapeJsonPointerSegment(property.Name);
          if (!IsKnownMetadataMember(scopeKind, childPath))
          {
            if (result.Count >= maximum)
            {
              throw new ResourceLimitException(result.Count + 1L, maximum);
            }
            result.Add(childPath, property.Value.GetRawText());
            continue;
          }
          CollectUnknownMembers(property.Value, childPath, scopeKind, result, maximum);
        }
      }
      else if (element.ValueKind == JsonValueKind.Array)
      {
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
          CollectUnknownMembers(item, path + "/" + index++, scopeKind, result, maximum);
        }
      }
    }

    private static bool IsKnownMetadataMember(string scopeKind, string path)
    {
      if (path is "/format" or "/version" or "/kind" or "/lineage" or "/document" or "/id"
        or "/guards" or "/payload")
      {
        return true;
      }

      var segments = path.Split('/');
      if (segments.Length == 3 && segments[1] == "guards"
        && IsKnownGuard(scopeKind, segments[2]))
      {
        return true;
      }
      if (segments.Length == 4 && segments[1] == "guards"
        && IsKnownGuard(scopeKind, segments[2])
        && segments[3] is "projection" or "version" or "algorithm" or "digest")
      {
        return true;
      }

      return scopeKind switch
      {
        "manifest" => IsKnownManifestMember(segments),
        "mesh" => path == "/payload/partitions"
          || segments.Length == 5 && segments[1] == "payload" && segments[2] == "partitions"
            && int.TryParse(segments[3], out _) && segments[4] is "localId" or "sha256",
        "material" => path == "/payload/textureBinding",
        "object" => IsKnownObjectMember(path),
        "light" => path == "/payload/staticLight"
          || path is "/payload/staticLight/type" or "/payload/staticLight/physicalNumber"
            or "/payload/staticLight/record",
        _ => false
      };
    }

    internal static bool IsSupportedUnknownMetadataPath(string scopeKind, string path)
    {
      if (path.Length < 2 || path[0] != '/' || path.EndsWith("/", StringComparison.Ordinal))
      {
        return false;
      }
      for (var index = 0; index < path.Length; index++)
      {
        if (path[index] == '~'
          && (index + 1 >= path.Length || path[++index] is not ('0' or '1')))
        {
          return false;
        }
      }
      if (IsKnownMetadataMember(scopeKind, path))
      {
        return false;
      }

      var separator = path.LastIndexOf('/');
      var parent = path.Substring(0, separator);
      if (parent.Length == 0 || parent == "/payload" || parent == "/guards")
      {
        return true;
      }
      var segments = parent.Split('/');
      if (segments.Length == 3 && segments[1] == "guards"
        && IsKnownGuard(scopeKind, segments[2]))
      {
        return true;
      }
      if (scopeKind == "manifest")
      {
        return parent is "/payload/origin" or "/payload/asset" or "/payload/inventory"
          or "/payload/nextIds" or "/payload/origin/source" or "/payload/asset/staticAnimation"
          || segments.Length == 6 && segments[1] == "payload" && segments[2] == "asset"
            && segments[3] == "staticAnimation" && segments[4] == "classes"
            && IsCanonicalArrayIndex(segments[5]);
      }
      if (scopeKind == "mesh")
      {
        return segments.Length == 4 && segments[1] == "payload" && segments[2] == "partitions"
          && IsCanonicalArrayIndex(segments[3]);
      }
      if (scopeKind == "object")
      {
        return parent is "/payload/staticAnimation" or "/payload/attachment"
          or "/payload/cannon" or "/payload/staticLightInstance";
      }
      return scopeKind == "light" && parent == "/payload/staticLight";
    }

    private static bool IsCanonicalArrayIndex(string value)
    {
      return int.TryParse(
          value,
          System.Globalization.NumberStyles.None,
          System.Globalization.CultureInfo.InvariantCulture,
          out var index)
        && index >= 0
        && value == index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool IsKnownGuard(string scopeKind, string name)
    {
      return scopeKind is "mesh" or "object" && name == "nativeProjection"
        || scopeKind == "object" && name is "cannon.position" or "cannon.direction"
        || scopeKind == "light" && name is "staticLight.pose" or "staticLight.type"
          or "staticLight.color" or "staticLight.intensity" or "staticLight.direction"
          or "staticLight.cones";
    }

    private static bool IsKnownManifestMember(IReadOnlyList<string> segments)
    {
      var path = string.Join("/", segments);
      if (path is "/payload/origin" or "/payload/asset" or "/payload/inventory" or "/payload/nextIds"
        or "/payload/origin/kind" or "/payload/origin/source" or "/payload/origin/parentLineage"
        or "/payload/origin/parentDocument" or "/payload/origin/source/byteLength"
        or "/payload/origin/source/sha256")
      {
        return true;
      }
      if (segments.Count == 4 && segments[1] == "payload"
        && segments[2] is "inventory" or "nextIds"
        && segments[3] is "object" or "mesh" or "material" or "light")
      {
        return true;
      }
      if (segments.Count == 4 && segments[1] == "payload" && segments[2] == "asset"
        && segments[3] is "sourceMsh" or "staticRenderObjectLocalIds" or "sourceObjectLocalIds"
          or "staticRenderObjectInventory" or "sourceObjectInventory" or "nextStaticRenderObjectLocalId"
          or "nextSourceObjectLocalId" or "staticAnimation")
      {
        return true;
      }
      if (segments.Count == 5 && segments[1] == "payload" && segments[2] == "asset"
        && segments[3] == "staticAnimation" && segments[4] is "lengths" or "frameIndices" or "classes")
      {
        return true;
      }
      return segments.Count == 7 && segments[1] == "payload" && segments[2] == "asset"
        && segments[3] == "staticAnimation" && segments[4] == "classes"
        && int.TryParse(segments[5], out _) && segments[6] is "class" or "objects" or "nativeObjects" or "sha256";
    }

    private static bool IsKnownObjectMember(string path)
    {
      if (path is "/payload/staticAnimation" or "/payload/attachment"
        or "/payload/cannon" or "/payload/staticLightInstance")
      {
        return true;
      }
      if (path.StartsWith("/payload/staticAnimation/", StringComparison.Ordinal))
      {
        return path.Substring("/payload/staticAnimation/".Length) is "animationClassValue" or "class"
          or "declaredLength" or "status" or "scaleFrames" or "translationFrames" or "matrices" or "sha256";
      }
      if (path.StartsWith("/payload/attachment/", StringComparison.Ordinal))
      {
        var name = path.Substring(path.LastIndexOf('/') + 1);
        return name is "physicalNumber" or "record";
      }
      if (path.StartsWith("/payload/cannon/", StringComparison.Ordinal))
      {
        var name = path.Substring(path.LastIndexOf('/') + 1);
        return name is "physicalNumber" or "attachmentRecord" or "renderPositionRecord";
      }
      if (path.StartsWith("/payload/staticLightInstance/", StringComparison.Ordinal))
      {
        var name = path.Substring(path.LastIndexOf('/') + 1);
        return name is "type" or "physicalNumber" or "definitionLocalId" or "attachmentRecord";
      }
      return false;
    }

    private static string EscapeJsonPointerSegment(string value)
    {
      return value.Replace("~", "~0").Replace("/", "~1");
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

    private static IReadOnlyList<byte> ReadBase64(
      JsonElement owner,
      string propertyName,
      int maximumDecodedBytes)
    {
      return DecodeBase64Url(
        owner.GetProperty(propertyName).GetString()
          ?? throw new MalformedMetadataException($"Missing {propertyName} animation data."),
        maximumDecodedBytes);
    }

    internal static IReadOnlyList<byte> DecodeBase64Url(string value, int maximumDecodedBytes)
    {
      ValidateBase64Length(value, maximumDecodedBytes);
      var padded = value.Replace('-', '+').Replace('_', '/');
      padded += new string('=', (4 - (padded.Length % 4)) % 4);
      return Array.AsReadOnly(Convert.FromBase64String(padded));
    }

    private static void ValidateBase64Length(string value, int maximumDecodedBytes)
    {
      if (value.Length % 4 == 1
        || value.Any(character => !((character >= 'a' && character <= 'z')
          || (character >= 'A' && character <= 'Z')
          || (character >= '0' && character <= '9')
          || character is '-' or '_')))
      {
        throw new MalformedMetadataException("Opaque metadata must use unpadded base64url.");
      }
      var decodedLength = checked(value.Length * 6L / 8L);
      if (decodedLength > maximumDecodedBytes)
      {
        throw new ResourceLimitException(decodedLength, maximumDecodedBytes);
      }
    }

    internal static string EncodeBase64Url(IReadOnlyList<byte> value)
    {
      return Convert.ToBase64String(value.ToArray())
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
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
      IReadOnlyDictionary<string, string> unknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds,
      IReadOnlyDictionary<StaticRenderObjectId, PreviewLayout> previewLayouts,
      AnimationProjectionSet animations,
      IReadOnlyList<AnimationLayout> animationLayouts,
      string? bufferFileName,
      string? sourceBaseName)
    {
      var rootSourceObject = asset.RootSourceObject;
      var sources = StaticSourceObjectTraversal.Flatten(rootSourceObject).ToArray();
      var attachments = ProjectAttachments(asset);
      var cannons = ProjectCannons(asset);
      var staticLights = ProjectStaticLights(asset);
      var placementRootIndex = sources.Length
        + attachments.Count
        + cannons.Count
        + staticLights.Count;
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
            WriteExtras(writer, CreateStaticLightMetadata(baseline, light, unknownMetadata));
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
        WriteExtras(writer, manifest);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("nodes");
        foreach (var source in sources)
        {
          writer.WriteStartObject();
          writer.WriteString("name", sourceBaseName is null
            ? $"Source object {source.Id.Value}"
            : $"{sourceBaseName}_{source.Id.Value}");
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
          if (source.Children.Count > 0 || isRoot && (attachments.Count > 0
            || cannons.Count > 0
            || staticLights.Count > 0))
          {
            writer.WriteStartArray("children");
            foreach (var child in source.Children)
            {
              writer.WriteNumberValue(nodeIndices[child.Id]);
            }
            if (isRoot)
            {
              var helperIndex = sources.Length;
              for (var index = 0;
                index < attachments.Count + cannons.Count + staticLights.Count;
                index++)
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
            unknownMetadata,
            metadataNextIds,
            animationProjection: animations.Objects.SingleOrDefault(item =>
              item.SourceObjectLocalId == source.Id.Value)));
          writer.WriteEndObject();
        }
        foreach (var attachment in attachments)
        {
          writer.WriteStartObject();
          writer.WriteString("name", GetAttachmentHelperName(attachment.PhysicalNumber));
          WriteTransform(writer, attachment.Translation, attachment.Rotation);
          WriteExtras(writer, CreateAttachmentMetadata(baseline, attachment, unknownMetadata));
          writer.WriteEndObject();
        }
        foreach (var cannon in cannons)
        {
          writer.WriteStartObject();
          writer.WriteString("name", GetCannonHelperName(cannon.PhysicalNumber));
          WriteTransform(writer, cannon.Translation, cannon.Rotation);
          WriteExtras(writer, CreateCannonMetadata(baseline, cannon, unknownMetadata));
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
          WriteExtras(writer, CreateStaticLightInstanceMetadata(baseline, light, unknownMetadata));
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
        foreach (var source in sources)
        {
          writer.WriteStartObject();
          writer.WriteString("name", sourceBaseName is null
            ? $"Static mesh {source.Id.Value}"
            : $"{sourceBaseName}_{source.Id.Value}_Mesh");
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
            layouts,
            unknownMetadata));
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
          WriteExtras(writer, CreateMaterialMetadata(baseline, renderObject, unknownMetadata));
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
      IReadOnlyDictionary<string, string> unknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds,
      StaticMeshAsset? sourceAsset = null,
      AnimationProjectionSet? animations = null,
      ProjectedAnimationObject? animationProjection = null)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, scopeKind, localId);
        WriteUnknownMetadata(writer, unknownMetadata, scopeKind, localId, false);
        writer.WriteStartObject("guards");
        if (fingerprint is not null)
        {
          WriteGuard(
            writer,
            "nativeProjection",
            "static-geometry",
            1,
            fingerprint,
            unknownMetadata,
            scopeKind,
            localId);
        }
        WriteUnknownMetadata(writer, unknownMetadata, scopeKind, localId, "/guards/");
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        if (sourceAsset is not null)
        {
          var serializedSource = sourceAsset.GetSerializedRepresentation().ToArray();
          writer.WriteStartObject("origin");
          writer.WriteString("kind", "mshExport");
          writer.WriteStartObject("source");
          writer.WriteNumber("byteLength", serializedSource.Length);
          using (var sha256 = SHA256.Create())
          {
            writer.WriteString("sha256", EncodeBase64Url(sha256.ComputeHash(serializedSource)));
          }
          WriteUnknownMetadata(
            writer,
            unknownMetadata,
            scopeKind,
            localId,
            "/payload/origin/source/");
          writer.WriteEndObject();
          WriteUnknownMetadata(writer, unknownMetadata, scopeKind, localId, "/payload/origin/");
          writer.WriteEndObject();
          writer.WriteStartObject("asset");
          if (sourceMsh is not null)
          {
            writer.WriteString("sourceMsh", sourceMsh);
          }
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
            var animationClassIndex = 0;
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
              WriteUnknownMetadata(
                writer,
                unknownMetadata,
                scopeKind,
                localId,
                $"/payload/asset/staticAnimation/classes/{animationClassIndex++}/");
              writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteUnknownMetadata(
              writer,
              unknownMetadata,
              scopeKind,
              localId,
              "/payload/asset/staticAnimation/");
            writer.WriteEndObject();
          }

          WriteUnknownMetadata(writer, unknownMetadata, scopeKind, localId, "/payload/asset/");
          writer.WriteEndObject();
          WriteScopeInventory(writer, sourceAsset, metadataNextIds, unknownMetadata);
        }
        else if (sourceMsh is not null)
        {
          writer.WriteString("sourceMsh", sourceMsh);
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
            EncodeBase64Url(StaticAnimationProjection.SerializeScaleFrames(
              animationProjection.SourceTracks)));
          writer.WriteString(
            "translationFrames",
            EncodeBase64Url(StaticAnimationProjection.SerializeTranslationFrames(
              animationProjection.SourceTracks)));
          writer.WriteString(
            "matrices",
            EncodeBase64Url(StaticAnimationProjection.SerializeMatrices(
              animationProjection.SourceTracks)));
          if (animationProjection.Fingerprint is not null)
          {
            writer.WriteString("sha256", animationProjection.Fingerprint);
          }
          WriteUnknownMetadata(
            writer,
            unknownMetadata,
            scopeKind,
            localId,
            "/payload/staticAnimation/");
          writer.WriteEndObject();
        }

        WriteUnknownMetadata(writer, unknownMetadata, scopeKind, localId, true);
        writer.WriteEndObject();
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
      IReadOnlyDictionary<StaticRenderObjectId, PartitionLayout> layouts,
      IReadOnlyDictionary<string, string> unknownMetadata)
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
        WriteMetadataStart(writer, baseline, "mesh", source.Id.Value);
        WriteUnknownMetadata(writer, unknownMetadata, "mesh", source.Id.Value, false);
        writer.WriteStartObject("guards");
        WriteGuard(
          writer,
          "nativeProjection",
          "static-geometry",
          1,
          fingerprint.Sha256,
          unknownMetadata,
          "mesh",
          source.Id.Value);
        WriteUnknownMetadata(writer, unknownMetadata, "mesh", source.Id.Value, "/guards/");
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteStartArray("partitions");
        var partitionIndex = 0;
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
          WriteUnknownMetadata(
            writer,
            unknownMetadata,
            "mesh",
            source.Id.Value,
            $"/payload/partitions/{partitionIndex++}/");
          writer.WriteEndObject();
        }

        writer.WriteEndArray();
        WriteUnknownMetadata(writer, unknownMetadata, "mesh", source.Id.Value, true);
        writer.WriteEndObject();
        writer.WriteEndObject();
      }

      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateMaterialMetadata(
      InterchangeBaseline baseline,
      StaticRenderObject renderObject,
      IReadOnlyDictionary<string, string> unknownMetadata)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, "material", renderObject.LocalId);
        WriteUnknownMetadata(writer, unknownMetadata, "material", renderObject.LocalId, false);
        writer.WriteStartObject("guards");
        WriteUnknownMetadata(writer, unknownMetadata, "material", renderObject.LocalId, "/guards/");
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteString(
          "textureBinding",
          EncodeBase64Url(renderObject.TexturePathBytes.ToArray()));
        WriteUnknownMetadata(writer, unknownMetadata, "material", renderObject.LocalId, true);
        writer.WriteEndObject();
        writer.WriteEndObject();
      }

      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateAttachmentMetadata(
      InterchangeBaseline baseline,
      ProjectedAttachment attachment,
      IReadOnlyDictionary<string, string> unknownMetadata)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, "object", attachment.LocalId);
        WriteUnknownMetadata(writer, unknownMetadata, "object", attachment.LocalId, false);
        writer.WriteStartObject("guards");
        WriteGuard(writer, "nativeProjection", "attachment.pose", 1, CreateAttachmentPoseFingerprint(
          baseline,
          attachment.LocalId,
          attachment.PhysicalNumber,
          attachment.Record), unknownMetadata, "object", attachment.LocalId);
        WriteUnknownMetadata(writer, unknownMetadata, "object", attachment.LocalId, "/guards/");
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteStartObject("attachment");
        writer.WriteNumber("physicalNumber", attachment.PhysicalNumber);
        writer.WriteString("record", EncodeBase64Url(attachment.Record));
        WriteUnknownMetadata(
          writer,
          unknownMetadata,
          "object",
          attachment.LocalId,
          "/payload/attachment/");
        writer.WriteEndObject();
        WriteUnknownMetadata(writer, unknownMetadata, "object", attachment.LocalId, true);
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateCannonMetadata(
      InterchangeBaseline baseline,
      ProjectedCannon cannon,
      IReadOnlyDictionary<string, string> unknownMetadata)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, "object", cannon.LocalId);
        WriteUnknownMetadata(writer, unknownMetadata, "object", cannon.LocalId, false);
        writer.WriteStartObject("guards");
        foreach (var guard in CreateCannonGuards(baseline, cannon))
        {
          WriteGuard(
            writer,
            guard.Key,
            guard.Key,
            1,
            guard.Value,
            unknownMetadata,
            "object",
            cannon.LocalId);
        }
        WriteUnknownMetadata(writer, unknownMetadata, "object", cannon.LocalId, "/guards/");
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteStartObject("cannon");
        writer.WriteNumber("physicalNumber", cannon.PhysicalNumber);
        writer.WriteString("attachmentRecord", EncodeBase64Url(cannon.AttachmentRecord));
        writer.WriteString("renderPositionRecord", EncodeBase64Url(cannon.RenderPositionRecord));
        WriteUnknownMetadata(
          writer,
          unknownMetadata,
          "object",
          cannon.LocalId,
          "/payload/cannon/");
        writer.WriteEndObject();
        WriteUnknownMetadata(writer, unknownMetadata, "object", cannon.LocalId, true);
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateStaticLightInstanceMetadata(
      InterchangeBaseline baseline,
      ProjectedStaticLight light,
      IReadOnlyDictionary<string, string> unknownMetadata)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, "object", light.InstanceLocalId);
        WriteUnknownMetadata(writer, unknownMetadata, "object", light.InstanceLocalId, false);
        writer.WriteStartObject("guards");
        WriteUnknownMetadata(writer, unknownMetadata, "object", light.InstanceLocalId, "/guards/");
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteStartObject("staticLightInstance");
        writer.WriteString("type", light.Type);
        writer.WriteNumber("physicalNumber", light.PhysicalNumber);
        writer.WriteNumber("definitionLocalId", light.LocalId);
        writer.WriteString("attachmentRecord", EncodeBase64Url(light.AttachmentRecord));
        WriteUnknownMetadata(
          writer,
          unknownMetadata,
          "object",
          light.InstanceLocalId,
          "/payload/staticLightInstance/");
        writer.WriteEndObject();
        WriteUnknownMetadata(writer, unknownMetadata, "object", light.InstanceLocalId, true);
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateStaticLightMetadata(
      InterchangeBaseline baseline,
      ProjectedStaticLight light,
      IReadOnlyDictionary<string, string> unknownMetadata)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, "light", light.LocalId);
        WriteUnknownMetadata(writer, unknownMetadata, "light", light.LocalId, false);
        writer.WriteStartObject("guards");
        foreach (var guard in CreateStaticLightGuards(baseline, light))
        {
          WriteGuard(
            writer,
            guard.Key,
            guard.Key,
            1,
            guard.Value,
            unknownMetadata,
            "light",
            light.LocalId);
        }
        WriteUnknownMetadata(writer, unknownMetadata, "light", light.LocalId, "/guards/");
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteStartObject("staticLight");
        writer.WriteString("type", light.Type);
        writer.WriteNumber("physicalNumber", light.PhysicalNumber);
        writer.WriteString("record", EncodeBase64Url(light.Record));
        WriteUnknownMetadata(
          writer,
          unknownMetadata,
          "light",
          light.LocalId,
          "/payload/staticLight/");
        writer.WriteEndObject();
        WriteUnknownMetadata(writer, unknownMetadata, "light", light.LocalId, true);
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static IReadOnlyDictionary<string, string> CreateStaticLightGuards(
      InterchangeBaseline baseline,
      string type,
      int physicalNumber,
      int localId,
      byte[] record,
      byte[] attachmentRecord)
    {
      return CreateStaticLightGuards(
        baseline,
        ProjectStaticLight(type, physicalNumber, localId, 1, record, attachmentRecord));
    }

    private static IReadOnlyDictionary<string, string> CreateStaticLightGuards(
      InterchangeBaseline baseline,
      ProjectedStaticLight light)
    {
      return new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["staticLight.pose"] = CreateStaticLightFingerprint(
          baseline,
          light.LocalId,
          "staticLight.pose",
          writer => WriteCanonicalVector(writer, light.Translation)),
        ["staticLight.type"] = CreateStaticLightFingerprint(
          baseline,
          light.LocalId,
          "staticLight.type",
          writer => WriteFingerprintString(writer, light.Type)),
        ["staticLight.color"] = CreateStaticLightFingerprint(
          baseline,
          light.LocalId,
          "staticLight.color",
          writer => WriteCanonicalVector(writer, light.Color)),
        ["staticLight.intensity"] = CreateStaticLightFingerprint(
          baseline,
          light.LocalId,
          "staticLight.intensity",
          writer => WriteStaticLightGuardFloat(writer, light.Intensity)),
        ["staticLight.direction"] = CreateStaticLightFingerprint(
          baseline,
          light.LocalId,
          "staticLight.direction",
          writer => WriteCanonicalDirection(writer, RoundTripStaticLightDirection(light.Rotation))),
        ["staticLight.cones"] = CreateStaticLightFingerprint(
          baseline,
          light.LocalId,
          "staticLight.cones",
          writer =>
          {
            WriteStaticLightGuardFloat(writer, light.InnerConeAngle);
            WriteStaticLightGuardFloat(writer, light.OuterConeAngle);
          })
      };
    }

    internal static string CreateStaticLightFingerprint(
      InterchangeBaseline baseline,
      int localId,
      string projection,
      Action<BinaryWriter> writeProjection)
    {
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        WriteFingerprintHeader(writer, baseline, projection, "light", localId);
        writeProjection(writer);
      }
      return Hash(preimage.ToArray());
    }

    private static void WriteCanonicalVector(BinaryWriter writer, Vector3 value)
    {
      WriteStaticLightGuardFloat(writer, value.X);
      WriteStaticLightGuardFloat(writer, value.Y);
      WriteStaticLightGuardFloat(writer, value.Z);
    }

    private static void WriteCanonicalDirection(BinaryWriter writer, Vector3 value)
    {
      WriteStaticLightGuardFloat(writer, value.X);
      WriteStaticLightGuardFloat(writer, value.Y);
      WriteStaticLightGuardFloat(writer, value.Z);
    }

    private static Vector3 RoundTripStaticLightDirection(Quaternion rotation)
    {
      var transform = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(rotation));
      Matrix4x4.Decompose(transform, out _, out var roundTrippedRotation, out _);
      return Vector3.Transform(-Vector3.UnitZ, roundTrippedRotation);
    }

    private static void WriteStaticLightGuardFloat(BinaryWriter writer, float value)
    {
      WriteCanonicalFloat(writer, MathF.Round(value, 5));
    }

    internal static string CreateAttachmentPoseFingerprint(
      InterchangeBaseline baseline,
      int localId,
      int physicalNumber,
      IReadOnlyList<byte> record)
    {
      var bytes = record.ToArray();
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        WriteFingerprintHeader(writer, baseline, "attachment.pose", "object", localId);
        writer.Write(physicalNumber);
        writer.Write(BinaryPrimitives.ReadInt16LittleEndian(bytes));
        writer.Write(BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(2)));
        writer.Write(BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(4)));
        writer.Write(bytes[6]);
      }
      return Hash(preimage.ToArray());
    }

    internal static IReadOnlyDictionary<string, string> CreateCannonGuards(
      InterchangeBaseline baseline,
      int localId,
      int physicalNumber,
      IReadOnlyList<byte> attachmentRecord,
      IReadOnlyList<byte> renderPositionRecord)
    {
      return new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["cannon.position"] = CreateCannonPositionFingerprint(
          baseline,
          localId,
          physicalNumber,
          renderPositionRecord),
        ["cannon.direction"] = CreateCannonDirectionFingerprint(
          baseline,
          localId,
          physicalNumber,
          attachmentRecord)
      };
    }

    private static IReadOnlyDictionary<string, string> CreateCannonGuards(
      InterchangeBaseline baseline,
      ProjectedCannon cannon)
    {
      return CreateCannonGuards(
        baseline,
        cannon.LocalId,
        cannon.PhysicalNumber,
        cannon.AttachmentRecord,
        cannon.RenderPositionRecord);
    }

    internal static string CreateCannonPositionFingerprint(
      InterchangeBaseline baseline,
      int localId,
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
          "cannon.position",
          "object",
          localId);
        writer.Write(physicalNumber);
        WriteCanonicalFloat(writer, ReadFinitePreview(bytes, 0));
        WriteCanonicalFloat(writer, ReadFinitePreview(bytes, 8));
        WriteCanonicalFloat(writer, ReadFinitePreview(bytes, 4));
      }
      return Hash(preimage.ToArray());
    }

    internal static string CreateCannonDirectionFingerprint(
      InterchangeBaseline baseline,
      int localId,
      int physicalNumber,
      IReadOnlyList<byte> attachmentRecord)
    {
      var bytes = attachmentRecord.ToArray();
      using var preimage = new MemoryStream();
      using (var writer = new BinaryWriter(preimage, Encoding.UTF8, true))
      {
        WriteFingerprintHeader(writer, baseline, "cannon.direction", "object", localId);
        writer.Write(physicalNumber);
        writer.Write(bytes[6]);
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

    private static void WriteMetadataStart(
      Utf8JsonWriter writer,
      InterchangeBaseline baseline,
      string scopeKind,
      int localId)
    {
      writer.WriteStartObject();
      writer.WriteString("format", "earthtool.msh.gltf");
      writer.WriteNumber("version", 1);
      writer.WriteString("kind", scopeKind);
      writer.WriteString("lineage", baseline.AssetLineageId);
      writer.WriteString("document", baseline.DocumentId);
      writer.WriteNumber("id", localId);
    }

    private static void WriteUnknownMetadata(
      Utf8JsonWriter writer,
      IReadOnlyDictionary<string, string> unknownMetadata,
      string scopeKind,
      int localId,
      bool payload)
    {
      WriteUnknownMetadata(
        writer,
        unknownMetadata,
        scopeKind,
        localId,
        payload ? "/payload/" : "/");
    }

    private static void WriteUnknownMetadata(
      Utf8JsonWriter writer,
      IReadOnlyDictionary<string, string> unknownMetadata,
      string scopeKind,
      int localId,
      string section)
    {
      var prefix = $"{scopeKind}:{localId}:";
      foreach (var member in unknownMetadata.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal))
        .OrderBy(item => item.Key, StringComparer.Ordinal))
      {
        var name = member.Key.Substring(prefix.Length);
        if (!name.StartsWith(section, StringComparison.Ordinal))
        {
          continue;
        }
        name = name.Substring(section.Length);
        if (section == "/" && name.StartsWith("payload/", StringComparison.Ordinal))
        {
          continue;
        }
        if (name.Length == 0 || name.IndexOf('/') >= 0)
        {
          continue;
        }
        writer.WritePropertyName(name.Replace("~1", "/").Replace("~0", "~"));
        writer.WriteRawValue(member.Value, false);
      }
    }

    private static void WriteGuard(
      Utf8JsonWriter writer,
      string name,
      string projection,
      int version,
      string sha256,
      IReadOnlyDictionary<string, string> unknownMetadata,
      string scopeKind,
      int localId)
    {
      if (sha256.Length != 64)
      {
        throw new InvalidOperationException("A metadata guard requires a SHA-256 digest.");
      }
      var bytes = new byte[32];
      for (var index = 0; index < bytes.Length; index++)
      {
        bytes[index] = byte.Parse(
          sha256.Substring(index * 2, 2),
          System.Globalization.NumberStyles.HexNumber,
          System.Globalization.CultureInfo.InvariantCulture);
      }
      if (bytes.Length != 32)
      {
        throw new InvalidOperationException("A metadata guard requires a SHA-256 digest.");
      }
      var digest = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
      writer.WriteStartObject(name);
      writer.WriteString("projection", projection);
      writer.WriteNumber("version", version);
      writer.WriteString("algorithm", "sha256");
      writer.WriteString("digest", digest);
      WriteUnknownMetadata(
        writer,
        unknownMetadata,
        scopeKind,
        localId,
        $"/guards/{EscapeJsonPointerSegment(name)}/");
      writer.WriteEndObject();
    }

    private static void WriteScopeInventory(
      Utf8JsonWriter writer,
      StaticMeshAsset asset,
      IReadOnlyDictionary<string, int> metadataNextIds,
      IReadOnlyDictionary<string, string> unknownMetadata)
    {
      var objectIds = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .Select(source => source.Id.Value)
        .Concat(ProjectAttachments(asset).Select(attachment => attachment.LocalId))
        .Concat(ProjectCannons(asset).Select(cannon => cannon.LocalId))
        .Concat(ProjectStaticLights(asset).Select(light => light.InstanceLocalId))
        .OrderBy(id => id)
        .ToArray();
      var inventories = new Dictionary<string, int[]>(StringComparer.Ordinal)
      {
        ["object"] = objectIds,
        ["mesh"] = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
          .Select(source => source.Id.Value).OrderBy(id => id).ToArray(),
        ["material"] = asset.StaticRenderObjectSequence.Select(record => record.LocalId)
          .OrderBy(id => id).ToArray(),
        ["light"] = ProjectStaticLights(asset).Select(light => light.LocalId).OrderBy(id => id).ToArray()
      };
      writer.WriteStartObject("inventory");
      foreach (var kind in new[] { "object", "mesh", "material", "light" })
      {
        writer.WriteStartArray(kind);
        foreach (var id in inventories[kind])
        {
          writer.WriteNumberValue(id);
        }
        writer.WriteEndArray();
      }
      WriteUnknownMetadata(writer, unknownMetadata, "manifest", 0, "/payload/inventory/");
      writer.WriteEndObject();
      writer.WriteStartObject("nextIds");
      foreach (var kind in new[] { "object", "mesh", "material", "light" })
      {
        var ids = inventories[kind];
        var next = ids.Length == 0 ? 1 : checked(ids[^1] + 1);
        if (kind == "mesh" && asset.NextSourceObjectLocalId.HasValue)
        {
          next = Math.Max(next, asset.NextSourceObjectLocalId.Value);
        }
        else if (kind == "material" && asset.NextStaticRenderObjectLocalId.HasValue)
        {
          next = Math.Max(next, asset.NextStaticRenderObjectLocalId.Value);
        }
        else if (kind == "light")
        {
          next = Math.Max(next, 9);
        }
        if (metadataNextIds.TryGetValue(kind, out var preservedNext))
        {
          next = Math.Max(next, preservedNext);
        }
        writer.WriteNumber(kind, next);
      }
      WriteUnknownMetadata(writer, unknownMetadata, "manifest", 0, "/payload/nextIds/");
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
        || root.GetProperty("scenes").GetArrayLength() != 1)
      {
        if (intent == GltfImportIntent.Edit)
        {
          throw new MetadataConflictException(
            GltfDiagnosticCodes.InvalidSceneContract,
            2001,
            "scenes",
            "Edit import requires exactly one declared default scene.",
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RepairNativeExternally);
        }
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
          || texture.EnumerateObject().Any(property => property.Name is not ("source" or "name"))
          || !texture.TryGetProperty("source", out var source)
          || source.GetInt32() < 0
          || source.GetInt32() >= images.GetArrayLength())
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

    private static IReadOnlyList<ProjectedAttachment> ProjectAttachments(StaticMeshAsset asset)
    {
      var table = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var firstArtistObjectId = GetFirstArtistObjectLocalId(asset);
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
          GetAttachmentArtistObjectLocalId(firstArtistObjectId, physicalNumber),
          physicalNumber,
          record,
          new Vector3(x / 256f, z / 256f, storedNegativeY / 256f),
          rotation));
      }
      return result.AsReadOnly();
    }

    private static IReadOnlyList<ProjectedCannon> ProjectCannons(
      StaticMeshAsset asset)
    {
      var attachments = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var renderPositions = asset.CommonBaseHeader.CannonRenderPositions.ToArray();
      var firstArtistObjectId = GetFirstArtistObjectLocalId(asset);
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
          GetCannonArtistObjectLocalId(firstArtistObjectId, physicalNumber),
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
      var firstArtistObjectId = GetFirstArtistObjectLocalId(asset);
      var result = new List<ProjectedStaticLight>();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        var spotAttachment = attachments.AsSpan((physicalNumber + 11) * 8, 8).ToArray();
        if (BinaryPrimitives.ReadInt16LittleEndian(spotAttachment) != short.MinValue)
        {
          result.Add(ProjectStaticLight(
            "spot",
            physicalNumber,
            physicalNumber,
            GetStaticLightArtistObjectLocalId(firstArtistObjectId, physicalNumber),
            spots.AsSpan((physicalNumber - 1) * 0x30, 0x30).ToArray(),
            spotAttachment));
        }

        var omniAttachment = attachments.AsSpan((physicalNumber + 15) * 8, 8).ToArray();
        if (BinaryPrimitives.ReadInt16LittleEndian(omniAttachment) != short.MinValue)
        {
          result.Add(ProjectStaticLight(
            "point",
            physicalNumber,
            physicalNumber + 4,
            GetStaticLightArtistObjectLocalId(firstArtistObjectId, physicalNumber + 4),
            omnis.AsSpan((physicalNumber - 1) * 0x1C, 0x1C).ToArray(),
            omniAttachment));
        }
      }
      return result.AsReadOnly();
    }

    private static ProjectedStaticLight ProjectStaticLight(
      string type,
      int physicalNumber,
      int localId,
      int instanceLocalId,
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
        localId,
        instanceLocalId,
        record,
        attachmentRecord,
        position,
        rotation,
        color,
        intensity,
        inner,
        outer);
    }

    internal static int GetFirstArtistObjectLocalId(StaticMeshAsset asset)
    {
      var highest = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .Max(source => source.Id.Value);
      return Math.Max(asset.NextSourceObjectLocalId ?? checked(highest + 1), checked(highest + 1));
    }

    internal static int GetAttachmentArtistObjectLocalId(int firstArtistObjectId, int physicalNumber)
    {
      return checked(firstArtistObjectId + physicalNumber - 1);
    }

    internal static int GetAttachmentPhysicalNumber(int firstArtistObjectId, int localId)
    {
      return checked(localId - firstArtistObjectId + 1);
    }

    internal static int GetCannonArtistObjectLocalId(int firstArtistObjectId, int physicalNumber)
    {
      return checked(firstArtistObjectId + 49 + physicalNumber - 1);
    }

    internal static int GetStaticLightArtistObjectLocalId(int firstArtistObjectId, int definitionLocalId)
    {
      return checked(firstArtistObjectId + 53 + definitionLocalId - 1);
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
      var (range, localNumber) = physicalNumber switch
      {
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
      return $"ET_Cannon_{physicalNumber}_Attachment_{physicalNumber}";
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
        ? $"ET_SpotLight_{physicalNumber}_Attachment_{physicalNumber + 12}"
        : $"ET_OmniLight_{physicalNumber}_Attachment_{physicalNumber + 16}";
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
      internal int LocalId { get; }

      internal int PhysicalNumber { get; }
      internal byte[] Record { get; }
      internal Vector3 Translation { get; }
      internal Quaternion Rotation { get; }

      internal ProjectedAttachment(
        int localId,
        int physicalNumber,
        byte[] record,
        Vector3 translation,
        Quaternion rotation)
      {
        LocalId = localId;
        PhysicalNumber = physicalNumber;
        Record = record;
        Translation = translation;
        Rotation = rotation;
      }
    }

    private sealed class ProjectedCannon
    {
      internal int LocalId { get; }

      internal int PhysicalNumber { get; }
      internal byte[] AttachmentRecord { get; }
      internal byte[] RenderPositionRecord { get; }
      internal Vector3 Translation { get; }
      internal Quaternion Rotation { get; }

      internal ProjectedCannon(
        int localId,
        int physicalNumber,
        byte[] attachmentRecord,
        byte[] renderPositionRecord,
        Vector3 translation,
        Quaternion rotation)
      {
        LocalId = localId;
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
      internal int LocalId { get; }
      internal int InstanceLocalId { get; }
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
        int localId,
        int instanceLocalId,
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
        LocalId = localId;
        InstanceLocalId = instanceLocalId;
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
        if (builders.Count == 0)
        {
          continue;
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

  internal sealed class MetadataConflictException : Exception
  {
    internal string Code { get; }

    internal int EventId { get; }

    internal string Path { get; }

    internal IReadOnlyList<string> Actions { get; }

    internal IReadOnlyDictionary<string, string> ConflictData { get; }

    internal MetadataConflictException(
      string code,
      int eventId,
      string path,
      string message,
      params string[] actions)
      : this(code, eventId, path, message, null, actions)
    {
    }

    internal MetadataConflictException(
      string code,
      int eventId,
      string path,
      string message,
      IReadOnlyDictionary<string, string>? data,
      params string[] actions)
      : base(message)
    {
      Code = code;
      EventId = eventId;
      Path = path;
      Actions = Array.AsReadOnly(actions);
      ConflictData = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
        data?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<string, string>());
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
