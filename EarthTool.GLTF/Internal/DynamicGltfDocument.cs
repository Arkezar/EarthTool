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
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunkType = 0x4E4F534A;
    private const uint BinaryChunkType = 0x004E4942;

    internal static byte[] Create(
      DynamicMeshAsset asset,
      InterchangeBaseline baseline,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyList<int> objectIds,
      out NativeProjectionFingerprint fingerprint)
    {
      var package = CreatePackage(asset, baseline, profile, false, previews, objectIds, out fingerprint);
      return Pack(package.Json, package.Binary);
    }

    internal static GltfPackage CreateSeparate(
      DynamicMeshAsset asset,
      InterchangeBaseline baseline,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyList<int> objectIds,
      out NativeProjectionFingerprint fingerprint)
    {
      return CreatePackage(asset, baseline, profile, true, previews, objectIds, out fingerprint);
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
      IReadOnlyList<int> objectIds,
      out NativeProjectionFingerprint fingerprint)
    {
      var objects = Flatten(asset.RootDynamicObject, profile, objectIds);
      ValidateSupportedEffects(objects);
      var binary = CreateBinary(objects, out var layouts);
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
      fingerprint = CreateFingerprint(objects);
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
        previewLayouts);
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
      var actualFingerprint = CreateFingerprint(objects);
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
        profile);
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
      var ownedPositionAccessors = new HashSet<int>();
      var ownedPositionViews = new HashSet<int>();
      var ownedPositionRanges = new List<(long Start, long End)>();
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
        if (source.KnownEffectType == DynamicEffectType.Explosion)
        {
          if (!node.TryGetProperty("mesh", out var meshElement))
          {
            throw AmbiguousPreview(pair.Value, "An Explosion scope has no native preview mesh.");
          }
          var meshIndex = meshElement.GetInt32();
          var meshes = root.GetProperty("meshes");
          if (meshIndex < 0 || meshIndex >= meshes.GetArrayLength() || !ownedMeshes.Add(meshIndex))
          {
            throw AmbiguousPreview(pair.Value, "Explosion scopes must own unique native preview meshes.");
          }
          var primitives = meshes[meshIndex].GetProperty("primitives");
          if (primitives.GetArrayLength() != 1
            || !primitives[0].TryGetProperty("material", out var materialElement)
            || !ownedMaterials.Add(materialElement.GetInt32()))
          {
            throw AmbiguousPreview(pair.Value, "Explosion scopes must own unique native preview materials.");
          }
          var positionAccessor = primitives[0].GetProperty("attributes")
            .GetProperty("POSITION").GetInt32();
          var accessors = root.GetProperty("accessors");
          if (positionAccessor < 0
            || positionAccessor >= accessors.GetArrayLength()
            || !ownedPositionAccessors.Add(positionAccessor))
          {
            throw AmbiguousPreview(pair.Value, "Explosion scopes must own unique POSITION accessors.");
          }
          var positionView = accessors[positionAccessor].GetProperty("bufferView").GetInt32();
          if (!ownedPositionViews.Add(positionView))
          {
            throw AmbiguousPreview(pair.Value, "Explosion scopes must own unique POSITION buffer views.");
          }
          var view = root.GetProperty("bufferViews")[positionView];
          var accessor = accessors[positionAccessor];
          var stride = view.TryGetProperty("byteStride", out var strideElement)
            ? strideElement.GetInt32()
            : 12;
          var start = (view.TryGetProperty("byteOffset", out var viewOffset)
              ? viewOffset.GetInt64()
              : 0)
            + (accessor.TryGetProperty("byteOffset", out var accessorOffset)
              ? accessorOffset.GetInt64()
              : 0);
          var count = accessor.GetProperty("count").GetInt64();
          var end = checked(start + (count - 1) * stride + 12);
          if (ownedPositionRanges.Any(range => start < range.End && range.Start < end))
          {
            throw AmbiguousPreview(pair.Value, "Explosion POSITION byte ranges must not overlap.");
          }
          ownedPositionRanges.Add((start, end));
        }
        else if (node.TryGetProperty("mesh", out _))
        {
          throw AmbiguousPreview(pair.Value, "A metadata-only dynamic scope cannot own preview geometry.");
        }
        scopes.Add(pair.Key, new NativeObjectScope(
          pair.Key,
          pair.Value,
          Array.AsReadOnly(childIds),
          ReadNodeTranslation(node)));
      }
      return new NativeObjectGraph(scopes);
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
        || !meshName.SequenceEqual(source.Extension.MeshNameBytes)
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
      GltfOperationProfile profile)
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
        changes);
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
      ICollection<PreservationChange> changes)
    {
      var source = sourceObjects.Single(item => item.Id == id).Object.Extension;
      var native = nativeGraph.Scopes[id];
      var prefix = (byte[])slices[id].Prefix.Clone();
      BinaryPrimitives.WriteUInt32LittleEndian(
        prefix.AsSpan(prefix.Length - sizeof(uint)),
        checked((uint)native.ChildIds.Count));
      if (id != 1 && native.Translation != source.ChildStartTranslation)
      {
        WriteMshVector(prefix, 0x3EC, native.Translation);
        changes.Add(new PreservationChange(
          $"DynamicObjectScopes[{id}].Extension.ChildStartTranslation",
          PreservationDisposition.Regenerated,
          "dynamic-native-transform-edit"));
      }
      if (source.KnownEffectType == DynamicEffectType.Explosion)
      {
        var preview = ReadExplosionPreview(root, binary, native.NodeIndex);
        if (!preview.Rectangle.Equals(source.StartEffectRectangle))
        {
          WriteRectangle(prefix, 0x38C, preview.Rectangle);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.StartEffectRectangle",
            PreservationDisposition.Regenerated,
            "dynamic-explosion-preview-edit"));
        }
        if (!preview.Depth.Equals(source.EffectDepthOffset))
        {
          WriteSingle(prefix, 0x3AC, preview.Depth);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.EffectDepthOffset",
            PreservationDisposition.Regenerated,
            "dynamic-explosion-preview-edit"));
        }
        if (preview.Color != source.VisibleEffectColor)
        {
          WriteVector(prefix, 0x3C8, preview.Color);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.VisibleEffectColor",
            PreservationDisposition.Regenerated,
            "dynamic-explosion-material-edit"));
        }
        if (!preview.Alpha.Equals(source.StartAlpha))
        {
          WriteSingle(prefix, 0x3E0, preview.Alpha);
          changes.Add(new PreservationChange(
            $"DynamicObjectScopes[{id}].Extension.StartAlpha",
            PreservationDisposition.Regenerated,
            "dynamic-explosion-material-edit"));
        }
      }
      destination.Write(prefix, 0, prefix.Length);
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
          changes);
      }
    }

    private static ExplosionPreview ReadExplosionPreview(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int nodeIndex)
    {
      var node = root.GetProperty("nodes")[nodeIndex];
      if (!node.TryGetProperty("mesh", out var meshIndexElement))
      {
        throw new InvalidDataException("An Explosion preview mesh is missing.");
      }
      var primitive = root.GetProperty("meshes")[meshIndexElement.GetInt32()]
        .GetProperty("primitives")[0];
      var positions = ReadVector3Accessor(
        root,
        binary,
        primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32());
      if (positions.Length != 4
        || positions.Any(value => !IsFinite(value))
        || positions[0].X != positions[3].X
        || positions[1].X != positions[2].X
        || positions[0].Y != positions[1].Y
        || positions[2].Y != positions[3].Y
        || positions.Any(value => value.Z != positions[0].Z))
      {
        throw new InvalidDataException("An Explosion preview must remain one axis-aligned four-vertex quad.");
      }
      var materialIndex = primitive.GetProperty("material").GetInt32();
      var factor = root.GetProperty("materials")[materialIndex]
        .GetProperty("pbrMetallicRoughness").GetProperty("baseColorFactor")
        .EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (factor.Length != 4 || factor.Any(value => !float.IsFinite(value)))
      {
        throw new InvalidDataException("An Explosion preview base color must contain four finite values.");
      }
      return new ExplosionPreview(
        new EffectRectangle(
          positions[0].X,
          positions[2].Y,
          positions[1].X,
          positions[0].Y),
        positions[0].Z,
        new Vector3(factor[0], factor[1], factor[2]),
        factor[3]);
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
        throw new InvalidDataException("An Explosion POSITION accessor must use float VEC3 values.");
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
        throw new InvalidDataException("An Explosion POSITION accessor exceeds its buffer.");
      }
      var values = new Vector3[count];
      for (var index = 0; index < count; index++)
      {
        var item = binary.Slice(offset + index * stride, 12);
        values[index] = new Vector3(ReadSingle(item), ReadSingle(item.Slice(4)), ReadSingle(item.Slice(8)));
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
      cursor = checked(cursor + sizeof(uint) + textureLength);
      var childCount = checked((int)ReadUInt32(source, cursor));
      cursor += sizeof(uint);
      if (childCount != scope.Object.Children.Count)
      {
        throw new InvalidDataException("Dynamic source object structure is inconsistent.");
      }
      result.Add(scope.Id, new DynamicRecordSlice(source.AsSpan(offset, cursor - offset).ToArray()));
      foreach (var child in scope.Object.Children)
      {
        cursor = ReadRecordSlices(source, cursor, sourceObjects, ref sourceIndex, result);
      }
      return cursor;
    }

    private static int GetRootOffset(byte[] source)
    {
      var declaration = ReadUInt32(source, 0);
      return sizeof(uint)
        + ((declaration & 0x10000000) != 0 ? sizeof(uint) : 0)
        + ((declaration & 0x20000000) != 0 ? 16 : 0);
    }

    private static Vector3 ReadNodeTranslation(JsonElement node)
    {
      if (node.TryGetProperty("matrix", out _)
        || node.TryGetProperty("rotation", out _)
        || node.TryGetProperty("scale", out _))
      {
        throw new InvalidDataException("Dynamic object nodes support translation edits only.");
      }
      if (!node.TryGetProperty("translation", out var translation))
      {
        return Vector3.Zero;
      }
      var values = translation.EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (values.Length != 3 || values.Any(value => !float.IsFinite(value)))
      {
        throw new InvalidDataException("Dynamic node translation must contain three finite values.");
      }
      return new Vector3(values[0], -values[2], values[1]);
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
      out IReadOnlyDictionary<int, DynamicMeshLayout> layouts)
    {
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
      var result = new Dictionary<int, DynamicMeshLayout>();
      foreach (var scope in objects.Where(item =>
        item.Object.Extension.KnownEffectType == DynamicEffectType.Explosion))
      {
        var extension = scope.Object.Extension;
        Align(writer, stream);
        var positionOffset = checked((int)stream.Position);
        var rectangle = extension.StartEffectRectangle;
        var z = extension.EffectDepthOffset;
        Write(writer, new Vector3(rectangle.Left, rectangle.Bottom, z));
        Write(writer, new Vector3(rectangle.Right, rectangle.Bottom, z));
        Write(writer, new Vector3(rectangle.Right, rectangle.Top, z));
        Write(writer, new Vector3(rectangle.Left, rectangle.Top, z));
        var normalOffset = checked((int)stream.Position);
        for (var index = 0; index < 4; index++)
        {
          Write(writer, Vector3.UnitZ);
        }
        var uvOffset = checked((int)stream.Position);
        foreach (var uv in CreatePreviewTextureCoordinates(extension))
        {
          writer.Write(uv.X);
          writer.Write(uv.Y);
        }
        var indexOffset = checked((int)stream.Position);
        foreach (var index in new ushort[] { 0, 1, 2, 0, 2, 3 })
        {
          writer.Write(index);
        }
        result.Add(scope.Id, new DynamicMeshLayout(
          positionOffset,
          normalOffset,
          uvOffset,
          indexOffset));
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

    private static Vector2[] CreatePreviewTextureCoordinates(DynamicEffectExtension extension)
    {
      if (extension.SpriteSheetColumnCount <= 0
        || extension.SpriteSheetRowCount <= 0
        || extension.FirstSourceFrame < 0)
      {
        return new[] { new Vector2(0, 1), Vector2.One, new Vector2(1, 0), Vector2.Zero };
      }
      var frame = extension.FirstSourceFrame;
      var column = frame % extension.SpriteSheetColumnCount;
      var row = frame / extension.SpriteSheetColumnCount;
      if (row >= extension.SpriteSheetRowCount)
      {
        return new[] { new Vector2(0, 1), Vector2.One, new Vector2(1, 0), Vector2.Zero };
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

    private static byte[] CreateJson(
      InterchangeBaseline baseline,
      IReadOnlyList<DynamicObjectScope> objects,
      IReadOnlyDictionary<int, DynamicMeshLayout> layouts,
      int binaryLength,
      string? bufferUri,
      string manifest,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyList<TexPreview> previewImages,
      IReadOnlyDictionary<string, DynamicImageLayout> previewLayouts)
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
        var explosionIndex = 0;
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
          if (extension.KnownEffectType == DynamicEffectType.Explosion)
          {
            writer.WriteNumber("mesh", explosionIndex++);
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
          WriteExtras(writer, CreateObjectMetadata(baseline, scope));
          writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (layouts.Count != 0)
        {
          writer.WriteStartArray("meshes");
          explosionIndex = 0;
          var accessorIndex = 0;
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            writer.WriteStartObject();
            writer.WriteString("name", $"ET_ExplosionPreview_{scope.Id}");
            writer.WriteStartArray("primitives");
            writer.WriteStartObject();
            writer.WriteStartObject("attributes");
            writer.WriteNumber("POSITION", accessorIndex);
            writer.WriteNumber("NORMAL", accessorIndex + 1);
            writer.WriteNumber("TEXCOORD_0", accessorIndex + 2);
            writer.WriteEndObject();
            writer.WriteNumber("indices", accessorIndex + 3);
            writer.WriteNumber("material", explosionIndex++);
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
            writer.WriteStartObject();
            writer.WriteString("name", $"ET_ExplosionPreview_{scope.Id}");
            writer.WriteStartObject("pbrMetallicRoughness");
            writer.WriteStartArray("baseColorFactor");
            writer.WriteNumberValue(extension.VisibleEffectColor.X);
            writer.WriteNumberValue(extension.VisibleEffectColor.Y);
            writer.WriteNumberValue(extension.VisibleEffectColor.Z);
            writer.WriteNumberValue(extension.StartAlpha);
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
            WriteBufferView(writer, layout.PositionOffset, 48, 34962);
            WriteBufferView(writer, layout.NormalOffset, 48, 34962);
            WriteBufferView(writer, layout.TextureCoordinateOffset, 32, 34962);
            WriteBufferView(writer, layout.IndexOffset, 12, 34963);
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
            var rectangle = scope.Object.Extension.StartEffectRectangle;
            var z = scope.Object.Extension.EffectDepthOffset;
            WriteAccessor(writer, bufferViewIndex, 5126, 4, "VEC3",
              new[] { rectangle.Left, rectangle.Bottom, z },
              new[] { rectangle.Right, rectangle.Top, z });
            WriteAccessor(writer, bufferViewIndex + 1, 5126, 4, "VEC3",
              new[] { 0f, 0f, 1f }, new[] { 0f, 0f, 1f });
            WriteAccessor(writer, bufferViewIndex + 2, 5126, 4, "VEC2", null, null);
            WriteAccessor(writer, bufferViewIndex + 3, 5123, 6, "SCALAR",
              new[] { 0f }, new[] { 3f });
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
        writer.WriteEndObject();
        writer.WriteStartObject("payload");
        writer.WriteNumber("effectType", scope.Object.Extension.EffectType);
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

    private static void ValidateSupportedEffects(IReadOnlyList<DynamicObjectScope> objects)
    {
      var unsupported = objects.FirstOrDefault(item =>
        item.Object.Extension.KnownEffectType.HasValue
        && item.Object.Extension.KnownEffectType is not DynamicEffectType.Group
          and not DynamicEffectType.Explosion);
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
      IReadOnlyList<DynamicObjectScope> objects)
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
          if (extension.KnownEffectType == DynamicEffectType.Explosion)
          {
            writer.Write(extension.StartEffectRectangle.X0);
            writer.Write(extension.StartEffectRectangle.Y1);
            writer.Write(extension.StartEffectRectangle.X1);
            writer.Write(extension.StartEffectRectangle.Y0);
            writer.Write(extension.EffectDepthOffset);
            writer.Write(extension.VisibleEffectColor.X);
            writer.Write(extension.VisibleEffectColor.Y);
            writer.Write(extension.VisibleEffectColor.Z);
            writer.Write(extension.StartAlpha);
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

      internal NativeObjectScope(
        int id,
        int nodeIndex,
        IReadOnlyList<int> childIds,
        Vector3 translation)
      {
        Id = id;
        NodeIndex = nodeIndex;
        ChildIds = childIds;
        Translation = translation;
      }
    }

    private sealed class DynamicRecordSlice
    {
      internal byte[] Prefix { get; }

      internal DynamicRecordSlice(byte[] prefix)
      {
        Prefix = prefix;
      }
    }

    private readonly struct ExplosionPreview
    {
      internal EffectRectangle Rectangle { get; }
      internal float Depth { get; }
      internal Vector3 Color { get; }
      internal float Alpha { get; }

      internal ExplosionPreview(
        EffectRectangle rectangle,
        float depth,
        Vector3 color,
        float alpha)
      {
        Rectangle = rectangle;
        Depth = depth;
        Color = color;
        Alpha = alpha;
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

      internal DynamicMeshLayout(
        int positionOffset,
        int normalOffset,
        int textureCoordinateOffset,
        int indexOffset)
      {
        PositionOffset = positionOffset;
        NormalOffset = normalOffset;
        TextureCoordinateOffset = textureCoordinateOffset;
        IndexOffset = indexOffset;
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
