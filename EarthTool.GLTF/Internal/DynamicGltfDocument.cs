#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Expert;
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
using System.Threading;

namespace EarthTool.GLTF.Internal
{
  internal sealed class DynamicGltfImport
  {
    internal DynamicMeshAsset Asset { get; }
    internal NativeProjectionFingerprint Fingerprint { get; }
    internal PreservationReport Preservation { get; }
    internal IReadOnlyList<int> ObjectIds { get; }

    internal DynamicGltfImport(
      DynamicMeshAsset asset,
      NativeProjectionFingerprint fingerprint,
      PreservationReport preservation,
      IReadOnlyList<int> objectIds)
    {
      Asset = asset;
      Fingerprint = fingerprint;
      Preservation = preservation;
      ObjectIds = objectIds;
    }
  }

  internal static class DynamicGltfDocument
  {
    internal const string ProjectionName = "dynamic-group-explosion-preview";
    internal const int ProjectionVersion = 1;
    private const int MetadataVersion = 2;
    private const int PreviewTotalLifetimeTicks = 100;
    private const int PreviewRemainingLifetimeTicks = 100;
    private const uint PreviewGlobalTick = 0;
    private const float PreviewTextureScale = 1;
    private const float PreviewLifetimeProgress = 0;
    private const float _previewParentPhase = 0;
    private static readonly Vector3 _previewWaterColor = new(0.2f, 0.45f, 0.7f);
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunkType = 0x4E4F534A;
    private const uint BinaryChunkType = 0x004E4942;

    internal static byte[] Create(
      DynamicMeshAsset asset,
      InterchangeBaseline baseline,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews,
      IReadOnlyList<int> objectIds,
      out NativeProjectionFingerprint fingerprint)
    {
      var package = CreatePackage(
        asset, baseline, profile, false, previews, meshPreviews, objectIds, out fingerprint);
      return Pack(package.Json, package.Binary);
    }

    internal static GltfPackage CreateSeparate(
      DynamicMeshAsset asset,
      InterchangeBaseline baseline,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews,
      IReadOnlyList<int> objectIds,
      out NativeProjectionFingerprint fingerprint)
    {
      return CreatePackage(
        asset, baseline, profile, true, previews, meshPreviews, objectIds, out fingerprint);
    }

    internal static DynamicGltfImport ImportGlb(
      byte[] glb,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      if (glb.Length > profile.MaxInputBytes)
      {
        throw new ResourceLimitException(glb.Length, profile.MaxInputBytes);
      }
      ValidateGlb(glb, profile);
      var jsonLength = checked((int)ReadUInt32(glb, 12));
      var binaryHeader = checked(20 + jsonLength);
      var binaryLength = checked((int)ReadUInt32(glb, binaryHeader));
      return Import(
        glb.AsMemory(20, jsonLength),
        glb.AsMemory(binaryHeader + 8, binaryLength),
        expectedBaseline,
        profile,
        cancellationToken);
    }

    internal static DynamicGltfImport ImportSeparate(
      byte[] json,
      byte[] binary,
      string bufferUri,
      IReadOnlyDictionary<string, byte[]> images,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      GlbDocument.ValidateSeparate(json, binary, bufferUri, images);
      return Import(json, binary, expectedBaseline, profile, cancellationToken);
    }

    internal static void ValidateGlb(byte[] glb, GltfOperationProfile profile)
    {
      if (glb.Length < 32
        || ReadUInt32(glb, 0) != GlbMagic
        || ReadUInt32(glb, 4) != 2
        || ReadUInt32(glb, 8) != glb.Length)
      {
        throw new InvalidDataException("Invalid dynamic GLB header.");
      }
      var jsonLength = checked((int)ReadUInt32(glb, 12));
      var binaryHeader = checked(20 + jsonLength);
      if (jsonLength <= 0
        || binaryHeader + 8 > glb.Length
        || ReadUInt32(glb, 16) != JsonChunkType
        || ReadUInt32(glb, binaryHeader + 4) != BinaryChunkType
        || binaryHeader + 8 + ReadUInt32(glb, binaryHeader) != glb.Length)
      {
        throw new InvalidDataException("Invalid dynamic GLB chunks.");
      }
      using var json = JsonDocument.Parse(
        glb.AsMemory(20, jsonLength),
        new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth });
      ValidateGraphBounds(json.RootElement, profile);
      ModelRoot.ParseGLB(
        new ArraySegment<byte>(glb),
        new ReadSettings { Validation = ValidationMode.Strict });
    }

    internal static void ValidateSeparatePackage(
      byte[] json,
      byte[] binary,
      string bufferUri,
      IReadOnlyDictionary<string, byte[]> images,
      GltfOperationProfile profile)
    {
      using var document = JsonDocument.Parse(
        json,
        new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth });
      ValidateGraphBounds(document.RootElement, profile);
      if (!HasDynamicManifest(json, profile.MaxJsonDepth))
      {
        throw new InvalidDataException("Required dynamic manifest metadata is absent.");
      }
      GlbDocument.ValidateSeparate(json, binary, bufferUri, images);
    }

    internal static bool HasDynamicManifest(ReadOnlyMemory<byte> jsonBytes, int maximumDepth)
    {
      try
      {
        using var json = JsonDocument.Parse(
          jsonBytes,
          new JsonDocumentOptions { MaxDepth = maximumDepth });
        var metadata = GetEarthToolMetadata(json.RootElement.GetProperty("scenes")[0], "scenes[0]");
        using var envelope = JsonDocument.Parse(
          metadata,
          new JsonDocumentOptions { MaxDepth = maximumDepth });
        var root = envelope.RootElement;
        return root.TryGetProperty("version", out var version)
          && version.GetInt32() == MetadataVersion
          && root.TryGetProperty("payload", out var payload)
          && payload.TryGetProperty("assetKind", out var assetKind)
          && assetKind.GetString() == "dynamic";
      }
      catch (Exception ex) when (ex is JsonException
        or InvalidOperationException
        or KeyNotFoundException
        or DynamicMetadataGraphException)
      {
        return false;
      }
    }

    private static GltfPackage CreatePackage(
      DynamicMeshAsset asset,
      InterchangeBaseline baseline,
      GltfOperationProfile profile,
      bool separate,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews,
      IReadOnlyList<int> objectIds,
      out NativeProjectionFingerprint fingerprint)
    {
      var objects = Flatten(asset.RootDynamicObject, profile, objectIds);
      ValidateSupportedEffects(objects);
      var effectPreviews = CreateEffectPreviews(objects, profile, meshPreviews);
      var binary = CreateBinary(objects, effectPreviews, out var layouts);
      var previewImages = objects
        .Where(scope => previews.ContainsKey(scope.Id))
        .Select(scope => previews[scope.Id])
        .GroupBy(preview => preview.ContentAddress, StringComparer.Ordinal)
        .Select(group => group.First())
        .ToArray();
      binary = AppendPreviewImages(binary, previewImages, out var previewLayouts);
      if (binary.Length == 0)
      {
        binary = new byte[4];
      }
      var bufferFileName = Hash(binary) + ".bin";
      fingerprint = CreateFingerprint(objects, effectPreviews);
      var manifest = CreateManifestMetadata(asset, baseline, objects, fingerprint);
      if (Encoding.UTF8.GetByteCount(manifest) > profile.MaxMetadataBytes)
      {
        throw new ResourceLimitException(Encoding.UTF8.GetByteCount(manifest), profile.MaxMetadataBytes);
      }
      var json = CreateJson(
        baseline,
        objects,
        layouts,
        binary.Length,
        separate ? bufferFileName : null,
        manifest,
        previews,
        previewImages,
        previewLayouts,
        effectPreviews);
      var outputLength = separate
        ? checked(json.Length + binary.Length)
        : checked(28 + ((json.Length + 3) & ~3) + ((binary.Length + 3) & ~3));
      if (outputLength > profile.MaxOutputBytes)
      {
        throw new ResourceLimitException(outputLength, profile.MaxOutputBytes);
      }
      return new GltfPackage(
        json,
        binary,
        separate ? bufferFileName : string.Empty,
        new Dictionary<string, byte[]>(StringComparer.Ordinal));
    }

    private static DynamicGltfImport Import(
      ReadOnlyMemory<byte> jsonBytes,
      ReadOnlyMemory<byte> binary,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      try
      {
        return ImportCore(jsonBytes, binary, expectedBaseline, profile, cancellationToken);
      }
      catch (Exception ex) when (ex is KeyNotFoundException
        or InvalidOperationException
        or FormatException
        or JsonException)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          "extras.earthtool",
          ex.Message);
      }
    }

    private static DynamicGltfImport ImportCore(
      ReadOnlyMemory<byte> jsonBytes,
      ReadOnlyMemory<byte> binary,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      using var json = JsonDocument.Parse(
        jsonBytes,
        new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth });
      var root = json.RootElement;
      ValidateGraphBounds(root, profile);
      var manifestText = GetEarthToolMetadata(root.GetProperty("scenes")[0], "scenes[0]");
      if (Encoding.UTF8.GetByteCount(manifestText) > profile.MaxMetadataBytes)
      {
        throw new ResourceLimitException(Encoding.UTF8.GetByteCount(manifestText), profile.MaxMetadataBytes);
      }
      ValidateMetadataBudgets(root, manifestText, profile);
      using var manifest = ParseMetadata(manifestText, profile);
      var manifestRoot = manifest.RootElement;
      ValidateMetadataHeader(manifestRoot, "manifest", 0, expectedBaseline);
      var payload = manifestRoot.GetProperty("payload");
      if (payload.GetProperty("assetKind").GetString() != "dynamic")
      {
        throw new UnsupportedGltfDomainException("StaticMesh");
      }
      var sourceText = payload.GetProperty("sourceMsh").GetString()
        ?? throw new InvalidDataException("Dynamic source MSH metadata is null.");
      var sourceBytes = GlbDocument.DecodeBase64Url(sourceText, profile.MaxInputBytes).ToArray();
      var source = MshExpert.CreateDynamic(
        sourceBytes,
        new MeshAssetLineageId(expectedBaseline.AssetLineageId),
        CreateMshProfile(profile));
      if (!source.TryGetValue(out var sourceAsset))
      {
        throw new InvalidDataException("Dynamic source MSH metadata is invalid.");
      }
      var objects = Flatten(
        sourceAsset!.RootDynamicObject,
        profile,
        ReadObjectIds(root, profile));
      ValidateSupportedEffects(objects);
      var effectPreviews = CreateEffectPreviews(
        objects,
        profile,
        new Dictionary<int, ReferencedMeshPreview>());
      var inventory = payload.GetProperty("objectInventory").EnumerateArray()
        .Select(item => item.GetInt32()).ToArray();
      if (!inventory.SequenceEqual(objects.Select(item => item.Id).OrderBy(id => id))
        || payload.GetProperty("nextObjectId").GetInt32() <= objects.Max(item => item.Id))
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.InvalidManifestInventory,
          2020,
          "scenes[0].extras.earthtool",
          "Dynamic manifest scope inventory is invalid.");
      }
      var nativeGraph = ValidateObjectGraph(root, objects, expectedBaseline, profile);
      var fingerprintElement = payload.GetProperty("nativeProjection");
      var fingerprint = new NativeProjectionFingerprint(
        fingerprintElement.GetProperty("name").GetString()
          ?? throw new InvalidDataException("Dynamic projection name is null."),
        fingerprintElement.GetProperty("version").GetInt32(),
        fingerprintElement.GetProperty("sha256").GetString()
          ?? throw new InvalidDataException("Dynamic projection digest is null."));
      if (fingerprint.Name != ProjectionName || fingerprint.Version != ProjectionVersion)
      {
        throw new UnsupportedGltfDomainException("DynamicProjection");
      }
      var actualFingerprint = CreateFingerprint(objects, effectPreviews);
      if (!string.Equals(fingerprint.Sha256, actualFingerprint.Sha256, StringComparison.Ordinal))
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.StaleNativeProjection,
          2016,
          "scenes[0].extras.earthtool",
          "Dynamic source projection fingerprint is stale.");
      }
      return Reconcile(
        sourceAsset!,
        objects,
        nativeGraph,
        root,
        binary.Span,
        fingerprint,
        profile,
        effectPreviews);
    }

    private static NativeObjectGraph ValidateObjectGraph(
      JsonElement root,
      IReadOnlyList<DynamicObjectScope> sourceObjects,
      InterchangeBaseline baseline,
      GltfOperationProfile profile)
    {
      var nodes = root.GetProperty("nodes");
      if (nodes.GetArrayLength() != sourceObjects.Count)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.InvalidManifestInventory,
          2020,
          "nodes",
          "Dynamic metadata inventory does not match native nodes.");
      }
      var seen = new HashSet<int>();
      var nodeIndicesById = new Dictionary<int, int>();
      var meshNamesById = new Dictionary<int, IReadOnlyList<byte>>();
      for (var nodeIndex = 0; nodeIndex < nodes.GetArrayLength(); nodeIndex++)
      {
        var node = nodes[nodeIndex];
        var metadataText = GetEarthToolMetadata(node, $"nodes[{nodeIndex}]");
        if (Encoding.UTF8.GetByteCount(metadataText) > profile.MaxMetadataBytes)
        {
          throw MetadataLimit(
            $"nodes[{nodeIndex}].extras.earthtool",
            "A dynamic metadata envelope exceeds its byte limit.");
        }
        using var metadata = ParseMetadata(metadataText, profile);
        var metadataRoot = metadata.RootElement;
        var localId = metadataRoot.GetProperty("scope").GetProperty("localId").GetInt32();
        ValidateMetadataHeader(metadataRoot, "object", localId, baseline);
        if (localId <= 0 || localId > sourceObjects.Count || !seen.Add(localId))
        {
          throw new DynamicMetadataGraphException(
            GltfDiagnosticCodes.DuplicateScopeIdentity,
            2009,
            $"nodes[{nodeIndex}].extras.earthtool",
            "Dynamic object scope identity is duplicate or invalid.");
        }
        nodeIndicesById.Add(localId, nodeIndex);
        var source = sourceObjects.Single(item => item.Id == localId);
        var payload = metadataRoot.GetProperty("payload");
        meshNamesById.Add(
          localId,
          DecodeMetadataBytes(payload, "meshName", profile.MaxInputBytes));
        if (payload.GetProperty("effectType").GetUInt32() != source.Object.Extension.EffectType)
        {
          throw new DynamicMetadataGraphException(
            GltfDiagnosticCodes.UnknownRequiredSemantics,
            2018,
            $"nodes[{nodeIndex}].extras.earthtool",
            "Dynamic effect declaration does not match source metadata.");
        }
        var expectedChildren = source.ChildIds;
        var declaredChildren = payload.GetProperty("orderedChildIds").EnumerateArray()
          .Select(item => item.GetInt32()).ToArray();
        if (!declaredChildren.SequenceEqual(expectedChildren))
        {
          throw new DynamicMetadataGraphException(
            GltfDiagnosticCodes.StaleNativeProjection,
            2016,
            $"nodes[{nodeIndex}].extras.earthtool",
            "Dynamic ordered-child metadata is stale.");
        }
        ValidatePreservedObjectPayload(payload, source.Object, nodeIndex, profile);
        if (!metadataRoot.TryGetProperty("guards", out var guards)
          || !guards.TryGetProperty("orderedChildren", out var guard))
        {
          throw new DynamicMetadataGraphException(
            GltfDiagnosticCodes.MissingRequiredGuard,
            2014,
            $"nodes[{nodeIndex}].extras.earthtool",
            "The dynamic ordered-child guard is missing.");
        }
        if (guard.GetProperty("projection").GetString() != "dynamic-ordered-children"
          || guard.GetProperty("version").GetInt32() != 1)
        {
          throw new DynamicMetadataGraphException(
            GltfDiagnosticCodes.UnsupportedGuard,
            2015,
            $"nodes[{nodeIndex}].extras.earthtool",
            "The dynamic ordered-child guard is unsupported.");
        }
        if (guard.GetProperty("sha256").GetString() != HashIds(expectedChildren))
        {
          throw new DynamicMetadataGraphException(
            GltfDiagnosticCodes.StaleNativeProjection,
            2016,
            $"nodes[{nodeIndex}].extras.earthtool",
            "The dynamic ordered-child guard is stale.");
        }
        ValidateEffectPreviewMetadata(metadataRoot, payload, source.Object.Extension, nodeIndex);
      }
      if (seen.Count != sourceObjects.Count)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.MissingExpectedScope,
          2010,
          "nodes",
          "Dynamic object scope inventory is incomplete.");
      }
      if (!nodeIndicesById.TryGetValue(1, out var rootNodeIndex) || rootNodeIndex != 0)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.KindCarrierMismatch,
          2008,
          "nodes[0]",
          "The dynamic root scope must own the scene root node.");
      }
      var scopes = new Dictionary<int, NativeObjectScope>();
      var ownedMeshes = new HashSet<int>();
      var ownedMaterials = new HashSet<int>();
      var ownedAccessors = new HashSet<int>();
      var ownedViews = new HashSet<int>();
      var ownedRanges = new List<(long Start, long End)>();
      foreach (var pair in nodeIndicesById)
      {
        var node = nodes[pair.Value];
        var childIds = node.TryGetProperty("children", out var children)
          ? children.EnumerateArray().Select(childIndex =>
          {
            var index = childIndex.GetInt32();
            var match = nodeIndicesById.SingleOrDefault(item => item.Value == index);
            return match.Key != 0
              ? match.Key
              : throw new InvalidDataException("Dynamic child node has no object scope.");
          }).ToArray()
          : Array.Empty<int>();
        var source = sourceObjects.Single(item => item.Id == pair.Key).Object.Extension;
        if (HasNativePreview(source.KnownEffectType))
        {
          if (!node.TryGetProperty("mesh", out var meshElement))
          {
            throw AmbiguousPreview(pair.Value, "A sprite-effect scope has no native preview mesh.");
          }
          var meshIndex = meshElement.GetInt32();
          var meshes = root.GetProperty("meshes");
          if (meshIndex < 0 || meshIndex >= meshes.GetArrayLength() || !ownedMeshes.Add(meshIndex))
          {
            throw AmbiguousPreview(pair.Value, "Sprite-effect scopes must own unique native preview meshes.");
          }
          var primitives = meshes[meshIndex].GetProperty("primitives");
          if (primitives.GetArrayLength() != 1
            || !primitives[0].TryGetProperty("material", out var materialElement)
            || !ownedMaterials.Add(materialElement.GetInt32()))
          {
            throw AmbiguousPreview(pair.Value, "Sprite-effect scopes must own unique native preview materials.");
          }
          var primitive = primitives[0];
          var attributes = primitive.GetProperty("attributes");
          var positionAccessorIndex = attributes.GetProperty("POSITION").GetInt32();
          var positionCount = root.GetProperty("accessors")[positionAccessorIndex]
            .GetProperty("count").GetInt32();
          var maximumPositions = source.KnownEffectType == DynamicEffectType.ScalableObject
            ? profile.MaxMeshPreviewVertices
            : profile.MaxActiveRenderVertices;
          if (positionCount <= 0 || positionCount > maximumPositions)
          {
            throw new ResourceLimitException(positionCount, maximumPositions);
          }
          ValidateOwnedAccessor(root, pair.Value,
            positionAccessorIndex, 5126, "VEC3", 12,
            ownedAccessors, ownedViews, ownedRanges);
          ValidateOwnedAccessor(root, pair.Value,
            attributes.GetProperty("NORMAL").GetInt32(), 5126, "VEC3", 12,
            ownedAccessors, ownedViews, ownedRanges);
          ValidateOwnedAccessor(root, pair.Value,
            attributes.GetProperty("TEXCOORD_0").GetInt32(), 5126, "VEC2", 8,
            ownedAccessors, ownedViews, ownedRanges);
          var indexAccessorIndex = primitive.GetProperty("indices").GetInt32();
          var indexComponentType = root.GetProperty("accessors")[indexAccessorIndex]
            .GetProperty("componentType").GetInt32();
          if (indexComponentType is not 5123 and not 5125
            || indexComponentType == 5125
              && source.KnownEffectType != DynamicEffectType.ScalableObject)
          {
            throw AmbiguousPreview(pair.Value,
              "A dynamic preview index accessor has an unsupported component type.");
          }
          ValidateOwnedAccessor(root, pair.Value,
            indexAccessorIndex, indexComponentType, "SCALAR", indexComponentType == 5125 ? 4 : 2,
            ownedAccessors, ownedViews, ownedRanges);
        }
        else if (node.TryGetProperty("mesh", out _))
        {
          throw AmbiguousPreview(pair.Value, "A metadata-only dynamic scope cannot own preview geometry.");
        }
        var transform = ReadNodeTransform(
          node,
          source.KnownEffectType == DynamicEffectType.ScalableObject);
        scopes.Add(pair.Key, new NativeObjectScope(
          pair.Key,
          pair.Value,
          Array.AsReadOnly(childIds),
          transform.Translation,
          transform.ModelScale,
          meshNamesById[pair.Key]));
      }
      return new NativeObjectGraph(scopes);
    }

    private static void ValidateOwnedAccessor(
      JsonElement root,
      int nodeIndex,
      int accessorIndex,
      int componentType,
      string type,
      int elementSize,
      ISet<int> ownedAccessors,
      ISet<int> ownedViews,
      ICollection<(long Start, long End)> ownedRanges)
    {
      var accessors = root.GetProperty("accessors");
      if (accessorIndex < 0
        || accessorIndex >= accessors.GetArrayLength()
        || !ownedAccessors.Add(accessorIndex))
      {
        throw AmbiguousPreview(nodeIndex,
          "Dynamic preview scopes must own unique geometry accessors.");
      }
      var accessor = accessors[accessorIndex];
      if (accessor.GetProperty("componentType").GetInt32() != componentType
        || accessor.GetProperty("type").GetString() != type)
      {
        throw AmbiguousPreview(nodeIndex,
          "A dynamic preview accessor has an unsupported element representation.");
      }
      var views = root.GetProperty("bufferViews");
      var viewIndex = accessor.GetProperty("bufferView").GetInt32();
      if (viewIndex < 0 || viewIndex >= views.GetArrayLength() || !ownedViews.Add(viewIndex))
      {
        throw AmbiguousPreview(nodeIndex,
          "Dynamic preview scopes must own unique geometry buffer views.");
      }
      var view = views[viewIndex];
      if (view.GetProperty("buffer").GetInt32() != 0)
      {
        throw AmbiguousPreview(nodeIndex, "Dynamic preview geometry must use the package buffer.");
      }
      var count = accessor.GetProperty("count").GetInt64();
      var stride = view.TryGetProperty("byteStride", out var strideElement)
        ? strideElement.GetInt64()
        : elementSize;
      var start = (view.TryGetProperty("byteOffset", out var viewOffset)
          ? viewOffset.GetInt64()
          : 0)
        + (accessor.TryGetProperty("byteOffset", out var accessorOffset)
          ? accessorOffset.GetInt64()
          : 0);
      var end = checked(start + (count - 1) * stride + elementSize);
      var bufferLength = root.GetProperty("buffers")[0].GetProperty("byteLength").GetInt64();
      if (count <= 0 || stride < elementSize || start < 0 || end > bufferLength
        || ownedRanges.Any(range => start < range.End && range.Start < end))
      {
        throw AmbiguousPreview(nodeIndex,
          "Dynamic preview geometry byte ranges must be bounded and non-overlapping.");
      }
      ownedRanges.Add((start, end));
    }

    private static void ValidatePreservedObjectPayload(
      JsonElement payload,
      DynamicObject source,
      int nodeIndex,
      GltfOperationProfile profile)
    {
      var common = DecodeMetadataBytes(payload, "commonBaseHeader", 0x368);
      var effect = DecodeMetadataBytes(payload, "effectRepresentation", 0x9C);
      var meshName = DecodeMetadataBytes(payload, "meshName", profile.MaxInputBytes);
      var texturePath = DecodeMetadataBytes(payload, "texturePath", profile.MaxInputBytes);
      if (!common.SequenceEqual(source.CommonBaseHeader.SerializedRepresentation)
        || !effect.SequenceEqual(source.Extension.SerializedRepresentation)
        || source.Extension.KnownEffectType != DynamicEffectType.ScalableObject
          && !meshName.SequenceEqual(source.Extension.MeshNameBytes)
        || !texturePath.SequenceEqual(source.Extension.TexturePathBytes))
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.StaleNativeProjection,
          2016,
          $"nodes[{nodeIndex}].extras.earthtool",
          "Dynamic metadata-only serialized representations are stale.");
      }
    }

    private static IReadOnlyList<byte> DecodeMetadataBytes(
      JsonElement payload,
      string name,
      int maximum)
    {
      var value = payload.GetProperty(name).GetString()
        ?? throw new InvalidOperationException("Dynamic serialized metadata is null.");
      return GlbDocument.DecodeBase64Url(value, maximum);
    }

    private static DynamicMetadataGraphException AmbiguousPreview(int nodeIndex, string message)
    {
      return new DynamicMetadataGraphException(
        GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
        2012,
        $"nodes[{nodeIndex}]",
        message);
    }

    private static void ValidateEffectPreviewMetadata(
      JsonElement metadata,
      JsonElement payload,
      DynamicEffectExtension source,
      int nodeIndex)
    {
      if (!HasExplicitPreviewContract(source.KnownEffectType))
      {
        return;
      }
      var expectedContext = PreviewContextName(source.KnownEffectType);
      if (payload.GetProperty("previewContext").GetString() != expectedContext
        || payload.GetProperty("previewFrameDomain").GetString() != PreviewFrameDomain(source)
        || payload.GetProperty("previewSourceFrame").GetInt32() != PreviewSourceFrame(source)
        || payload.GetProperty("previewTotalLifetimeTicks").GetInt32() != PreviewTotalLifetimeTicks
        || payload.GetProperty("previewRemainingLifetimeTicks").GetInt32()
          != PreviewRemainingLifetimeTicks
        || payload.GetProperty("previewGlobalTick").GetUInt32() != PreviewGlobalTick
        || payload.GetProperty("previewTextureScale").GetSingle() != PreviewTextureScale
        || payload.GetProperty("previewLifetimeProgress").GetSingle() != PreviewLifetimeProgress
        || payload.GetProperty("previewParentPhase").GetSingle() != _previewParentPhase
        || source.KnownEffectType == DynamicEffectType.ScalableObject
          && payload.GetProperty("previewModelScalePhase").GetSingle()
            != GetScalablePreviewPhase(source))
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.StaleNativeProjection,
          2016,
          $"nodes[{nodeIndex}].extras.earthtool",
          "Dynamic effect preview inputs are stale.");
      }
      if (!metadata.GetProperty("guards").TryGetProperty("effectPreview", out var guard))
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.MissingRequiredGuard,
          2014,
          $"nodes[{nodeIndex}].extras.earthtool",
          "The dynamic effect-preview guard is missing.");
      }
      if (guard.GetProperty("projection").GetString() != "dynamic-effect-preview"
        || guard.GetProperty("version").GetInt32() != 1)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.UnsupportedGuard,
          2015,
          $"nodes[{nodeIndex}].extras.earthtool",
          "The dynamic effect-preview guard is unsupported.");
      }
      if (guard.GetProperty("sha256").GetString() != HashPreviewContract(source))
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.StaleNativeProjection,
          2016,
          $"nodes[{nodeIndex}].extras.earthtool",
          "The dynamic effect-preview guard is stale.");
      }
    }

    private static IReadOnlyList<int> ReadObjectIds(
      JsonElement root,
      GltfOperationProfile profile)
    {
      var result = new List<int>();
      var nodes = root.GetProperty("nodes");
      for (var index = 0; index < nodes.GetArrayLength(); index++)
      {
        var metadataText = GetEarthToolMetadata(nodes[index], $"nodes[{index}]");
        if (Encoding.UTF8.GetByteCount(metadataText) > profile.MaxMetadataBytes)
        {
          throw new ResourceLimitException(Encoding.UTF8.GetByteCount(metadataText), profile.MaxMetadataBytes);
        }
        using var metadata = ParseMetadata(metadataText, profile);
        result.Add(metadata.RootElement.GetProperty("scope").GetProperty("localId").GetInt32());
      }
      if (result.Any(id => id <= 0) || result.Distinct().Count() != result.Count)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.DuplicateScopeIdentity,
          2009,
          "nodes",
          "Dynamic object scope identity is duplicate or invalid.");
      }
      return result.AsReadOnly();
    }

    private static void ValidateMetadataBudgets(
      JsonElement root,
      string manifest,
      GltfOperationProfile profile)
    {
      var nodes = root.GetProperty("nodes");
      if (nodes.GetArrayLength() + 1 > profile.MaxMetadataEnvelopes)
      {
        throw MetadataLimit("scenes[0].extras.earthtool", "Dynamic metadata has too many envelopes.");
      }
      long totalBytes = Encoding.UTF8.GetByteCount(manifest);
      if (totalBytes > profile.MaxTotalMetadataBytes)
      {
        throw MetadataLimit(
          "scenes[0].extras.earthtool",
          "Dynamic metadata exceeds the cumulative byte limit.");
      }
      var elementCount = 0;
      var unknownCount = 0;
      ValidateEnvelopeBudget(
        manifest,
        "scenes[0].extras.earthtool",
        profile,
        ref elementCount,
        ref unknownCount);
      for (var index = 0; index < nodes.GetArrayLength(); index++)
      {
        var path = $"nodes[{index}].extras.earthtool";
        var metadata = GetEarthToolMetadata(nodes[index], $"nodes[{index}]");
        var metadataBytes = Encoding.UTF8.GetByteCount(metadata);
        if (metadataBytes > profile.MaxMetadataBytes)
        {
          throw MetadataLimit(path, "A dynamic metadata envelope exceeds its byte limit.");
        }
        totalBytes = checked(totalBytes + metadataBytes);
        if (totalBytes > profile.MaxTotalMetadataBytes)
        {
          throw MetadataLimit(path, "Dynamic metadata exceeds the cumulative byte limit.");
        }
        ValidateEnvelopeBudget(metadata, path, profile, ref elementCount, ref unknownCount);
      }
    }

    private static void ValidateEnvelopeBudget(
      string metadata,
      string path,
      GltfOperationProfile profile,
      ref int elementCount,
      ref int unknownCount)
    {
      using var envelope = ParseMetadata(metadata, profile);
      if (envelope.RootElement.TryGetProperty("guards", out var guards)
        && guards.EnumerateObject().Take(profile.MaxMetadataGuards + 1).Count()
          > profile.MaxMetadataGuards)
      {
        throw MetadataLimit(path, "Dynamic metadata has too many guards.");
      }
      CountMetadataElements(envelope.RootElement, profile.MaxMetadataElements, path, ref elementCount);
      CountUnknownMetadataMembers(
        envelope.RootElement,
        profile.MaxUnknownMetadataMembers,
        path,
        ref unknownCount);
    }

    private static void CountUnknownMetadataMembers(
      JsonElement element,
      int maximum,
      string path,
      ref int count)
    {
      if (element.ValueKind == JsonValueKind.Object)
      {
        foreach (var property in element.EnumerateObject())
        {
          if (!IsKnownMetadataMember(property.Name))
          {
            count = checked(count + 1);
            if (count > maximum)
            {
              throw MetadataLimit(path, "Dynamic metadata has too many unknown additive members.");
            }
          }
          CountUnknownMetadataMembers(property.Value, maximum, path, ref count);
        }
      }
      else if (element.ValueKind == JsonValueKind.Array)
      {
        foreach (var item in element.EnumerateArray())
        {
          CountUnknownMetadataMembers(item, maximum, path, ref count);
        }
      }
    }

    private static bool IsKnownMetadataMember(string name)
    {
      return name is "format"
        or "version"
        or "assetLineageId"
        or "documentId"
        or "scope"
        or "kind"
        or "localId"
        or "guards"
        or "orderedChildren"
        or "effectPreview"
        or "projection"
        or "sha256"
        or "payload"
        or "assetKind"
        or "sourceMsh"
        or "objectInventory"
        or "nextObjectId"
        or "nativeProjection"
        or "name"
        or "effectType"
        or "previewContext"
        or "previewFrameDomain"
        or "previewSourceFrame"
        or "previewTotalLifetimeTicks"
        or "previewRemainingLifetimeTicks"
        or "previewGlobalTick"
        or "previewTextureScale"
        or "previewLifetimeProgress"
        or "previewParentPhase"
        or "previewModelScalePhase"
        or "orderedChildIds"
        or "commonBaseHeader"
        or "effectRepresentation"
        or "meshName"
        or "texturePath";
    }

    private static void CountMetadataElements(
      JsonElement element,
      int maximum,
      string path,
      ref int count)
    {
      count = checked(count + 1);
      if (count > maximum)
      {
        throw MetadataLimit(path, "Dynamic metadata exceeds the cumulative element limit.");
      }
      if (element.ValueKind == JsonValueKind.Object)
      {
        foreach (var property in element.EnumerateObject())
        {
          CountMetadataElements(property.Value, maximum, path, ref count);
        }
      }
      else if (element.ValueKind == JsonValueKind.Array)
      {
        foreach (var item in element.EnumerateArray())
        {
          CountMetadataElements(item, maximum, path, ref count);
        }
      }
    }

    private static DynamicMetadataGraphException MetadataLimit(string path, string message)
    {
      return new DynamicMetadataGraphException(
        GltfDiagnosticCodes.MetadataResourceLimitExceeded,
        2005,
        path,
        message);
    }

    private static DynamicGltfImport Reconcile(
      DynamicMeshAsset sourceAsset,
      IReadOnlyList<DynamicObjectScope> sourceObjects,
      NativeObjectGraph nativeGraph,
      JsonElement root,
      ReadOnlySpan<byte> binary,
      NativeProjectionFingerprint fingerprint,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews)
    {
      var sourceBytes = sourceAsset.GetSerializedRepresentation();
      var rootOffset = GetRootOffset(sourceBytes);
      var slices = new Dictionary<int, DynamicRecordSlice>();
      var sourceIndex = 0;
      var payloadEnd = ReadRecordSlices(
        sourceBytes,
        rootOffset,
        sourceObjects,
        ref sourceIndex,
        slices);
      var changes = new List<PreservationChange>();
      var hierarchyChanged = sourceObjects.Any(scope =>
        !scope.ChildIds.SequenceEqual(nativeGraph.Scopes[scope.Id].ChildIds));
      if (hierarchyChanged)
      {
        changes.Add(new PreservationChange(
          "RootDynamicObject.Children",
          PreservationDisposition.Regenerated,
          "dynamic-native-hierarchy-edit"));
      }
      using var output = new MemoryStream(sourceBytes.Length);
      output.Write(sourceBytes, 0, rootOffset);
      WriteReconciledRecord(
        output,
        1,
        sourceObjects,
        nativeGraph,
        slices,
        root,
        binary,
        changes,
        effectPreviews,
        profile);
      output.Write(sourceBytes, payloadEnd, sourceBytes.Length - payloadEnd);
      var objectIds = GetPreorderIds(nativeGraph);
      if (changes.Count == 0)
      {
        changes.Add(new PreservationChange(
          "RootDynamicObject",
          PreservationDisposition.Retained,
          "dynamic-source-metadata"));
        return new DynamicGltfImport(
          sourceAsset,
          fingerprint,
          new PreservationReport(changes),
          objectIds);
      }
      var built = MshExpert.CreateDynamic(
        output.ToArray(),
        sourceAsset.LineageId,
        CreateMshProfile(profile));
      if (!built.TryGetValue(out var asset))
      {
        throw new InvalidDataException("The dynamic native edits cannot form one valid MSH snapshot.");
      }
      return new DynamicGltfImport(asset!, fingerprint, new PreservationReport(changes), objectIds);
    }

    private static IReadOnlyList<int> GetPreorderIds(NativeObjectGraph graph)
    {
      var result = new List<int>();
      AddPreorderId(1, graph, result);
      return result.AsReadOnly();
    }

    private static void AddPreorderId(int id, NativeObjectGraph graph, ICollection<int> result)
    {
      result.Add(id);
      foreach (var childId in graph.Scopes[id].ChildIds)
      {
        AddPreorderId(childId, graph, result);
      }
    }

    private static void WriteReconciledRecord(
      Stream destination,
      int id,
      IReadOnlyList<DynamicObjectScope> sourceObjects,
      NativeObjectGraph nativeGraph,
      IReadOnlyDictionary<int, DynamicRecordSlice> slices,
      JsonElement root,
      ReadOnlySpan<byte> binary,
      ICollection<PreservationChange> changes,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews,
      GltfOperationProfile profile)
    {
      var source = sourceObjects.Single(item => item.Id == id).Object.Extension;
      var native = nativeGraph.Scopes[id];
      var slice = slices[id];
      var prefix = (byte[])slice.FixedPrefix.Clone();
      if (id != 1 && native.Translation != source.ChildStartTranslation)
      {
        WriteMshVector(prefix, 0x3EC, native.Translation);
        changes.Add(new PreservationChange(
          $"DynamicObjectScopes[{id}].Extension.ChildStartTranslation",
          PreservationDisposition.Regenerated,
          "dynamic-native-transform-edit"));
      }
      if (source.KnownEffectType == DynamicEffectType.ScalableObject
        && !native.ModelScale.Equals(effectPreviews[id].ModelScale))
      {
        var startScale = SolveStartValue(
          native.ModelScale,
          source.EndModelScale,
          effectPreviews[id].ModelScalePhase,
          id,
          "model scale");
        WriteSingle(prefix, 0x3E8, startScale);
        changes.Add(new PreservationChange(
          $"DynamicObjectScopes[{id}].Extension.StartModelScale",
          PreservationDisposition.Regenerated,
          "dynamic-scalable-transform-edit"));
      }
      if (source.KnownEffectType == DynamicEffectType.ScalableObject
        && !native.MeshNameBytes.SequenceEqual(source.MeshNameBytes))
      {
        changes.Add(new PreservationChange(
          $"DynamicObjectScopes[{id}].Extension.MeshNameBytes",
          PreservationDisposition.Regenerated,
          "dynamic-scalable-binding-edit"));
      }
      if (effectPreviews.TryGetValue(id, out var sourcePreview))
      {
        var preview = ReadDynamicPreview(root, binary, id, native.NodeIndex, sourcePreview, profile);
        if (sourcePreview.IsRibbon)
        {
          if (!preview.RibbonHalfWidth!.Value.Equals(sourcePreview.RibbonHalfWidth))
          {
            WriteSingle(prefix, 0x3B0, preview.RibbonHalfWidth.Value);
            changes.Add(new PreservationChange(
              $"DynamicObjectScopes[{id}].Extension.RibbonHalfWidth",
              PreservationDisposition.Regenerated,
              "dynamic-ribbon-preview-edit"));
          }
          if (preview.RibbonPathChanged)
          {
            changes.Add(new PreservationChange(
              $"DynamicObjectScopes[{id}].PreviewPath",
              PreservationDisposition.Retained,
              "dynamic-runtime-preview-input"));
          }
        }
        else if (sourcePreview.IsSphere)
        {
          if (preview.GeometryChanged)
          {
            changes.Add(new PreservationChange(
              $"DynamicObjectScopes[{id}].PreviewShape",
              PreservationDisposition.Retained,
              "dynamic-runtime-preview-input"));
          }
        }
        else if (!sourcePreview.IsScalable
          && !preview.Rectangle.Equals(sourcePreview.Rectangle))
        {
          var rectangle = SolveStartRectangle(
            preview.Rectangle,
            source.EndEffectRectangle,
            sourcePreview.RectanglePhase,
            id);
          WriteRectangle(prefix, 0x38C, rectangle);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.StartEffectRectangle",
            PreservationDisposition.Regenerated,
            "dynamic-sprite-preview-edit"));
        }
        if (sourcePreview.OwnsDepth && !preview.Depth.Equals(source.EffectDepthOffset))
        {
          WriteSingle(prefix, 0x3AC, preview.Depth);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.EffectDepthOffset",
            PreservationDisposition.Regenerated,
            "dynamic-sprite-preview-edit"));
        }
        if (sourcePreview.OwnsColor && preview.Color != sourcePreview.Color)
        {
          var color = source.KnownEffectType == DynamicEffectType.Smoke
            || IsAttachedPreview(source.KnownEffectType)
            ? SolveModulatedColor(preview.Color, source.VisibleTerrainLightGain, id)
            : preview.Color;
          WriteVector(prefix, 0x3C8, color);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.VisibleEffectColor",
            PreservationDisposition.Regenerated,
            "dynamic-sprite-material-edit"));
        }
        if (sourcePreview.OwnsAlpha && !preview.Alpha.Equals(sourcePreview.Alpha))
        {
          var alpha = SolveStartValue(
            preview.Alpha,
            source.EndAlpha,
            sourcePreview.AlphaPhase,
            id,
            "alpha");
          WriteSingle(prefix, 0x3E0, alpha);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.StartAlpha",
            PreservationDisposition.Regenerated,
            "dynamic-sprite-material-edit"));
        }
      }
      destination.Write(prefix, 0, prefix.Length);
      WriteLengthPrefixed(
        destination,
        source.KnownEffectType == DynamicEffectType.ScalableObject
          ? native.MeshNameBytes
          : source.MeshNameBytes);
      WriteLengthPrefixed(destination, slice.TexturePathBytes);
      var childCount = new byte[sizeof(uint)];
      BinaryPrimitives.WriteUInt32LittleEndian(childCount, checked((uint)native.ChildIds.Count));
      destination.Write(childCount, 0, childCount.Length);
      foreach (var childId in native.ChildIds)
      {
        WriteReconciledRecord(
          destination,
          childId,
          sourceObjects,
          nativeGraph,
          slices,
          root,
          binary,
          changes,
          effectPreviews,
          profile);
      }
    }

    private static DynamicEditedPreview ReadDynamicPreview(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int id,
      int nodeIndex,
      DynamicEffectPreview sourcePreview,
      GltfOperationProfile profile)
    {
      var node = root.GetProperty("nodes")[nodeIndex];
      if (!node.TryGetProperty("mesh", out var meshIndexElement))
      {
        throw new InvalidDataException("A sprite-effect preview mesh is missing.");
      }
      var primitive = root.GetProperty("meshes")[meshIndexElement.GetInt32()]
        .GetProperty("primitives")[0];
      if (sourcePreview.IsScalable)
      {
        var scalableMaterialIndex = primitive.GetProperty("material").GetInt32();
        var scalableBaseColorFactor = root.GetProperty("materials")[scalableMaterialIndex]
          .GetProperty("pbrMetallicRoughness").GetProperty("baseColorFactor")
          .EnumerateArray().Select(item => item.GetSingle()).ToArray();
        if (scalableBaseColorFactor.Length != 4
          || scalableBaseColorFactor.Any(value => !float.IsFinite(value)))
        {
          throw new InvalidDataException(
            "A scalable-object preview base color must contain four finite values.");
        }
        return new DynamicEditedPreview(
          new Vector3(
            scalableBaseColorFactor[0],
            scalableBaseColorFactor[1],
            scalableBaseColorFactor[2]),
          scalableBaseColorFactor[3],
          false);
      }
      var positions = ReadVector3Accessor(
        root,
        binary,
        primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32());
      if (sourcePreview.IsRibbon)
      {
        return ReadRibbonPreview(root, binary, id, primitive, positions, sourcePreview, profile);
      }
      if (sourcePreview.IsSphere)
      {
        return ReadSpherePreview(root, binary, id, primitive, positions, sourcePreview, profile);
      }
      if (positions.Length != 4 || positions.Any(value => !IsFinite(value)))
      {
        throw new InvalidDataException("A sprite-effect preview must contain four finite vertices.");
      }
      EffectRectangle rectangle;
      float depth;
      if (sourcePreview.Horizontal)
      {
        if (positions[0].X != positions[3].X
          || positions[1].X != positions[2].X
          || positions[0].Z != positions[1].Z
          || positions[2].Z != positions[3].Z
          || positions.Any(value => value.Y != positions[0].Y))
        {
          throw new InvalidDataException(
            "A terrain-plane sprite preview must remain one axis-aligned four-vertex quad.");
        }
        rectangle = new EffectRectangle(
          positions[0].X,
          -positions[2].Z,
          positions[1].X,
          -positions[0].Z);
        depth = positions[0].Y;
      }
      else
      {
        if (positions[0].X != positions[3].X
          || positions[1].X != positions[2].X
          || positions[0].Y != positions[1].Y
          || positions[2].Y != positions[3].Y
          || positions.Any(value => value.Z != positions[0].Z))
        {
          throw new InvalidDataException(
            "A billboard sprite preview must remain one axis-aligned four-vertex quad.");
        }
        rectangle = new EffectRectangle(
          positions[0].X,
          positions[2].Y,
          positions[1].X,
          positions[0].Y);
        depth = positions[0].Z;
      }
      var materialIndex = primitive.GetProperty("material").GetInt32();
      var baseColorFactor = root.GetProperty("materials")[materialIndex]
        .GetProperty("pbrMetallicRoughness").GetProperty("baseColorFactor")
        .EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (baseColorFactor.Length != 4 || baseColorFactor.Any(value => !float.IsFinite(value)))
      {
        throw new InvalidDataException("A sprite-effect preview base color must contain four finite values.");
      }
      return new DynamicEditedPreview(
        rectangle,
        depth,
        new Vector3(baseColorFactor[0], baseColorFactor[1], baseColorFactor[2]),
        baseColorFactor[3]);
    }

    private static DynamicEditedPreview ReadSpherePreview(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int id,
      JsonElement primitive,
      IReadOnlyList<Vector3> positions,
      DynamicEffectPreview sourcePreview,
      GltfOperationProfile profile)
    {
      if (positions.Count != sourcePreview.Positions.Count
        || positions.Count > profile.MaxActiveRenderVertices
        || positions.Any(value => !IsFinite(value)))
      {
        throw SphereEditFailure(id, "A Sphere preview must retain its bounded finite vertex inventory.");
      }
      var attributes = primitive.GetProperty("attributes");
      var normals = ReadVector3Accessor(
        root,
        binary,
        attributes.GetProperty("NORMAL").GetInt32());
      var textureCoordinates = ReadVector2Accessor(
        root,
        binary,
        attributes.GetProperty("TEXCOORD_0").GetInt32());
      var indices = ReadUInt16Accessor(
        root,
        binary,
        primitive.GetProperty("indices").GetInt32());
      if (normals.Length != positions.Count
        || normals.Any(value => !IsFinite(value))
        || !textureCoordinates.SequenceEqual(sourcePreview.TextureCoordinates)
        || !indices.Select(value => (uint)value).SequenceEqual(sourcePreview.Indices))
      {
        throw SphereEditFailure(id,
          "A Sphere preview must retain finite normals, texture coordinates, and guarded topology.");
      }
      var materialIndex = primitive.GetProperty("material").GetInt32();
      var baseColorFactor = root.GetProperty("materials")[materialIndex]
        .GetProperty("pbrMetallicRoughness").GetProperty("baseColorFactor")
        .EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (baseColorFactor.Length != 4 || baseColorFactor.Any(value => !float.IsFinite(value)))
      {
        throw SphereEditFailure(id, "A Sphere preview base color must contain four finite values.");
      }
      return new DynamicEditedPreview(
        new Vector3(baseColorFactor[0], baseColorFactor[1], baseColorFactor[2]),
        baseColorFactor[3],
        !positions.SequenceEqual(sourcePreview.Positions)
          || !normals.SequenceEqual(sourcePreview.Normals));
    }

    private static DynamicPreviewException SphereEditFailure(int id, string message)
    {
      return new DynamicPreviewException(
        $"DynamicObjectScopes[{id}].SpherePreview",
        message);
    }

    private static DynamicEditedPreview ReadRibbonPreview(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int id,
      JsonElement primitive,
      IReadOnlyList<Vector3> positions,
      DynamicEffectPreview sourcePreview,
      GltfOperationProfile profile)
    {
      if (positions.Count != sourcePreview.Positions.Count
        || positions.Count < 4
        || positions.Count > profile.MaxActiveRenderVertices
        || positions.Count % 2 != 0
        || positions.Any(value => !IsFinite(value)))
      {
        throw RibbonEditFailure(id,
          "A ribbon preview must retain its bounded even count of finite vertex pairs.");
      }
      var attributes = primitive.GetProperty("attributes");
      var normals = ReadVector3Accessor(
        root,
        binary,
        attributes.GetProperty("NORMAL").GetInt32());
      var textureCoordinates = ReadVector2Accessor(
        root,
        binary,
        attributes.GetProperty("TEXCOORD_0").GetInt32());
      var indices = ReadUInt16Accessor(
        root,
        binary,
        primitive.GetProperty("indices").GetInt32());
      if (normals.Length != positions.Count
        || normals.Where((value, index) =>
          !IsFinite(value)
          || value.LengthSquared() < 0.99f
          || Vector3.Dot(Vector3.Normalize(value), sourcePreview.Normals[index]) < 0.9999f).Any()
        || !textureCoordinates.SequenceEqual(sourcePreview.TextureCoordinates)
        || !indices.Select(value => (uint)value).SequenceEqual(sourcePreview.Indices))
      {
        throw RibbonEditFailure(id,
          "A ribbon preview must retain finite normals, atlas-side UVs, and guarded winding topology.");
      }

      var firstCenter = (positions[0] + positions[1]) * 0.5f;
      var nextCenter = (positions[2] + positions[3]) * 0.5f;
      var firstSide = (positions[0] - positions[1]) * 0.5f;
      var ribbonHalfWidthMagnitude = firstSide.Length();
      var sourceNormal = Vector3.Normalize(sourcePreview.Normals[0]);
      var orientation = Vector3.Dot(
        Vector3.Cross(nextCenter - firstCenter, firstSide),
        sourceNormal);
      if (!float.IsFinite(ribbonHalfWidthMagnitude)
        || ribbonHalfWidthMagnitude <= 0
        || Math.Abs(orientation) <= 0.000001f)
      {
        throw RibbonEditFailure(id, "A ribbon preview path or ribbon half-width is degenerate.");
      }
      var sideDirection = Vector3.Normalize(firstSide);
      var sourceFirstSide = (sourcePreview.Positions[0] - sourcePreview.Positions[1]) * 0.5f;
      var sourceSideDirection = Vector3.Normalize(sourceFirstSide);
      var pathChanged = false;
      for (var pair = 0; pair < positions.Count / 2; pair++)
      {
        var left = positions[pair * 2];
        var right = positions[pair * 2 + 1];
        var side = (left - right) * 0.5f;
        if (Math.Abs(side.Length() - ribbonHalfWidthMagnitude)
            > Math.Max(0.0001f, ribbonHalfWidthMagnitude * 0.0001f)
          || Vector3.Dot(Vector3.Normalize(side), sideDirection) < 0.9999f)
        {
          throw RibbonEditFailure(id,
            "Every ribbon preview pair must retain one consistent ribbon half-width and orientation.");
        }
        var center = (left + right) * 0.5f;
        var sourceCenter = (sourcePreview.Positions[pair * 2]
          + sourcePreview.Positions[pair * 2 + 1]) * 0.5f;
        pathChanged |= center != sourceCenter;
        if (pair + 1 < positions.Count / 2)
        {
          var following = (positions[(pair + 1) * 2] + positions[(pair + 1) * 2 + 1]) * 0.5f;
          var segmentOrientation = Vector3.Dot(
            Vector3.Cross(following - center, side),
            sourceNormal);
          if ((following - center).LengthSquared() <= 0.000000000001f
            || Math.Abs(segmentOrientation) <= 0.000001f
            || Math.Sign(segmentOrientation) != Math.Sign(orientation))
          {
            throw RibbonEditFailure(id, "A ribbon preview contains a degenerate path segment.");
          }
        }
      }
      pathChanged |= Vector3.Dot(sideDirection, sourceSideDirection) < 0.9999f;
      var signedRibbonHalfWidth = ribbonHalfWidthMagnitude * Math.Sign(orientation);
      var materialIndex = primitive.GetProperty("material").GetInt32();
      var baseColorFactor = root.GetProperty("materials")[materialIndex]
        .GetProperty("pbrMetallicRoughness").GetProperty("baseColorFactor")
        .EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (baseColorFactor.Length != 4 || baseColorFactor.Any(value => !float.IsFinite(value)))
      {
        throw RibbonEditFailure(id, "A ribbon preview base color must contain four finite values.");
      }
      return new DynamicEditedPreview(
        new Vector3(baseColorFactor[0], baseColorFactor[1], baseColorFactor[2]),
        baseColorFactor[3],
        signedRibbonHalfWidth,
        pathChanged);
    }

    private static DynamicPreviewException RibbonEditFailure(int id, string message)
    {
      return new DynamicPreviewException(
        $"DynamicObjectScopes[{id}].RibbonPreview",
        message);
    }

    private static Vector3[] ReadVector3Accessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex)
    {
      var accessor = root.GetProperty("accessors")[accessorIndex];
      if (accessor.GetProperty("componentType").GetInt32() != 5126
        || accessor.GetProperty("type").GetString() != "VEC3")
      {
        throw new InvalidDataException("A sprite-effect POSITION accessor must use float VEC3 values.");
      }
      var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
      var count = accessor.GetProperty("count").GetInt32();
      var stride = view.TryGetProperty("byteStride", out var strideElement)
        ? strideElement.GetInt32()
        : 12;
      var offset = (view.TryGetProperty("byteOffset", out var viewOffset)
          ? viewOffset.GetInt32()
          : 0)
        + (accessor.TryGetProperty("byteOffset", out var accessorOffset)
          ? accessorOffset.GetInt32()
          : 0);
      if (count < 0 || stride < 12 || offset < 0 || (long)offset + (long)count * stride > binary.Length)
      {
        throw new InvalidDataException("A sprite-effect POSITION accessor exceeds its buffer.");
      }
      var values = new Vector3[count];
      for (var index = 0; index < count; index++)
      {
        var item = binary.Slice(offset + index * stride, 12);
        values[index] = new Vector3(ReadSingle(item), ReadSingle(item.Slice(4)), ReadSingle(item.Slice(8)));
      }
      return values;
    }

    private static Vector2[] ReadVector2Accessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex)
    {
      var accessor = root.GetProperty("accessors")[accessorIndex];
      if (accessor.GetProperty("componentType").GetInt32() != 5126
        || accessor.GetProperty("type").GetString() != "VEC2")
      {
        throw new InvalidDataException("A ribbon TEXCOORD_0 accessor must use float VEC2 values.");
      }
      var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
      var count = accessor.GetProperty("count").GetInt32();
      var stride = view.TryGetProperty("byteStride", out var strideElement)
        ? strideElement.GetInt32()
        : 8;
      var offset = (view.TryGetProperty("byteOffset", out var viewOffset)
          ? viewOffset.GetInt32()
          : 0)
        + (accessor.TryGetProperty("byteOffset", out var accessorOffset)
          ? accessorOffset.GetInt32()
          : 0);
      if (count < 0 || stride < 8 || offset < 0 || (long)offset + (long)count * stride > binary.Length)
      {
        throw new InvalidDataException("A ribbon TEXCOORD_0 accessor exceeds its buffer.");
      }
      var values = new Vector2[count];
      for (var index = 0; index < count; index++)
      {
        var item = binary.Slice(offset + index * stride, 8);
        values[index] = new Vector2(ReadSingle(item), ReadSingle(item.Slice(4)));
      }
      return values;
    }

    private static ushort[] ReadUInt16Accessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex)
    {
      var accessor = root.GetProperty("accessors")[accessorIndex];
      if (accessor.GetProperty("componentType").GetInt32() != 5123
        || accessor.GetProperty("type").GetString() != "SCALAR")
      {
        throw new InvalidDataException("A ribbon index accessor must use unsigned-short scalars.");
      }
      var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
      var count = accessor.GetProperty("count").GetInt32();
      var stride = view.TryGetProperty("byteStride", out var strideElement)
        ? strideElement.GetInt32()
        : 2;
      var offset = (view.TryGetProperty("byteOffset", out var viewOffset)
          ? viewOffset.GetInt32()
          : 0)
        + (accessor.TryGetProperty("byteOffset", out var accessorOffset)
          ? accessorOffset.GetInt32()
          : 0);
      if (count < 0 || stride < 2 || offset < 0 || (long)offset + (long)count * stride > binary.Length)
      {
        throw new InvalidDataException("A ribbon index accessor exceeds its buffer.");
      }
      var values = new ushort[count];
      for (var index = 0; index < count; index++)
      {
        values[index] = BinaryPrimitives.ReadUInt16LittleEndian(
          binary.Slice(offset + index * stride, 2));
      }
      return values;
    }

    private static int ReadRecordSlices(
      byte[] source,
      int offset,
      IReadOnlyList<DynamicObjectScope> sourceObjects,
      ref int sourceIndex,
      IDictionary<int, DynamicRecordSlice> result)
    {
      var scope = sourceObjects[sourceIndex++];
      var cursor = checked(offset + 0x404);
      var meshLength = checked((int)ReadUInt32(source, cursor));
      cursor = checked(cursor + sizeof(uint) + meshLength);
      var textureLength = checked((int)ReadUInt32(source, cursor));
      var textureOffset = checked(cursor + sizeof(uint));
      cursor = checked(cursor + sizeof(uint) + textureLength);
      var childCount = checked((int)ReadUInt32(source, cursor));
      cursor += sizeof(uint);
      if (childCount != scope.Object.Children.Count)
      {
        throw new InvalidDataException("Dynamic source object structure is inconsistent.");
      }
      result.Add(scope.Id, new DynamicRecordSlice(
        source.AsSpan(offset, 0x404).ToArray(),
        source.AsSpan(textureOffset, textureLength).ToArray()));
      foreach (var child in scope.Object.Children)
      {
        cursor = ReadRecordSlices(source, cursor, sourceObjects, ref sourceIndex, result);
      }
      return cursor;
    }

    private static void WriteLengthPrefixed(Stream destination, IReadOnlyList<byte> value)
    {
      var length = new byte[sizeof(uint)];
      BinaryPrimitives.WriteUInt32LittleEndian(length, checked((uint)value.Count));
      destination.Write(length, 0, length.Length);
      var bytes = value as byte[] ?? value.ToArray();
      destination.Write(bytes, 0, bytes.Length);
    }

    private static int GetRootOffset(byte[] source)
    {
      var declaration = ReadUInt32(source, 0);
      return sizeof(uint)
        + ((declaration & 0x10000000) != 0 ? sizeof(uint) : 0)
        + ((declaration & 0x20000000) != 0 ? 16 : 0);
    }

    private static (Vector3 Translation, float ModelScale) ReadNodeTransform(
      JsonElement node,
      bool supportsScale)
    {
      if (node.TryGetProperty("matrix", out _)
        || node.TryGetProperty("rotation", out _))
      {
        throw new InvalidDataException("Dynamic object nodes do not support matrix or rotation edits.");
      }
      var result = Vector3.Zero;
      if (node.TryGetProperty("translation", out var translation))
      {
        var values = translation.EnumerateArray().Select(item => item.GetSingle()).ToArray();
        if (values.Length != 3 || values.Any(value => !float.IsFinite(value)))
        {
          throw new InvalidDataException("Dynamic node translation must contain three finite values.");
        }
        result = new Vector3(values[0], -values[2], values[1]);
      }
      var modelScale = 1f;
      if (node.TryGetProperty("scale", out var scale))
      {
        if (!supportsScale)
        {
          throw new InvalidDataException("Only ScalableObject nodes support scale edits.");
        }
        var values = scale.EnumerateArray().Select(item => item.GetSingle()).ToArray();
        if (values.Length != 3
          || values.Any(value => !float.IsFinite(value))
          || values[0] != values[1]
          || values[0] != values[2])
        {
          throw new InvalidDataException("ScalableObject node scale must be finite and uniform.");
        }
        modelScale = values[0];
      }
      return (result, modelScale);
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    private static float ReadSingle(ReadOnlySpan<byte> source)
    {
      return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(source));
    }

    private static void WriteSingle(byte[] destination, int offset, float value)
    {
      BinaryPrimitives.WriteInt32LittleEndian(
        destination.AsSpan(offset, sizeof(float)),
        BitConverter.SingleToInt32Bits(value));
    }

    private static void WriteVector(byte[] destination, int offset, Vector3 value)
    {
      WriteSingle(destination, offset, value.X);
      WriteSingle(destination, offset + 4, value.Y);
      WriteSingle(destination, offset + 8, value.Z);
    }

    private static void WriteMshVector(byte[] destination, int offset, Vector3 value)
    {
      WriteSingle(destination, offset, value.X);
      WriteSingle(destination, offset + 4, -value.Y);
      WriteSingle(destination, offset + 8, value.Z);
    }

    private static void WriteRectangle(byte[] destination, int offset, EffectRectangle value)
    {
      WriteSingle(destination, offset, value.X0);
      WriteSingle(destination, offset + 4, value.Y1);
      WriteSingle(destination, offset + 8, value.X1);
      WriteSingle(destination, offset + 12, value.Y0);
    }

    private static JsonDocument ParseMetadata(string metadata, GltfOperationProfile profile)
    {
      try
      {
        return JsonDocument.Parse(
          metadata,
          new JsonDocumentOptions
          {
            MaxDepth = profile.MaxJsonDepth,
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false
          });
      }
      catch (JsonException ex)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          "extras.earthtool",
          ex.Message);
      }
    }

    private static void ValidateMetadataHeader(
      JsonElement metadata,
      string expectedKind,
      int expectedLocalId,
      InterchangeBaseline expectedBaseline)
    {
      if (metadata.GetProperty("format").GetString() != "earthtool.msh.gltf"
        || metadata.GetProperty("version").GetInt32() != MetadataVersion)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.UnsupportedMetadataVersion,
          2004,
          "extras.earthtool",
          "Dynamic metadata format or version is unsupported.");
      }
      var lineage = metadata.GetProperty("assetLineageId").GetGuid();
      var document = metadata.GetProperty("documentId").GetGuid();
      if (lineage != expectedBaseline.AssetLineageId)
      {
        throw new DynamicMetadataIdentityException(true);
      }
      if (document != expectedBaseline.DocumentId)
      {
        throw new DynamicMetadataIdentityException(false);
      }
      var scope = metadata.GetProperty("scope");
      if (scope.GetProperty("kind").GetString() != expectedKind
        || scope.GetProperty("localId").GetInt32() != expectedLocalId)
      {
        throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.KindCarrierMismatch,
          2008,
          "extras.earthtool",
          "Dynamic metadata scope does not match its carrier.");
      }
    }

    private static string GetEarthToolMetadata(JsonElement carrier, string path)
    {
      if (!carrier.TryGetProperty("extras", out var extras)
        || extras.ValueKind != JsonValueKind.Object
        || !extras.TryGetProperty("earthtool", out var metadata)
        || metadata.ValueKind != JsonValueKind.String)
      {
        throw new DynamicMetadataGraphException(
          path.StartsWith("scenes[", StringComparison.Ordinal)
            ? GltfDiagnosticCodes.MissingManifest
            : GltfDiagnosticCodes.MissingExpectedScope,
          path.StartsWith("scenes[", StringComparison.Ordinal) ? 2000 : 2010,
          path,
          $"Required dynamic metadata is absent at {path}.");
      }
      return metadata.GetString()
        ?? throw new DynamicMetadataGraphException(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          path,
          $"Dynamic metadata is null at {path}.");
    }

    private static byte[] CreateBinary(
      IReadOnlyList<DynamicObjectScope> objects,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews,
      out IReadOnlyDictionary<int, DynamicMeshLayout> layouts)
    {
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
      var result = new Dictionary<int, DynamicMeshLayout>();
      foreach (var scope in objects.Where(item => effectPreviews.ContainsKey(item.Id)))
      {
        var preview = effectPreviews[scope.Id];
        Align(writer, stream);
        var positionOffset = checked((int)stream.Position);
        foreach (var position in preview.Positions)
        {
          Write(writer, position);
        }
        var normalOffset = checked((int)stream.Position);
        foreach (var normal in preview.Normals)
        {
          Write(writer, normal);
        }
        var uvOffset = checked((int)stream.Position);
        foreach (var uv in preview.TextureCoordinates)
        {
          writer.Write(uv.X);
          writer.Write(uv.Y);
        }
        var indexOffset = checked((int)stream.Position);
        var indexComponentType = preview.Indices.Any(index => index >= ushort.MaxValue)
          ? 5125
          : 5123;
        foreach (var index in preview.Indices)
        {
          if (indexComponentType == 5125)
          {
            writer.Write(index);
          }
          else
          {
            writer.Write(checked((ushort)index));
          }
        }
        result.Add(scope.Id, new DynamicMeshLayout(
          positionOffset,
          normalOffset,
          uvOffset,
          indexOffset,
          indexComponentType));
      }
      layouts = result;
      return stream.ToArray();
    }

    private static byte[] AppendPreviewImages(
      byte[] geometry,
      IReadOnlyList<TexPreview> previews,
      out IReadOnlyDictionary<string, DynamicImageLayout> layouts)
    {
      using var stream = new MemoryStream();
      stream.Write(geometry, 0, geometry.Length);
      var result = new Dictionary<string, DynamicImageLayout>(StringComparer.Ordinal);
      foreach (var preview in previews)
      {
        while (stream.Position % 4 != 0)
        {
          stream.WriteByte(0);
        }
        var offset = checked((int)stream.Position);
        stream.Write(preview.Png, 0, preview.Png.Length);
        result.Add(preview.ContentAddress, new DynamicImageLayout(offset, preview.Png.Length));
      }
      layouts = result;
      return stream.ToArray();
    }

    private static byte[] CreateJson(
      InterchangeBaseline baseline,
      IReadOnlyList<DynamicObjectScope> objects,
      IReadOnlyDictionary<int, DynamicMeshLayout> layouts,
      int binaryLength,
      string? bufferUri,
      string manifest,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyList<TexPreview> previewImages,
      IReadOnlyDictionary<string, DynamicImageLayout> previewLayouts,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews)
    {
      var nodeIndicesById = objects
        .Select((scope, index) => (scope.Id, index))
        .ToDictionary(item => item.Id, item => item.index);
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        writer.WriteStartObject();
        writer.WriteStartObject("asset");
        writer.WriteString("version", "2.0");
        writer.WriteString("generator", "EarthTool dynamic glTF interchange");
        writer.WriteEndObject();
        if (layouts.Count != 0)
        {
          writer.WriteStartArray("extensionsUsed");
          writer.WriteStringValue("KHR_materials_unlit");
          writer.WriteEndArray();
        }
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
        var previewIndex = 0;
        foreach (var scope in objects)
        {
          var extension = scope.Object.Extension;
          writer.WriteStartObject();
          writer.WriteString("name", $"ET_Dynamic_{scope.Id}_{EffectName(extension)}");
          if (scope.ChildIds.Count != 0)
          {
            writer.WriteStartArray("children");
            foreach (var childId in scope.ChildIds)
            {
              writer.WriteNumberValue(nodeIndicesById[childId]);
            }
            writer.WriteEndArray();
          }
          if (effectPreviews.ContainsKey(scope.Id))
          {
            writer.WriteNumber("mesh", previewIndex++);
          }
          if (scope.Id != 1)
          {
            var translation = GlbDocument.ProjectToGltf(extension.ChildStartTranslation);
            writer.WriteStartArray("translation");
            writer.WriteNumberValue(translation.X);
            writer.WriteNumberValue(translation.Y);
            writer.WriteNumberValue(translation.Z);
            writer.WriteEndArray();
          }
          if (effectPreviews.TryGetValue(scope.Id, out var nodePreview)
            && nodePreview.IsScalable)
          {
            writer.WriteStartArray("scale");
            writer.WriteNumberValue(nodePreview.ModelScale);
            writer.WriteNumberValue(nodePreview.ModelScale);
            writer.WriteNumberValue(nodePreview.ModelScale);
            writer.WriteEndArray();
          }
          WriteExtras(writer, CreateObjectMetadata(baseline, scope));
          writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (layouts.Count != 0)
        {
          writer.WriteStartArray("meshes");
          previewIndex = 0;
          var accessorIndex = 0;
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            writer.WriteStartObject();
            writer.WriteString("name", $"ET_{EffectName(scope.Object.Extension)}Preview_{scope.Id}");
            writer.WriteStartArray("primitives");
            writer.WriteStartObject();
            writer.WriteStartObject("attributes");
            writer.WriteNumber("POSITION", accessorIndex);
            writer.WriteNumber("NORMAL", accessorIndex + 1);
            writer.WriteNumber("TEXCOORD_0", accessorIndex + 2);
            writer.WriteEndObject();
            writer.WriteNumber("indices", accessorIndex + 3);
            writer.WriteNumber("material", previewIndex++);
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
            accessorIndex += 4;
          }
          writer.WriteEndArray();

          writer.WriteStartArray("materials");
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            var extension = scope.Object.Extension;
            var effectPreview = effectPreviews[scope.Id];
            writer.WriteStartObject();
            writer.WriteString("name", $"ET_{EffectName(extension)}Preview_{scope.Id}");
            writer.WriteStartObject("pbrMetallicRoughness");
            writer.WriteStartArray("baseColorFactor");
            writer.WriteNumberValue(effectPreview.Color.X);
            writer.WriteNumberValue(effectPreview.Color.Y);
            writer.WriteNumberValue(effectPreview.Color.Z);
            writer.WriteNumberValue(effectPreview.Alpha);
            writer.WriteEndArray();
            writer.WriteNumber("metallicFactor", 0);
            writer.WriteNumber("roughnessFactor", 1);
            if (previews.TryGetValue(scope.Id, out var preview))
            {
              writer.WriteStartObject("baseColorTexture");
              writer.WriteNumber("index", previewImages
                .Select((item, index) => (item, index))
                .Single(item => item.item.ContentAddress == preview.ContentAddress).index);
              writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteString("alphaMode", "BLEND");
            writer.WriteBoolean("doubleSided", true);
            writer.WriteStartObject("extensions");
            writer.WriteStartObject("KHR_materials_unlit");
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
        }

        if (previewImages.Count != 0)
        {
          writer.WriteStartArray("images");
          foreach (var preview in previewImages)
          {
            writer.WriteStartObject();
            writer.WriteString("name", $"ET_TexPreview_{preview.ContentAddress}");
            writer.WriteNumber(
              "bufferView",
              layouts.Count * 4 + previewImages
                .Select((item, index) => (item, index))
                .Single(item => ReferenceEquals(item.item, preview)).index);
            writer.WriteString("mimeType", "image/png");
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
          writer.WriteStartArray("textures");
          for (var index = 0; index < previewImages.Count; index++)
          {
            writer.WriteStartObject();
            writer.WriteNumber("source", index);
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
        }

        if (layouts.Count != 0)
        {
          writer.WriteStartArray("bufferViews");
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            var layout = layouts[scope.Id];
            var preview = effectPreviews[scope.Id];
            WriteBufferView(writer, layout.PositionOffset, preview.Positions.Count * 12, 34962);
            WriteBufferView(writer, layout.NormalOffset, preview.Normals.Count * 12, 34962);
            WriteBufferView(writer, layout.TextureCoordinateOffset,
              preview.TextureCoordinates.Count * 8, 34962);
            WriteBufferView(writer, layout.IndexOffset, preview.Indices.Count
              * (layout.IndexComponentType == 5125 ? 4 : 2), 34963);
          }
          foreach (var preview in previewImages)
          {
            var layout = previewLayouts[preview.ContentAddress];
            WriteBufferView(writer, layout.Offset, layout.Length, null);
          }
          writer.WriteEndArray();

          writer.WriteStartArray("accessors");
          var bufferViewIndex = 0;
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            var preview = effectPreviews[scope.Id];
            var layout = layouts[scope.Id];
            var positions = preview.Positions;
            WriteAccessor(writer, bufferViewIndex, 5126, positions.Count, "VEC3",
              new[]
              {
                positions.Min(item => item.X),
                positions.Min(item => item.Y),
                positions.Min(item => item.Z)
              },
              new[]
              {
                positions.Max(item => item.X),
                positions.Max(item => item.Y),
                positions.Max(item => item.Z)
              });
            WriteAccessor(writer, bufferViewIndex + 1, 5126, preview.Normals.Count, "VEC3",
              new[]
              {
                preview.Normals.Min(item => item.X),
                preview.Normals.Min(item => item.Y),
                preview.Normals.Min(item => item.Z)
              },
              new[]
              {
                preview.Normals.Max(item => item.X),
                preview.Normals.Max(item => item.Y),
                preview.Normals.Max(item => item.Z)
              });
            WriteAccessor(writer, bufferViewIndex + 2, 5126,
              preview.TextureCoordinates.Count, "VEC2", null, null);
            WriteAccessor(writer, bufferViewIndex + 3, layout.IndexComponentType,
              preview.Indices.Count, "SCALAR",
              new[] { 0f }, new[] { (float)preview.Positions.Count - 1 });
            bufferViewIndex += 4;
          }
          writer.WriteEndArray();
        }

        writer.WriteStartArray("buffers");
        writer.WriteStartObject();
        writer.WriteNumber("byteLength", binaryLength);
        if (bufferUri is not null)
        {
          writer.WriteString("uri", bufferUri);
        }
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
      }
      return stream.ToArray();
    }

    private static string CreateManifestMetadata(
      DynamicMeshAsset asset,
      InterchangeBaseline baseline,
      IReadOnlyList<DynamicObjectScope> objects,
      NativeProjectionFingerprint fingerprint)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, "manifest", 0);
        writer.WriteStartObject("payload");
        writer.WriteString("assetKind", "dynamic");
        writer.WriteString("sourceMsh", GlbDocument.EncodeBase64Url(asset.GetSerializedRepresentation()));
        writer.WriteStartArray("objectInventory");
        foreach (var scope in objects.OrderBy(item => item.Id))
        {
          writer.WriteNumberValue(scope.Id);
        }
        writer.WriteEndArray();
        writer.WriteNumber("nextObjectId", objects.Max(item => item.Id) + 1);
        writer.WriteStartObject("nativeProjection");
        writer.WriteString("name", fingerprint.Name);
        writer.WriteNumber("version", fingerprint.Version);
        writer.WriteString("sha256", fingerprint.Sha256);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string CreateObjectMetadata(
      InterchangeBaseline baseline,
      DynamicObjectScope scope)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        WriteMetadataStart(writer, baseline, "object", scope.Id);
        writer.WriteStartObject("guards");
        WriteGuard(writer, "orderedChildren", "dynamic-ordered-children", HashIds(scope.ChildIds));
        if (HasExplicitPreviewContract(scope.Object.Extension.KnownEffectType))
        {
          WriteGuard(writer, "effectPreview", "dynamic-effect-preview",
            HashPreviewContract(scope.Object.Extension));
        }
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteNumber("effectType", scope.Object.Extension.EffectType);
        if (HasExplicitPreviewContract(scope.Object.Extension.KnownEffectType))
        {
          writer.WriteString("previewContext", PreviewContextName(scope.Object.Extension.KnownEffectType));
          writer.WriteString("previewFrameDomain", PreviewFrameDomain(scope.Object.Extension));
          writer.WriteNumber("previewSourceFrame", PreviewSourceFrame(scope.Object.Extension));
          writer.WriteNumber("previewTotalLifetimeTicks", PreviewTotalLifetimeTicks);
          writer.WriteNumber("previewRemainingLifetimeTicks", PreviewRemainingLifetimeTicks);
          writer.WriteNumber("previewGlobalTick", PreviewGlobalTick);
          writer.WriteNumber("previewTextureScale", PreviewTextureScale);
          writer.WriteNumber("previewLifetimeProgress", PreviewLifetimeProgress);
          writer.WriteNumber("previewParentPhase", _previewParentPhase);
          if (scope.Object.Extension.KnownEffectType == DynamicEffectType.ScalableObject)
          {
            writer.WriteNumber(
              "previewModelScalePhase",
              GetScalablePreviewPhase(scope.Object.Extension));
          }
        }
        writer.WriteStartArray("orderedChildIds");
        foreach (var childId in scope.ChildIds)
        {
          writer.WriteNumberValue(childId);
        }
        writer.WriteEndArray();
        writer.WriteString("commonBaseHeader", GlbDocument.EncodeBase64Url(
          scope.Object.CommonBaseHeader.SerializedRepresentation));
        writer.WriteString("effectRepresentation", GlbDocument.EncodeBase64Url(
          scope.Object.Extension.SerializedRepresentation));
        writer.WriteString("meshName", GlbDocument.EncodeBase64Url(scope.Object.Extension.MeshNameBytes));
        writer.WriteString("texturePath", GlbDocument.EncodeBase64Url(scope.Object.Extension.TexturePathBytes));
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteMetadataStart(
      Utf8JsonWriter writer,
      InterchangeBaseline baseline,
      string kind,
      int localId)
    {
      writer.WriteStartObject();
      writer.WriteString("format", "earthtool.msh.gltf");
      writer.WriteNumber("version", MetadataVersion);
      writer.WriteString("assetLineageId", baseline.AssetLineageId.ToString("D"));
      writer.WriteString("documentId", baseline.DocumentId.ToString("D"));
      writer.WriteStartObject("scope");
      writer.WriteString("kind", kind);
      writer.WriteNumber("localId", localId);
      writer.WriteEndObject();
    }

    private static void WriteGuard(
      Utf8JsonWriter writer,
      string name,
      string projection,
      string digest)
    {
      writer.WriteStartObject(name);
      writer.WriteString("projection", projection);
      writer.WriteNumber("version", 1);
      writer.WriteString("sha256", digest);
      writer.WriteEndObject();
    }

    private static void WriteExtras(Utf8JsonWriter writer, string metadata)
    {
      writer.WriteStartObject("extras");
      writer.WriteString("earthtool", metadata);
      writer.WriteEndObject();
    }

    private static void WriteBufferView(
      Utf8JsonWriter writer,
      int offset,
      int length,
      int? target)
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

    private static void WriteAccessor(
      Utf8JsonWriter writer,
      int bufferView,
      int componentType,
      int count,
      string type,
      float[]? minimum,
      float[]? maximum)
    {
      writer.WriteStartObject();
      writer.WriteNumber("bufferView", bufferView);
      writer.WriteNumber("componentType", componentType);
      writer.WriteNumber("count", count);
      writer.WriteString("type", type);
      if (minimum is not null)
      {
        writer.WriteStartArray("min");
        foreach (var value in minimum)
        {
          writer.WriteNumberValue(value);
        }
        writer.WriteEndArray();
      }
      if (maximum is not null)
      {
        writer.WriteStartArray("max");
        foreach (var value in maximum)
        {
          writer.WriteNumberValue(value);
        }
        writer.WriteEndArray();
      }
      writer.WriteEndObject();
    }

    private static IReadOnlyList<DynamicObjectScope> Flatten(
      DynamicObject root,
      GltfOperationProfile profile,
      IReadOnlyList<int>? objectIds = null)
    {
      var objects = new List<DynamicObjectScope>();
      Add(root, 1, objects, profile, objectIds ?? Array.Empty<int>());
      if (objectIds is { Count: > 0 }
        && (objectIds.Count != objects.Count
          || objectIds.Any(id => id <= 0)
          || objectIds.Distinct().Count() != objectIds.Count))
      {
        throw new InvalidDataException("Retained dynamic object identities are invalid.");
      }
      for (var index = 0; index < objects.Count; index++)
      {
        var scope = objects[index];
        var childIds = new List<int>();
        foreach (var child in scope.Object.Children)
        {
          childIds.Add(objects.Single(item => ReferenceEquals(item.Object, child)).Id);
        }
        scope.SetChildIds(childIds);
      }
      return objects.AsReadOnly();
    }

    private static void Add(
      DynamicObject item,
      int depth,
      ICollection<DynamicObjectScope> result,
      GltfOperationProfile profile,
      IReadOnlyList<int> objectIds)
    {
      if (depth > profile.MaxHierarchyDepth)
      {
        throw new ResourceLimitException(depth, profile.MaxHierarchyDepth);
      }
      if (result.Count == profile.MaxNodes)
      {
        throw new ResourceLimitException(result.Count + 1, profile.MaxNodes);
      }
      var index = result.Count;
      result.Add(new DynamicObjectScope(
        objectIds.Count > index ? objectIds[index] : index + 1,
        item));
      foreach (var child in item.Children)
      {
        Add(child, depth + 1, result, profile, objectIds);
      }
    }

    private static IReadOnlyDictionary<int, DynamicEffectPreview> CreateEffectPreviews(
      IReadOnlyList<DynamicObjectScope> objects,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews)
    {
      var result = new Dictionary<int, DynamicEffectPreview>();
      var scalableVertexCount = 0;
      foreach (var scope in objects)
      {
        var extension = scope.Object.Extension;
        if (scope.Id != 1 && !IsFinite(extension.ChildStartTranslation))
        {
          throw new DynamicPreviewException(
            $"DynamicObjectScopes[{scope.Id}].Extension.ChildStartTranslation",
            "The deterministic hierarchy preview requires a finite child start translation.");
        }
        if (!HasNativePreview(extension.KnownEffectType))
        {
          continue;
        }
        var preview = CreateEffectPreview(
          scope.Id,
          extension,
          meshPreviews.TryGetValue(scope.Id, out var meshPreview) ? meshPreview : null);
        var maximumVertices = extension.KnownEffectType == DynamicEffectType.ScalableObject
          ? profile.MaxMeshPreviewVertices
          : profile.MaxActiveRenderVertices;
        if (preview.Positions.Count > maximumVertices)
        {
          throw new ResourceLimitException(
            preview.Positions.Count,
            maximumVertices);
        }
        if (extension.KnownEffectType == DynamicEffectType.ScalableObject)
        {
          scalableVertexCount = checked(scalableVertexCount + preview.Positions.Count);
          if (scalableVertexCount > profile.MaxMeshPreviewVertices)
          {
            throw new ResourceLimitException(
              scalableVertexCount,
              profile.MaxMeshPreviewVertices);
          }
        }
        result.Add(scope.Id, preview);
      }
      return result;
    }

    private static DynamicEffectPreview CreateEffectPreview(
      int id,
      DynamicEffectExtension extension,
      ReferencedMeshPreview? meshPreview)
    {
      if (extension.KnownEffectType == DynamicEffectType.ScalableObject)
      {
        if (!DynamicEffectSemantics.TrySelectFrame(
          extension,
          DynamicEffectEvaluationContext.Primary,
          PreviewTotalLifetimeTicks,
          PreviewRemainingLifetimeTicks,
          PreviewGlobalTick,
          out var scalableFrame,
          out var scalableFrameFailure))
        {
          throw PreviewFailure(id, "Frames", scalableFrameFailure);
        }
        if (!DynamicEffectSemantics.TryInterpolateModelScale(
          extension,
          scalableFrame.Phase,
          out var modelScale,
          out var scaleFailure))
        {
          throw PreviewFailure(id, "ModelScale", scaleFailure);
        }
        if (!DynamicEffectSemantics.TryInterpolateAlpha(
          extension,
          DynamicEffectEvaluationContext.Primary,
          scalableFrame.Phase,
          PreviewLifetimeProgress,
          out var scalableAlpha,
          out var scalableAlphaFailure))
        {
          throw PreviewFailure(id, "Alpha", scalableAlphaFailure);
        }
        if (!float.IsFinite(modelScale))
        {
          throw PreviewFailure(id, "ModelScale", DynamicSemanticFailure.NonFiniteInput);
        }
        ValidateFiniteMaterial(id, extension.VisibleEffectColor, scalableAlpha);
        meshPreview ??= CreateDiagnosticMeshPreview();
        return new DynamicEffectPreview(
          meshPreview.Positions,
          meshPreview.Normals,
          meshPreview.TextureCoordinates,
          meshPreview.Indices,
          Clamp(extension.VisibleEffectColor),
          Clamp(scalableAlpha),
          modelScale,
          scalableFrame.Phase,
          IsUnitRange(extension.VisibleEffectColor),
          IsUnitRange(scalableAlpha));
      }

      if (extension.KnownEffectType == DynamicEffectType.Explosion)
      {
        ValidateFiniteMaterial(id, extension.VisibleEffectColor, extension.StartAlpha);
        return new DynamicEffectPreview(
          extension.StartEffectRectangle,
          extension.EffectDepthOffset,
          Clamp(extension.VisibleEffectColor),
          Clamp(extension.StartAlpha),
          CreateLegacyExplosionTextureCoordinates(extension),
          false,
          true,
          IsUnitRange(extension.VisibleEffectColor),
          IsUnitRange(extension.StartAlpha),
          0,
          0);
      }

      if (extension.KnownEffectType == DynamicEffectType.Sphere)
      {
        return CreateSpherePreview(id, extension);
      }

      var context = IsAttachedPreview(extension.KnownEffectType)
        ? DynamicEffectEvaluationContext.AttachedParticle
        : DynamicEffectEvaluationContext.Primary;

      if (!DynamicEffectSemantics.TrySelectFrame(
        extension,
        context,
        PreviewTotalLifetimeTicks,
        PreviewRemainingLifetimeTicks,
        PreviewGlobalTick,
        out var frame,
        out var frameFailure))
      {
        throw PreviewFailure(id, "Frames", frameFailure);
      }
      if (!DynamicEffectSemantics.TryInterpolateAlpha(
        extension,
        context,
        frame.Phase,
        PreviewLifetimeProgress,
        out var alpha,
        out var alphaFailure))
      {
        throw PreviewFailure(id, "Alpha", alphaFailure);
      }
      var frameEnd = (long)extension.FirstSourceFrame + extension.FrameCount;
      if (frameEnd > int.MaxValue)
      {
        throw PreviewFailure(id, "Frames", DynamicSemanticFailure.ArithmeticOverflow);
      }

      Vector2[] textureCoordinates;
      if (extension.KnownEffectType is DynamicEffectType.Track or DynamicEffectType.MappedExplosion)
      {
        textureCoordinates = FullTextureCoordinates();
      }
      else if (DynamicEffectSemantics.TrySelectTextureRegion(
        extension,
        context,
        frame,
        PreviewTextureScale,
        out var region,
        out var textureFailure))
      {
        var atlasCapacity = (long)extension.SpriteSheetColumnCount
          * extension.SpriteSheetRowCount;
        if (extension.SpriteSheetRowCount <= 0
          || frame.SourceFrame < 0
          || frameEnd > atlasCapacity)
        {
          throw PreviewFailure(id, "SpriteSheet", DynamicSemanticFailure.InvalidSpriteSheet);
        }
        textureCoordinates = new[]
        {
          new Vector2(region.U0, region.V1),
          new Vector2(region.U1, region.V1),
          new Vector2(region.U1, region.V0),
          new Vector2(region.U0, region.V0)
        };
      }
      else
      {
        throw PreviewFailure(id, "SpriteSheet", textureFailure);
      }

      Vector3 color;
      if (extension.KnownEffectType == DynamicEffectType.Track)
      {
        color = Vector3.One;
      }
      else if (extension.KnownEffectType == DynamicEffectType.Keelwater)
      {
        color = _previewWaterColor;
      }
      else if (extension.KnownEffectType == DynamicEffectType.Smoke
        || IsAttachedPreview(extension.KnownEffectType))
      {
        if (!DynamicEffectSemantics.TryEvaluateVisibleEffectColor(
          extension,
          context,
          Vector3.One,
          1,
          out color,
          out var colorFailure))
        {
          throw PreviewFailure(id, "VisibleEffectColor", colorFailure);
        }
      }
      else
      {
        color = extension.VisibleEffectColor;
      }

      if (extension.KnownEffectType is DynamicEffectType.MappedExplosion
        or DynamicEffectType.FlatExplosion
        or DynamicEffectType.Laser
        or DynamicEffectType.Lightning)
      {
        if (!IsFinite(extension.TerrainLightColor))
        {
          throw new DynamicPreviewException(
            $"DynamicObjectScopes[{id}].Extension.TerrainLightColor",
            "The deterministic terrain-light preview requires a finite color.");
        }
        if (!DynamicEffectSemantics.TryEvaluateTerrainLightIntensity(
          extension.LightType,
          0,
          PreviewRemainingLifetimeTicks,
          PreviewTotalLifetimeTicks,
          0,
          out _,
          out var lightFailure))
        {
          throw PreviewFailure(id, "TerrainLight", lightFailure);
        }
      }

      if (extension.KnownEffectType == DynamicEffectType.LaserWall
        && !IsFinite(extension.TerrainLightColor))
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.TerrainLightColor",
          "The deterministic LaserWall terrain-light preview requires a finite color.");
      }

      ValidateFiniteMaterial(id, color, alpha);
      if (IsRibbonEffect(extension.KnownEffectType))
      {
        return CreateRibbonPreview(id, extension, color, alpha, textureCoordinates, frame.Phase);
      }

      if (!DynamicEffectSemantics.TryInterpolateEffectRectangle(
        extension,
        context,
        frame.Phase,
        out var rectangle,
        out var rectangleFailure))
      {
        throw PreviewFailure(id, "EffectRectangle", rectangleFailure);
      }
      var horizontal = extension.KnownEffectType is DynamicEffectType.Track
        or DynamicEffectType.MappedExplosion
        or DynamicEffectType.FlatExplosion;
      var ownsDepth = extension.KnownEffectType is DynamicEffectType.FlatExplosion
        or DynamicEffectType.Smoke
        || IsAttachedPreview(extension.KnownEffectType);
      var ownsColor = extension.KnownEffectType is not DynamicEffectType.Track
        and not DynamicEffectType.Keelwater
        && IsUnitRange(color);
      var ownsAlpha = IsUnitRange(alpha);
      var depth = ownsDepth ? extension.EffectDepthOffset : 0;
      if (!float.IsFinite(depth))
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.EffectDepthOffset",
          "The deterministic sprite preview requires a finite depth offset.");
      }
      var alphaPhase = context == DynamicEffectEvaluationContext.AttachedParticle
        ? frame.Phase
        : extension.UsesLifetimeProgressAlpha
          ? PreviewLifetimeProgress
          : frame.Phase;
      return new DynamicEffectPreview(
        rectangle,
        depth,
        Clamp(color),
        Clamp(alpha),
        textureCoordinates,
        horizontal,
        ownsDepth,
        ownsColor,
        ownsAlpha,
        frame.Phase,
        alphaPhase);
    }

    private static DynamicEffectPreview CreateSpherePreview(
      int id,
      DynamicEffectExtension extension)
    {
      if (!DynamicEffectSemantics.TrySelectSphereFrame(
        PreviewTotalLifetimeTicks,
        PreviewRemainingLifetimeTicks,
        out _,
        out var frameFailure))
      {
        throw PreviewFailure(id, "SphereLifetime", frameFailure);
      }
      ValidateFiniteMaterial(id, extension.VisibleEffectColor, 1);
      const int latitudeSegments = 8;
      const int longitudeSegments = 16;
      var positions = new Vector3[(latitudeSegments + 1) * (longitudeSegments + 1)];
      var normals = new Vector3[positions.Length];
      var textureCoordinates = new Vector2[positions.Length];
      for (var latitude = 0; latitude <= latitudeSegments; latitude++)
      {
        var v = (float)latitude / latitudeSegments;
        var polar = MathF.PI * v;
        for (var longitude = 0; longitude <= longitudeSegments; longitude++)
        {
          var u = (float)longitude / longitudeSegments;
          var azimuth = MathF.PI * 2 * u;
          var index = latitude * (longitudeSegments + 1) + longitude;
          var normal = new Vector3(
            MathF.Sin(polar) * MathF.Cos(azimuth),
            MathF.Cos(polar),
            MathF.Sin(polar) * MathF.Sin(azimuth));
          positions[index] = normal;
          normals[index] = normal;
          textureCoordinates[index] = new Vector2(u, v);
        }
      }
      var indices = new ushort[latitudeSegments * longitudeSegments * 6];
      var offset = 0;
      for (var latitude = 0; latitude < latitudeSegments; latitude++)
      {
        for (var longitude = 0; longitude < longitudeSegments; longitude++)
        {
          var topLeft = checked((ushort)(latitude * (longitudeSegments + 1) + longitude));
          var bottomLeft = checked((ushort)((latitude + 1) * (longitudeSegments + 1) + longitude));
          indices[offset++] = topLeft;
          indices[offset++] = bottomLeft;
          indices[offset++] = checked((ushort)(topLeft + 1));
          indices[offset++] = checked((ushort)(topLeft + 1));
          indices[offset++] = bottomLeft;
          indices[offset++] = checked((ushort)(bottomLeft + 1));
        }
      }
      return new DynamicEffectPreview(
        Array.AsReadOnly(positions),
        Array.AsReadOnly(normals),
        Array.AsReadOnly(textureCoordinates),
        Array.AsReadOnly(indices),
        Clamp(extension.VisibleEffectColor),
        1,
        IsUnitRange(extension.VisibleEffectColor));
    }

    private static DynamicEffectPreview CreateRibbonPreview(
      int id,
      DynamicEffectExtension extension,
      Vector3 color,
      float alpha,
      IReadOnlyList<Vector2> atlasCoordinates,
      float framePhase)
    {
      var ribbonHalfWidth = extension.RibbonHalfWidth;
      if (!float.IsFinite(ribbonHalfWidth) || ribbonHalfWidth == 0)
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.RibbonHalfWidth",
          "The deterministic ribbon preview requires a finite nonzero ribbon half-width.");
      }

      var centers = CreateRibbonCenters(extension.KnownEffectType!.Value);
      var side = extension.KnownEffectType == DynamicEffectType.Lightning
        ? Vector3.UnitX
        : Vector3.UnitY;
      var positions = new Vector3[centers.Length * 2];
      var normals = new Vector3[positions.Length];
      var textureCoordinates = new Vector2[positions.Length];
      var leftU = atlasCoordinates[0].X;
      var rightU = atlasCoordinates[1].X;
      var startV = atlasCoordinates[0].Y;
      var endV = atlasCoordinates[2].Y;
      for (var index = 0; index < centers.Length; index++)
      {
        var pathPhase = (float)index / (centers.Length - 1);
        positions[index * 2] = centers[index] + side * ribbonHalfWidth;
        positions[index * 2 + 1] = centers[index] - side * ribbonHalfWidth;
        normals[index * 2] = Vector3.UnitZ;
        normals[index * 2 + 1] = Vector3.UnitZ;
        var v = startV * (1 - pathPhase) + endV * pathPhase;
        textureCoordinates[index * 2] = new Vector2(leftU, v);
        textureCoordinates[index * 2 + 1] = new Vector2(rightU, v);
      }
      var indices = new ushort[(centers.Length - 1) * 6];
      for (var segment = 0; segment < centers.Length - 1; segment++)
      {
        var vertex = checked((ushort)(segment * 2));
        var offset = segment * 6;
        indices[offset] = vertex;
        indices[offset + 1] = checked((ushort)(vertex + 2));
        indices[offset + 2] = checked((ushort)(vertex + 1));
        indices[offset + 3] = checked((ushort)(vertex + 1));
        indices[offset + 4] = checked((ushort)(vertex + 2));
        indices[offset + 5] = checked((ushort)(vertex + 3));
      }
      var alphaPhase = extension.UsesLifetimeProgressAlpha
        ? PreviewLifetimeProgress
        : framePhase;
      return new DynamicEffectPreview(
        Array.AsReadOnly(positions),
        Array.AsReadOnly(normals),
        Array.AsReadOnly(textureCoordinates),
        Array.AsReadOnly(indices),
        Clamp(color),
        Clamp(alpha),
        ribbonHalfWidth,
        IsUnitRange(color),
        IsUnitRange(alpha),
        alphaPhase);
    }

    private static Vector3[] CreateRibbonCenters(DynamicEffectType effectType)
    {
      if (effectType is DynamicEffectType.Laser or DynamicEffectType.LaserWall)
      {
        return new[] { Vector3.Zero, new Vector3(8, 0, 0) };
      }
      if (effectType == DynamicEffectType.ElectricalCannon)
      {
        var centers = new Vector3[21];
        for (var index = 0; index < centers.Length; index++)
        {
          var phase = (float)index / (centers.Length - 1);
          var deviation = index is 0 or 20 ? 0 : ((index * 17) % 9 - 4) * 0.08f;
          centers[index] = new Vector3(8 * phase, deviation, 0);
        }
        return centers;
      }

      var lightning = new Vector3[31];
      for (var index = 0; index < lightning.Length; index++)
      {
        var phase = (float)index / (lightning.Length - 1);
        var deviation = index is 0 or 30 ? 0 : ((index * 13) % 11 - 5) * 0.06f;
        lightning[index] = new Vector3(deviation, 12 * (1 - phase), 0);
      }
      return lightning;
    }

    private static bool IsRibbonEffect(DynamicEffectType? effectType)
    {
      return effectType is DynamicEffectType.Laser
        or DynamicEffectType.LaserWall
        or DynamicEffectType.ElectricalCannon
        or DynamicEffectType.Lightning;
    }

    private static Vector2[] CreateLegacyExplosionTextureCoordinates(
      DynamicEffectExtension extension)
    {
      if (extension.SpriteSheetColumnCount <= 0
        || extension.SpriteSheetRowCount <= 0
        || extension.FirstSourceFrame < 0)
      {
        return FullTextureCoordinates();
      }
      var column = extension.FirstSourceFrame % extension.SpriteSheetColumnCount;
      var row = extension.FirstSourceFrame / extension.SpriteSheetColumnCount;
      if (row >= extension.SpriteSheetRowCount)
      {
        return FullTextureCoordinates();
      }
      var left = (float)column / extension.SpriteSheetColumnCount;
      var right = (float)(column + 1) / extension.SpriteSheetColumnCount;
      var top = (float)row / extension.SpriteSheetRowCount;
      var bottom = (float)(row + 1) / extension.SpriteSheetRowCount;
      return new[]
      {
        new Vector2(left, bottom),
        new Vector2(right, bottom),
        new Vector2(right, top),
        new Vector2(left, top)
      };
    }

    private static Vector2[] FullTextureCoordinates()
    {
      return new[] { new Vector2(0, 1), Vector2.One, new Vector2(1, 0), Vector2.Zero };
    }

    private static ReferencedMeshPreview CreateDiagnosticMeshPreview()
    {
      return MshPreviewLoader.CreateDiagnosticPreview();
    }

    private static void ValidateFiniteMaterial(int id, Vector3 color, float alpha)
    {
      if (!IsFinite(color) || !float.IsFinite(alpha))
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.VisibleMaterial",
          "The deterministic preview color and alpha must be finite.");
      }
    }

    private static bool IsUnitRange(Vector3 value)
    {
      return IsUnitRange(value.X) && IsUnitRange(value.Y) && IsUnitRange(value.Z);
    }

    private static bool IsUnitRange(float value)
    {
      return value >= 0 && value <= 1;
    }

    private static Vector3 Clamp(Vector3 value)
    {
      return Vector3.Min(Vector3.One, Vector3.Max(Vector3.Zero, value));
    }

    private static float Clamp(float value)
    {
      return Math.Min(1, Math.Max(0, value));
    }

    private static DynamicPreviewException PreviewFailure(
      int id,
      string field,
      DynamicSemanticFailure failure)
    {
      return new DynamicPreviewException(
        $"DynamicObjectScopes[{id}].Extension.{field}",
        $"The deterministic sprite preview cannot evaluate this domain ({failure}).");
    }

    internal static bool HasNativePreview(DynamicEffectType? effectType)
    {
      return effectType is DynamicEffectType.Explosion
        or DynamicEffectType.ScalableObject
        or DynamicEffectType.Track
        or DynamicEffectType.MappedExplosion
        or DynamicEffectType.FlatExplosion
        or DynamicEffectType.Laser
        or DynamicEffectType.LaserWall
        or DynamicEffectType.ElectricalCannon
        or DynamicEffectType.Lightning
        or DynamicEffectType.Smoke
        or DynamicEffectType.Shockwave
        or DynamicEffectType.Line
        or DynamicEffectType.Sphere
        or DynamicEffectType.Keelwater;
    }

    private static bool IsAttachedPreview(DynamicEffectType? effectType)
    {
      return effectType is DynamicEffectType.Shockwave
        or DynamicEffectType.Line
        or DynamicEffectType.Keelwater;
    }

    private static bool HasExplicitPreviewContract(DynamicEffectType? effectType)
    {
      return IsAttachedPreview(effectType)
        || effectType is DynamicEffectType.Sphere or DynamicEffectType.ScalableObject;
    }

    private static string PreviewContextName(DynamicEffectType? effectType)
    {
      return IsAttachedPreview(effectType) ? "attachedParticle" : "primary";
    }

    private static string PreviewFrameDomain(DynamicEffectExtension extension)
    {
      return extension.KnownEffectType == DynamicEffectType.Sphere
        ? "builtIn16"
        : extension.KnownEffectType == DynamicEffectType.Explosion
          ? "declarationStart"
          : "serialized";
    }

    private static int PreviewSourceFrame(DynamicEffectExtension extension)
    {
      if (extension.KnownEffectType == DynamicEffectType.Sphere)
      {
        return DynamicEffectSemantics.TrySelectSphereFrame(
          PreviewTotalLifetimeTicks,
          PreviewRemainingLifetimeTicks,
          out var sourceFrame,
          out _)
          ? sourceFrame
          : throw new InvalidOperationException("The fixed Sphere preview lifetime is invalid.");
      }
      if (extension.KnownEffectType == DynamicEffectType.Explosion)
      {
        return extension.FirstSourceFrame;
      }
      var context = IsAttachedPreview(extension.KnownEffectType)
        ? DynamicEffectEvaluationContext.AttachedParticle
        : DynamicEffectEvaluationContext.Primary;
      return DynamicEffectSemantics.TrySelectFrame(
        extension,
        context,
        PreviewTotalLifetimeTicks,
        PreviewRemainingLifetimeTicks,
        PreviewGlobalTick,
        out var frame,
        out _)
        ? frame.SourceFrame
        : throw new InvalidOperationException("The fixed dynamic preview frame domain is invalid.");
    }

    private static float GetScalablePreviewPhase(DynamicEffectExtension extension)
    {
      return DynamicEffectSemantics.TrySelectFrame(
        extension,
        DynamicEffectEvaluationContext.Primary,
        PreviewTotalLifetimeTicks,
        PreviewRemainingLifetimeTicks,
        PreviewGlobalTick,
        out var frame,
        out _)
        ? frame.Phase
        : throw new InvalidOperationException("The fixed ScalableObject preview frame domain is invalid.");
    }

    private static EffectRectangle SolveStartRectangle(
      EffectRectangle value,
      EffectRectangle end,
      float phase,
      int id)
    {
      return new EffectRectangle(
        SolveStartValue(value.X0, end.X0, phase, id, "rectangle"),
        SolveStartValue(value.Y1, end.Y1, phase, id, "rectangle"),
        SolveStartValue(value.X1, end.X1, phase, id, "rectangle"),
        SolveStartValue(value.Y0, end.Y0, phase, id, "rectangle"));
    }

    private static float SolveStartValue(float value, float end, float phase, int id, string field)
    {
      var denominator = 1 - phase;
      var result = (value - end * phase) / denominator;
      if (denominator == 0 || !float.IsFinite(result))
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.{field}",
          "The edited deterministic preview cannot be mapped back to its owned start value.");
      }
      return result;
    }

    private static Vector3 SolveModulatedColor(Vector3 value, float gain, int id)
    {
      var factor = Math.Min(1, gain);
      if (!float.IsFinite(factor) || factor == 0)
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.VisibleEffectColor",
          "The edited modulated color cannot be inverted through its deterministic light sample.");
      }
      var result = value / factor;
      if (!IsFinite(result))
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.VisibleEffectColor",
          "The edited modulated color produces a non-finite authoritative value.");
      }
      return result;
    }

    private static string HashPreviewContract(DynamicEffectExtension extension)
    {
      using var stream = new MemoryStream();
      using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
      {
        writer.Write(extension.EffectType);
        writer.Write(PreviewContextName(extension.KnownEffectType));
        writer.Write(PreviewFrameDomain(extension));
        writer.Write(PreviewSourceFrame(extension));
        writer.Write(PreviewTotalLifetimeTicks);
        writer.Write(PreviewRemainingLifetimeTicks);
        writer.Write(PreviewGlobalTick);
        writer.Write(PreviewTextureScale);
        writer.Write(PreviewLifetimeProgress);
        writer.Write(_previewParentPhase);
        writer.Write(_previewWaterColor.X);
        writer.Write(_previewWaterColor.Y);
        writer.Write(_previewWaterColor.Z);
      }
      return Hash(stream.ToArray());
    }

    private static void ValidateSupportedEffects(IReadOnlyList<DynamicObjectScope> objects)
    {
      var unsupported = objects.FirstOrDefault(item =>
        item.Object.Extension.KnownEffectType.HasValue
        && item.Object.Extension.KnownEffectType is not DynamicEffectType.Group
        && !HasNativePreview(item.Object.Extension.KnownEffectType));
      if (unsupported is not null)
      {
        throw new UnsupportedGltfDomainException(
          $"DynamicEffect.{unsupported.Object.Extension.KnownEffectType}");
      }
    }

    private static void ValidateGraphBounds(JsonElement root, GltfOperationProfile profile)
    {
      if (!root.TryGetProperty("nodes", out var nodes)
        || nodes.ValueKind != JsonValueKind.Array
        || nodes.GetArrayLength() == 0
        || nodes.GetArrayLength() > profile.MaxNodes)
      {
        throw new ResourceLimitException(
          root.TryGetProperty("nodes", out nodes) && nodes.ValueKind == JsonValueKind.Array
            ? nodes.GetArrayLength()
            : 0,
          profile.MaxNodes);
      }
      if (!root.TryGetProperty("scenes", out var scenes)
        || scenes.GetArrayLength() != 1
        || !root.TryGetProperty("scene", out var scene)
        || scene.GetInt32() != 0)
      {
        throw new InvalidDataException("Dynamic glTF requires one default scene.");
      }
      var visited = new HashSet<int>();
      ValidateDepth(nodes, 0, 1, profile.MaxHierarchyDepth, visited);
      if (visited.Count != nodes.GetArrayLength())
      {
        throw new InvalidDataException("Every dynamic glTF node must be reachable exactly once.");
      }
    }

    private static void ValidateDepth(
      JsonElement nodes,
      int nodeIndex,
      int depth,
      int maximumDepth,
      ISet<int> visited)
    {
      if (nodeIndex < 0 || nodeIndex >= nodes.GetArrayLength() || !visited.Add(nodeIndex))
      {
        throw new InvalidDataException("Dynamic glTF hierarchy contains an invalid or repeated node.");
      }
      if (depth > maximumDepth)
      {
        throw new ResourceLimitException(depth, maximumDepth);
      }
      var node = nodes[nodeIndex];
      if (!node.TryGetProperty("children", out var children))
      {
        return;
      }
      foreach (var child in children.EnumerateArray())
      {
        ValidateDepth(nodes, child.GetInt32(), depth + 1, maximumDepth, visited);
      }
    }

    private static NativeProjectionFingerprint CreateFingerprint(
      IReadOnlyList<DynamicObjectScope> objects,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews)
    {
      using var stream = new MemoryStream();
      using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
      {
        foreach (var scope in objects)
        {
          writer.Write(scope.Id);
          writer.Write(scope.Object.Extension.EffectType);
          writer.Write(scope.ChildIds.Count);
          foreach (var childId in scope.ChildIds)
          {
            writer.Write(childId);
          }
          var extension = scope.Object.Extension;
          writer.Write(extension.ChildStartTranslation.X);
          writer.Write(extension.ChildStartTranslation.Y);
          writer.Write(extension.ChildStartTranslation.Z);
          if (effectPreviews.TryGetValue(scope.Id, out var preview))
          {
            if (preview.IsScalable)
            {
              writer.Write(preview.ModelScale);
              writer.Write(preview.ModelScalePhase);
              writer.Write(preview.Color.X);
              writer.Write(preview.Color.Y);
              writer.Write(preview.Color.Z);
              writer.Write(preview.Alpha);
              continue;
            }
            writer.Write(preview.Rectangle.X0);
            writer.Write(preview.Rectangle.Y1);
            writer.Write(preview.Rectangle.X1);
            writer.Write(preview.Rectangle.Y0);
            writer.Write(preview.Depth);
            writer.Write(preview.Color.X);
            writer.Write(preview.Color.Y);
            writer.Write(preview.Color.Z);
            writer.Write(preview.Alpha);
            if (preview.IsRibbon || preview.IsSphere)
            {
              writer.Write(preview.RibbonHalfWidth);
              foreach (var position in preview.Positions)
              {
                writer.Write(position.X);
                writer.Write(position.Y);
                writer.Write(position.Z);
              }
              foreach (var index in preview.Indices)
              {
                writer.Write(index);
              }
            }
            if (extension.KnownEffectType != DynamicEffectType.Explosion)
            {
              foreach (var uv in preview.TextureCoordinates)
              {
                writer.Write(uv.X);
                writer.Write(uv.Y);
              }
            }
          }
        }
      }
      return new NativeProjectionFingerprint(
        ProjectionName,
        ProjectionVersion,
        Hash(stream.ToArray()));
    }

    private static string HashIds(IReadOnlyList<int> ids)
    {
      var bytes = new byte[ids.Count * sizeof(int)];
      for (var index = 0; index < ids.Count; index++)
      {
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(index * sizeof(int)), ids[index]);
      }
      return Hash(bytes);
    }

    private static string Hash(byte[] bytes)
    {
      using var sha256 = SHA256.Create();
      return BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", string.Empty)
        .ToLowerInvariant();
    }

    private static string EffectName(DynamicEffectExtension extension)
    {
      return extension.KnownEffectType?.ToString() ?? $"Unknown_{extension.EffectType:X8}";
    }

    private static MshOperationProfile CreateMshProfile(GltfOperationProfile profile)
    {
      return new MshOperationProfile(
        maxInputBytes: profile.MaxInputBytes,
        maxOutputBytes: profile.MaxOutputBytes,
        maxDynamicDepth: profile.MaxHierarchyDepth,
        maxDynamicObjects: profile.MaxNodes,
        maxDynamicChildrenPerObject: profile.MaxNodes);
    }

    private static byte[] Pack(byte[] json, byte[] binary)
    {
      var paddedJsonLength = (json.Length + 3) & ~3;
      var paddedBinaryLength = (binary.Length + 3) & ~3;
      var totalLength = checked(12 + 8 + paddedJsonLength + 8 + paddedBinaryLength);
      var glb = new byte[totalLength];
      WriteUInt32(glb, 0, GlbMagic);
      WriteUInt32(glb, 4, 2);
      WriteUInt32(glb, 8, checked((uint)totalLength));
      WriteUInt32(glb, 12, checked((uint)paddedJsonLength));
      WriteUInt32(glb, 16, JsonChunkType);
      json.CopyTo(glb, 20);
      for (var index = 20 + json.Length; index < 20 + paddedJsonLength; index++)
      {
        glb[index] = 0x20;
      }
      var binaryHeader = 20 + paddedJsonLength;
      WriteUInt32(glb, binaryHeader, checked((uint)paddedBinaryLength));
      WriteUInt32(glb, binaryHeader + 4, BinaryChunkType);
      binary.CopyTo(glb, binaryHeader + 8);
      return glb;
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(offset, sizeof(uint)));
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(destination.AsSpan(offset, sizeof(uint)), value);
    }

    private static void Align(BinaryWriter writer, Stream stream)
    {
      while (stream.Position % 4 != 0)
      {
        writer.Write((byte)0);
      }
    }

    private static void Write(BinaryWriter writer, Vector3 value)
    {
      writer.Write(value.X);
      writer.Write(value.Y);
      writer.Write(value.Z);
    }

    private sealed class DynamicObjectScope
    {
      internal int Id { get; }
      internal DynamicObject Object { get; }
      internal IReadOnlyList<int> ChildIds { get; private set; } = Array.Empty<int>();

      internal DynamicObjectScope(int id, DynamicObject item)
      {
        Id = id;
        Object = item;
      }

      internal void SetChildIds(IEnumerable<int> childIds)
      {
        ChildIds = Array.AsReadOnly(childIds.ToArray());
      }
    }

    private sealed class NativeObjectGraph
    {
      internal IReadOnlyDictionary<int, NativeObjectScope> Scopes { get; }

      internal NativeObjectGraph(IReadOnlyDictionary<int, NativeObjectScope> scopes)
      {
        Scopes = scopes;
      }
    }

    private sealed class NativeObjectScope
    {
      internal int Id { get; }
      internal int NodeIndex { get; }
      internal IReadOnlyList<int> ChildIds { get; }
      internal Vector3 Translation { get; }
      internal float ModelScale { get; }
      internal IReadOnlyList<byte> MeshNameBytes { get; }

      internal NativeObjectScope(
        int id,
        int nodeIndex,
        IReadOnlyList<int> childIds,
        Vector3 translation,
        float modelScale,
        IReadOnlyList<byte> meshNameBytes)
      {
        Id = id;
        NodeIndex = nodeIndex;
        ChildIds = childIds;
        Translation = translation;
        ModelScale = modelScale;
        MeshNameBytes = meshNameBytes;
      }
    }

    private sealed class DynamicRecordSlice
    {
      internal byte[] FixedPrefix { get; }
      internal byte[] TexturePathBytes { get; }

      internal DynamicRecordSlice(byte[] fixedPrefix, byte[] texturePathBytes)
      {
        FixedPrefix = fixedPrefix;
        TexturePathBytes = texturePathBytes;
      }
    }

    private sealed class DynamicEffectPreview
    {
      internal EffectRectangle Rectangle { get; }
      internal float Depth { get; }
      internal Vector3 Color { get; }
      internal float Alpha { get; }
      internal IReadOnlyList<Vector3> Positions { get; }
      internal IReadOnlyList<Vector3> Normals { get; }
      internal IReadOnlyList<Vector2> TextureCoordinates { get; }
      internal IReadOnlyList<uint> Indices { get; }
      internal bool Horizontal { get; }
      internal bool IsRibbon { get; }
      internal bool IsSphere { get; }
      internal bool IsScalable { get; }
      internal float RibbonHalfWidth { get; }
      internal float ModelScale { get; }
      internal float ModelScalePhase { get; }
      internal bool OwnsDepth { get; }
      internal bool OwnsColor { get; }
      internal bool OwnsAlpha { get; }
      internal float RectanglePhase { get; }
      internal float AlphaPhase { get; }

      internal DynamicEffectPreview(
        EffectRectangle rectangle,
        float depth,
        Vector3 color,
        float alpha,
        IReadOnlyList<Vector2> textureCoordinates,
        bool horizontal,
        bool ownsDepth,
        bool ownsColor,
        bool ownsAlpha,
        float rectanglePhase,
        float alphaPhase)
      {
        Rectangle = rectangle;
        Depth = depth;
        Color = color;
        Alpha = alpha;
        Positions = Array.AsReadOnly(horizontal
          ? new[]
          {
            new Vector3(rectangle.Left, depth, -rectangle.Bottom),
            new Vector3(rectangle.Right, depth, -rectangle.Bottom),
            new Vector3(rectangle.Right, depth, -rectangle.Top),
            new Vector3(rectangle.Left, depth, -rectangle.Top)
          }
          : new[]
          {
            new Vector3(rectangle.Left, rectangle.Bottom, depth),
            new Vector3(rectangle.Right, rectangle.Bottom, depth),
            new Vector3(rectangle.Right, rectangle.Top, depth),
            new Vector3(rectangle.Left, rectangle.Top, depth)
          });
        var normal = horizontal ? Vector3.UnitY : Vector3.UnitZ;
        Normals = Array.AsReadOnly(new[] { normal, normal, normal, normal });
        TextureCoordinates = textureCoordinates;
        Indices = Array.AsReadOnly(new uint[] { 0, 1, 2, 0, 2, 3 });
        Horizontal = horizontal;
        IsRibbon = false;
        IsSphere = false;
        IsScalable = false;
        RibbonHalfWidth = 0;
        ModelScale = 1;
        ModelScalePhase = 0;
        OwnsDepth = ownsDepth;
        OwnsColor = ownsColor;
        OwnsAlpha = ownsAlpha;
        RectanglePhase = rectanglePhase;
        AlphaPhase = alphaPhase;
      }

      internal DynamicEffectPreview(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<Vector3> normals,
        IReadOnlyList<Vector2> textureCoordinates,
        IReadOnlyList<ushort> indices,
        Vector3 color,
        float alpha,
        float ribbonHalfWidth,
        bool ownsColor,
        bool ownsAlpha,
        float alphaPhase)
      {
        Rectangle = default;
        Depth = 0;
        Color = color;
        Alpha = alpha;
        Positions = positions;
        Normals = normals;
        TextureCoordinates = textureCoordinates;
        Indices = Array.AsReadOnly(indices.Select(value => (uint)value).ToArray());
        Horizontal = false;
        IsRibbon = true;
        IsSphere = false;
        IsScalable = false;
        RibbonHalfWidth = ribbonHalfWidth;
        ModelScale = 1;
        ModelScalePhase = 0;
        OwnsDepth = false;
        OwnsColor = ownsColor;
        OwnsAlpha = ownsAlpha;
        RectanglePhase = 0;
        AlphaPhase = alphaPhase;
      }

      internal DynamicEffectPreview(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<Vector3> normals,
        IReadOnlyList<Vector2> textureCoordinates,
        IReadOnlyList<ushort> indices,
        Vector3 color,
        float alpha,
        bool ownsColor)
      {
        Rectangle = default;
        Depth = 0;
        Color = color;
        Alpha = alpha;
        Positions = positions;
        Normals = normals;
        TextureCoordinates = textureCoordinates;
        Indices = Array.AsReadOnly(indices.Select(value => (uint)value).ToArray());
        Horizontal = false;
        IsRibbon = false;
        IsSphere = true;
        IsScalable = false;
        RibbonHalfWidth = 0;
        ModelScale = 1;
        ModelScalePhase = 0;
        OwnsDepth = false;
        OwnsColor = ownsColor;
        OwnsAlpha = false;
        RectanglePhase = 0;
        AlphaPhase = 0;
      }

      internal DynamicEffectPreview(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<Vector3> normals,
        IReadOnlyList<Vector2> textureCoordinates,
        IReadOnlyList<uint> indices,
        Vector3 color,
        float alpha,
        float modelScale,
        float modelScalePhase,
        bool ownsColor,
        bool ownsAlpha)
      {
        Rectangle = default;
        Depth = 0;
        Color = color;
        Alpha = alpha;
        Positions = positions;
        Normals = normals;
        TextureCoordinates = textureCoordinates;
        Indices = indices;
        Horizontal = false;
        IsRibbon = false;
        IsSphere = false;
        IsScalable = true;
        RibbonHalfWidth = 0;
        ModelScale = modelScale;
        ModelScalePhase = modelScalePhase;
        OwnsDepth = false;
        OwnsColor = ownsColor;
        OwnsAlpha = ownsAlpha;
        RectanglePhase = 0;
        AlphaPhase = modelScalePhase;
      }
    }

    private readonly struct DynamicEditedPreview
    {
      internal EffectRectangle Rectangle { get; }
      internal float Depth { get; }
      internal Vector3 Color { get; }
      internal float Alpha { get; }
      internal float? RibbonHalfWidth { get; }
      internal bool RibbonPathChanged { get; }
      internal bool GeometryChanged { get; }

      internal DynamicEditedPreview(
        EffectRectangle rectangle,
        float depth,
        Vector3 color,
        float alpha)
      {
        Rectangle = rectangle;
        Depth = depth;
        Color = color;
        Alpha = alpha;
        RibbonHalfWidth = null;
        RibbonPathChanged = false;
        GeometryChanged = false;
      }

      internal DynamicEditedPreview(
        Vector3 color,
        float alpha,
        float ribbonHalfWidth,
        bool ribbonPathChanged)
      {
        Rectangle = default;
        Depth = 0;
        Color = color;
        Alpha = alpha;
        RibbonHalfWidth = ribbonHalfWidth;
        RibbonPathChanged = ribbonPathChanged;
        GeometryChanged = false;
      }

      internal DynamicEditedPreview(
        Vector3 color,
        float alpha,
        bool geometryChanged)
      {
        Rectangle = default;
        Depth = 0;
        Color = color;
        Alpha = alpha;
        RibbonHalfWidth = null;
        RibbonPathChanged = false;
        GeometryChanged = geometryChanged;
      }
    }

    private readonly struct DynamicImageLayout
    {
      internal int Offset { get; }
      internal int Length { get; }

      internal DynamicImageLayout(int offset, int length)
      {
        Offset = offset;
        Length = length;
      }
    }

    private readonly struct DynamicMeshLayout
    {
      internal int PositionOffset { get; }
      internal int NormalOffset { get; }
      internal int TextureCoordinateOffset { get; }
      internal int IndexOffset { get; }
      internal int IndexComponentType { get; }

      internal DynamicMeshLayout(
        int positionOffset,
        int normalOffset,
        int textureCoordinateOffset,
        int indexOffset,
        int indexComponentType)
      {
        PositionOffset = positionOffset;
        NormalOffset = normalOffset;
        TextureCoordinateOffset = textureCoordinateOffset;
        IndexOffset = indexOffset;
        IndexComponentType = indexComponentType;
      }
    }
  }

  internal sealed class DynamicMetadataIdentityException : Exception
  {
    internal bool IsLineage { get; }

    internal DynamicMetadataIdentityException(bool isLineage)
      : base(isLineage
        ? "Dynamic asset lineage differs from the expected baseline."
        : "Dynamic document identity differs from the expected baseline.")
    {
      IsLineage = isLineage;
    }
  }

  internal sealed class DynamicPreviewException : Exception
  {
    internal string Path { get; }

    internal DynamicPreviewException(string path, string message)
      : base(message)
    {
      Path = path;
    }
  }

  internal sealed class DynamicMetadataGraphException : Exception
  {
    internal string Code { get; }
    internal int EventId { get; }
    internal string Path { get; }

    internal DynamicMetadataGraphException(
      string code,
      int eventId,
      string path,
      string message)
      : base(message)
    {
      Code = code;
      EventId = eventId;
      Path = path;
    }
  }
}
