#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
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
  internal static class DynamicGltfDocument
  {
    private const int PreviewTotalLifetimeTicks = 100;
    private const int PreviewRemainingLifetimeTicks = 100;
    private const float PreviewTicksPerSecond = 20;
    private const float PreviewDurationSeconds = PreviewTotalLifetimeTicks / PreviewTicksPerSecond;
    private const uint PreviewGlobalTick = 0;
    private const float PreviewTextureScale = 1;
    private const float PreviewLifetimeProgress = 0;
    private static readonly Vector3 _previewWaterColor = new(0.2f, 0.45f, 0.7f);
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunkType = 0x4E4F534A;
    private const uint BinaryChunkType = 0x004E4942;

    internal static byte[] Create(
      DynamicMeshAsset asset,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews,
      string? sourceBaseName
    )
    {
      var package = CreatePackage(
        asset,
        profile,
        false,
        previews,
        meshPreviews,
        sourceBaseName
      );
      return Pack(package.Json, package.Binary);
    }

    internal static GltfPackage CreateSeparate(
      DynamicMeshAsset asset,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews,
      string? sourceBaseName
    )
    {
      return CreatePackage(
        asset,
        profile,
        true,
        previews,
        meshPreviews,
        sourceBaseName
      );
    }

    internal static void ValidateGlb(byte[] glb, GltfOperationProfile profile)
    {
      if (glb.Length > profile.MaxInputBytes)
      {
        throw new ResourceLimitException(glb.Length, profile.MaxInputBytes);
      }
      if (
        glb.Length < 32
        || ReadUInt32(glb, 0) != GlbMagic
        || ReadUInt32(glb, 4) != 2
        || ReadUInt32(glb, 8) != glb.Length
      )
      {
        throw new InvalidDataException("Invalid dynamic GLB header.");
      }
      var jsonLength = checked((int)ReadUInt32(glb, 12));
      var binaryHeader = checked(20 + jsonLength);
      if (
        jsonLength <= 0
        || binaryHeader + 8 > glb.Length
        || ReadUInt32(glb, 16) != JsonChunkType
        || ReadUInt32(glb, binaryHeader + 4) != BinaryChunkType
        || binaryHeader + 8 + ReadUInt32(glb, binaryHeader) != glb.Length
      )
      {
        throw new InvalidDataException("Invalid dynamic GLB chunks.");
      }
      using var json = JsonDocument.Parse(
        glb.AsMemory(20, jsonLength),
        new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth }
      );
      ValidateGraphBounds(json.RootElement, profile);
      ValidateAuthoringMetadataBudgets(json.RootElement, profile);
      ModelRoot.ParseGLB(
        new ArraySegment<byte>(glb),
        new ReadSettings { Validation = ValidationMode.Strict }
      );
    }

    internal static void ValidateSeparatePackage(
      byte[] json,
      byte[] binary,
      string bufferUri,
      IReadOnlyDictionary<string, byte[]> images,
      GltfOperationProfile profile
    )
    {
      var inputLength = checked(json.Length + (long)binary.Length);
      if (inputLength > profile.MaxInputBytes)
      {
        throw new ResourceLimitException(inputLength, profile.MaxInputBytes);
      }
      using var document = JsonDocument.Parse(
        json,
        new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth }
      );
      ValidateGraphBounds(document.RootElement, profile);
      ValidateAuthoringMetadataBudgets(document.RootElement, profile);
      GlbDocument.ValidateSeparate(json, binary, bufferUri, images);
    }

    private static GltfPackage CreatePackage(
      DynamicMeshAsset asset,
      GltfOperationProfile profile,
      bool separate,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews,
      string? sourceBaseName
    )
    {
      var objects = Flatten(asset.RootDynamicObject, profile, 2);
      if (objects.Count >= profile.MaxNodes)
      {
        throw new ResourceLimitException(objects.Count + 1, profile.MaxNodes);
      }
      ValidateSupportedEffects(objects);
      var effectPreviews = CreateEffectPreviews(objects, profile, meshPreviews);
      var animationTracks = CreateAnimationTracks(objects, effectPreviews);
      var binary = CreateBinary(objects, effectPreviews, out var layouts);
      var previewImages = objects
        .Where(scope => previews.ContainsKey(scope.Id))
        .Select(scope => previews[scope.Id])
        .GroupBy(preview => preview.ContentAddress, StringComparer.Ordinal)
        .Select(group => group.First())
        .ToArray();
      binary = AppendPreviewImages(binary, previewImages, out var previewLayouts);
      binary = AppendAnimationTracks(binary, animationTracks, out var animationLayout);
      if (binary.Length == 0)
      {
        binary = new byte[4];
      }
      var bufferFileName = Hash(binary) + ".bin";
      var authoringMetadata = CreateObjectAuthoringMetadata(objects, profile);
      var json = CreateJson(
        objects,
        layouts,
        binary.Length,
        separate ? bufferFileName : null,
        authoringMetadata,
        previews,
        previewImages,
        previewLayouts,
        effectPreviews,
        animationTracks,
        animationLayout,
        sourceBaseName
      );
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
        new Dictionary<string, byte[]>(StringComparer.Ordinal)
      );
    }

    private static byte[] CreateBinary(
      IReadOnlyList<DynamicObjectScope> objects,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews,
      out IReadOnlyDictionary<int, DynamicMeshLayout> layouts
    )
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
        result.Add(
          scope.Id,
          new DynamicMeshLayout(
            positionOffset,
            normalOffset,
            uvOffset,
            indexOffset,
            indexComponentType
          )
        );
      }
      layouts = result;
      return stream.ToArray();
    }

    private static byte[] AppendPreviewImages(
      byte[] geometry,
      IReadOnlyList<TexPreview> previews,
      out IReadOnlyDictionary<string, DynamicImageLayout> layouts
    )
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

    private static byte[] AppendAnimationTracks(
      byte[] content,
      IReadOnlyList<DynamicAnimationTrack> tracks,
      out DynamicAnimationLayout? layout
    )
    {
      if (tracks.Count == 0)
      {
        layout = null;
        return content;
      }
      using var stream = new MemoryStream();
      stream.Write(content, 0, content.Length);
      using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
      Align(writer, stream);
      var timeOffset = checked((int)stream.Position);
      writer.Write(0f);
      writer.Write(PreviewDurationSeconds);
      var outputOffsets = new List<int>(tracks.Count);
      foreach (var track in tracks)
      {
        Align(writer, stream);
        outputOffsets.Add(checked((int)stream.Position));
        Write(writer, track.Start);
        Write(writer, track.End);
      }
      layout = new DynamicAnimationLayout(timeOffset, outputOffsets.AsReadOnly());
      return stream.ToArray();
    }

    private static byte[] CreateJson(
      IReadOnlyList<DynamicObjectScope> objects,
      IReadOnlyDictionary<int, DynamicMeshLayout> layouts,
      int binaryLength,
      string? bufferUri,
      IReadOnlyDictionary<int, string> authoringMetadata,
      IReadOnlyDictionary<int, TexPreview> previews,
      IReadOnlyList<TexPreview> previewImages,
      IReadOnlyDictionary<string, DynamicImageLayout> previewLayouts,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews,
      IReadOnlyList<DynamicAnimationTrack> animationTracks,
      DynamicAnimationLayout? animationLayout,
      string? sourceBaseName
    )
    {
      var nodeIndicesById = objects
        .Select((scope, index) => (scope.Id, index: index + 1))
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
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteStartArray("nodes");
        writer.WriteStartObject();
        writer.WriteString("name", sourceBaseName ?? "EarthTool Placement");
        writer.WriteStartArray("children");
        writer.WriteNumberValue(1);
        writer.WriteEndArray();
        writer.WriteStartObject("extras");
        writer.WriteBoolean(GlbDocument.PlacementRootMarker, true);
        writer.WriteEndObject();
        writer.WriteEndObject();
        var previewIndex = 0;
        foreach (var scope in objects)
        {
          var extension = scope.Object.Extension;
          writer.WriteStartObject();
          writer.WriteString(
            "name",
            $"ET_Dynamic_{scope.Id}_{EffectName(extension)}"
          );
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
          if (effectPreviews.TryGetValue(scope.Id, out var nodePreview) && nodePreview.IsScalable)
          {
            writer.WriteStartArray("scale");
            writer.WriteNumberValue(nodePreview.ModelScale);
            writer.WriteNumberValue(nodePreview.ModelScale);
            writer.WriteNumberValue(nodePreview.ModelScale);
            writer.WriteEndArray();
          }
          WriteExtras(writer, authoringMetadata[scope.Id]);
          writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (animationLayout.HasValue)
        {
          var animationAccessorIndex = layouts.Count * 4;
          writer.WriteStartArray("animations");
          writer.WriteStartObject();
          writer.WriteString("name", "EarthTool Dynamic Preview");
          writer.WriteStartArray("samplers");
          for (var index = 0; index < animationTracks.Count; index++)
          {
            writer.WriteStartObject();
            writer.WriteNumber("input", animationAccessorIndex);
            writer.WriteNumber("output", animationAccessorIndex + index + 1);
            writer.WriteString("interpolation", "LINEAR");
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
          writer.WriteStartArray("channels");
          for (var index = 0; index < animationTracks.Count; index++)
          {
            var track = animationTracks[index];
            writer.WriteStartObject();
            writer.WriteNumber("sampler", index);
            writer.WriteStartObject("target");
            writer.WriteNumber("node", nodeIndicesById[track.ObjectId]);
            writer.WriteString("path", track.Path);
            writer.WriteEndObject();
            writer.WriteEndObject();
          }
          writer.WriteEndArray();
          writer.WriteEndObject();
          writer.WriteEndArray();
        }

        if (layouts.Count != 0)
        {
          writer.WriteStartArray("meshes");
          previewIndex = 0;
          var accessorIndex = 0;
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            writer.WriteStartObject();
            writer.WriteString(
              "name",
              sourceBaseName is null
                ? $"ET_{EffectName(scope.Object.Extension)}Preview_{scope.Id}"
                : $"{sourceBaseName}_{scope.Id}_{EffectName(scope.Object.Extension)}_Mesh"
            );
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
              writer.WriteNumber(
                "index",
                previewImages
                  .Select((item, index) => (item, index))
                  .Single(item => item.item.ContentAddress == preview.ContentAddress)
                  .index
              );
              writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteString("alphaMode", "BLEND");
            writer.WriteBoolean("doubleSided", true);
            writer.WriteStartObject("extensions");
            writer.WriteStartObject("KHR_materials_unlit");
            writer.WriteEndObject();
            writer.WriteEndObject();
            if (TryCreateMaterialAuthoringMetadata(extension, out var materialMetadata))
            {
              writer.WriteStartObject("extras");
              writer.WriteString("earthtoolAuthoring", materialMetadata);
              writer.WriteEndObject();
            }
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
              layouts.Count * 4
                + previewImages
                  .Select((item, index) => (item, index))
                  .Single(item => ReferenceEquals(item.item, preview))
                  .index
            );
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

        if (layouts.Count != 0 || animationLayout.HasValue)
        {
          writer.WriteStartArray("bufferViews");
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            var layout = layouts[scope.Id];
            var preview = effectPreviews[scope.Id];
            WriteBufferView(writer, layout.PositionOffset, preview.Positions.Count * 12, 34962);
            WriteBufferView(writer, layout.NormalOffset, preview.Normals.Count * 12, 34962);
            WriteBufferView(
              writer,
              layout.TextureCoordinateOffset,
              preview.TextureCoordinates.Count * 8,
              34962
            );
            WriteBufferView(
              writer,
              layout.IndexOffset,
              preview.Indices.Count * (layout.IndexComponentType == 5125 ? 4 : 2),
              34963
            );
          }
          foreach (var preview in previewImages)
          {
            var layout = previewLayouts[preview.ContentAddress];
            WriteBufferView(writer, layout.Offset, layout.Length, null);
          }
          if (animationLayout.HasValue)
          {
            WriteBufferView(writer, animationLayout.Value.TimeOffset, 2 * sizeof(float), null);
            foreach (var outputOffset in animationLayout.Value.OutputOffsets)
            {
              WriteBufferView(writer, outputOffset, 2 * 3 * sizeof(float), null);
            }
          }
          writer.WriteEndArray();

          writer.WriteStartArray("accessors");
          var bufferViewIndex = 0;
          foreach (var scope in objects.Where(item => layouts.ContainsKey(item.Id)))
          {
            var preview = effectPreviews[scope.Id];
            var layout = layouts[scope.Id];
            var positions = preview.Positions;
            WriteAccessor(
              writer,
              bufferViewIndex,
              5126,
              positions.Count,
              "VEC3",
              new[]
              {
                positions.Min(item => item.X),
                positions.Min(item => item.Y),
                positions.Min(item => item.Z),
              },
              new[]
              {
                positions.Max(item => item.X),
                positions.Max(item => item.Y),
                positions.Max(item => item.Z),
              }
            );
            WriteAccessor(
              writer,
              bufferViewIndex + 1,
              5126,
              preview.Normals.Count,
              "VEC3",
              new[]
              {
                preview.Normals.Min(item => item.X),
                preview.Normals.Min(item => item.Y),
                preview.Normals.Min(item => item.Z),
              },
              new[]
              {
                preview.Normals.Max(item => item.X),
                preview.Normals.Max(item => item.Y),
                preview.Normals.Max(item => item.Z),
              }
            );
            WriteAccessor(
              writer,
              bufferViewIndex + 2,
              5126,
              preview.TextureCoordinates.Count,
              "VEC2",
              null,
              null
            );
            WriteAccessor(
              writer,
              bufferViewIndex + 3,
              layout.IndexComponentType,
              preview.Indices.Count,
              "SCALAR",
              new[] { 0f },
              new[] { (float)preview.Positions.Count - 1 }
            );
            bufferViewIndex += 4;
          }
          if (animationLayout.HasValue)
          {
            var animationBufferViewIndex = layouts.Count * 4 + previewImages.Count;
            WriteAccessor(
              writer,
              animationBufferViewIndex,
              5126,
              2,
              "SCALAR",
              new[] { 0f },
              new[] { PreviewDurationSeconds }
            );
            for (var index = 0; index < animationTracks.Count; index++)
            {
              WriteAccessor(
                writer,
                animationBufferViewIndex + index + 1,
                5126,
                2,
                "VEC3",
                null,
                null
              );
            }
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

    private static IReadOnlyDictionary<int, string> CreateObjectAuthoringMetadata(
      IReadOnlyList<DynamicObjectScope> objects,
      GltfOperationProfile profile)
    {
      if (objects.Count > profile.MaxMetadataEnvelopes)
      {
        throw new MetadataResourceLimitException(objects.Count, profile.MaxMetadataEnvelopes);
      }

      var result = new Dictionary<int, string>();
      long totalBytes = 0;
      var totalElements = 0;
      foreach (var scope in objects)
      {
        var metadata = CreateObjectAuthoringMetadata(scope, profile);
        totalBytes = checked(totalBytes + Encoding.UTF8.GetByteCount(metadata));
        if (totalBytes > profile.MaxTotalMetadataBytes)
        {
          throw new MetadataResourceLimitException(totalBytes, profile.MaxTotalMetadataBytes);
        }
        totalElements = checked(
          totalElements + CountAuthoringMetadataElements(metadata, profile, totalElements)
        );
        if (totalElements > profile.MaxMetadataElements)
        {
          throw new MetadataResourceLimitException(totalElements, profile.MaxMetadataElements);
        }
        result.Add(scope.Id, metadata);
      }
      return result;
    }

    private static string CreateObjectAuthoringMetadata(
      DynamicObjectScope scope,
      GltfOperationProfile profile)
    {
      var extension = scope.Object.Extension;
      var effectType = extension.KnownEffectType
        ?? throw new UnsupportedGltfDomainException("DynamicEffectType");
      var requirements = DynamicEffectBehavior.GetAuthoringRequirements(effectType);
      CanonicalDynamicFrameSequence? frames = null;
      if ((requirements & DynamicAuthoringRequirement.Frames) != 0)
      {
        frames = new CanonicalDynamicFrameSequence(
          extension.FirstSourceFrame,
          extension.FrameCount,
          extension.FramePeriodTicks);
      }
      CanonicalDynamicSpriteSheet? spriteSheet = null;
      if ((requirements & DynamicAuthoringRequirement.SpriteSheet) != 0)
      {
        spriteSheet = new CanonicalDynamicSpriteSheet(
          frames!.Value,
          extension.SpriteSheetColumnCount,
          extension.SpriteSheetRowCount);
      }
      var values = DynamicAuthoringValues.Create(
        frames,
        spriteSheet,
        extension.EndEffectRectangle,
        new CanonicalDynamicTerrainLight(
          extension.KnownLightType ?? DynamicLightType.Constant,
          extension.TerrainLightColor),
        extension.VisibleTerrainLightGain,
        extension.KnownAlphaTiming ?? DynamicAlphaTiming.FramePhase,
        extension.EndAlpha,
        extension.UsesAdditiveBlending,
        MeshResourceKey(extension));
      return CanonicalAuthoringMetadata.Write(
        CanonicalAuthoringOwner.Parse($"ET_Dynamic_{scope.Id}_{EffectName(extension)}"),
        values,
        profile);
    }

    private static string? MeshResourceKey(DynamicEffectExtension extension)
    {
      if (extension.KnownEffectType != DynamicEffectType.ScalableObject
        || extension.MeshNameBytes.Count == 0
        || extension.MeshNameBytes.Any(value => value is 0 or > 0x7F))
      {
        return null;
      }
      return Encoding.ASCII.GetString(extension.MeshNameBytes.ToArray());
    }

    private static bool TryCreateMaterialAuthoringMetadata(
      DynamicEffectExtension extension,
      out string authoringMetadata)
    {
      var bytes = extension.TexturePathBytes;
      if (bytes.Count == 0 || bytes.Any(value => value is 0 or > 0x7F))
      {
        authoringMetadata = string.Empty;
        return false;
      }
      var resourceKey = Encoding.ASCII.GetString(bytes.ToArray());
      if (!AuthoringValidation.IsCanonicalTextureResourceKey(resourceKey))
      {
        authoringMetadata = string.Empty;
        return false;
      }
      authoringMetadata = CanonicalAuthoringMetadata.WriteMaterial(resourceKey, GltfOperationProfile.Default);
      return true;
    }

    private static int CountAuthoringMetadataElements(
      string metadata,
      GltfOperationProfile profile,
      int consumedElements)
    {
      var reader = new Utf8JsonReader(
        Encoding.UTF8.GetBytes(metadata),
        new JsonReaderOptions { MaxDepth = profile.MaxJsonDepth }
      );
      var count = 0;
      try
      {
        while (reader.Read())
        {
          count = checked(count + 1);
          if (consumedElements + count > profile.MaxMetadataElements)
          {
            throw new MetadataResourceLimitException(
              consumedElements + count,
              profile.MaxMetadataElements
            );
          }
        }
      }
      catch (JsonException)
      {
        return count;
      }
      return count;
    }

    private static void WriteExtras(Utf8JsonWriter writer, string authoringMetadata)
    {
      writer.WriteStartObject("extras");
      writer.WriteString("earthtoolAuthoring", authoringMetadata);
      writer.WriteEndObject();
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

    private static void WriteAccessor(
      Utf8JsonWriter writer,
      int bufferView,
      int componentType,
      int count,
      string type,
      float[]? minimum,
      float[]? maximum
    )
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
      int initialDepth = 1
    )
    {
      var objects = new List<DynamicObjectScope>();
      Add(root, initialDepth, objects, profile);
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
      GltfOperationProfile profile
    )
    {
      if (depth > profile.MaxHierarchyDepth)
      {
        throw new ResourceLimitException(depth, profile.MaxHierarchyDepth);
      }
      if (result.Count == profile.MaxNodes)
      {
        throw new ResourceLimitException(result.Count + 1, profile.MaxNodes);
      }
      result.Add(new DynamicObjectScope(result.Count + 1, item));
      foreach (var child in item.Children)
      {
        Add(child, depth + 1, result, profile);
      }
    }

    private static IReadOnlyDictionary<int, DynamicEffectPreview> CreateEffectPreviews(
      IReadOnlyList<DynamicObjectScope> objects,
      GltfOperationProfile profile,
      IReadOnlyDictionary<int, ReferencedMeshPreview> meshPreviews
    )
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
            "The deterministic hierarchy preview requires a finite child start translation."
          );
        }
        if (!HasNativePreview(extension.KnownEffectType))
        {
          continue;
        }
        var preview = CreateEffectPreview(
          scope.Id,
          extension,
          meshPreviews.TryGetValue(scope.Id, out var meshPreview) ? meshPreview : null
        );
        var maximumVertices =
          extension.KnownEffectType == DynamicEffectType.ScalableObject
            ? profile.MaxMeshPreviewVertices
            : profile.MaxActiveRenderVertices;
        if (preview.Positions.Count > maximumVertices)
        {
          throw new ResourceLimitException(preview.Positions.Count, maximumVertices);
        }
        if (extension.KnownEffectType == DynamicEffectType.ScalableObject)
        {
          scalableVertexCount = checked(scalableVertexCount + preview.Positions.Count);
          if (scalableVertexCount > profile.MaxMeshPreviewVertices)
          {
            throw new ResourceLimitException(scalableVertexCount, profile.MaxMeshPreviewVertices);
          }
        }
        result.Add(scope.Id, preview);
      }
      return result;
    }

    private static IReadOnlyList<DynamicAnimationTrack> CreateAnimationTracks(
      IReadOnlyList<DynamicObjectScope> objects,
      IReadOnlyDictionary<int, DynamicEffectPreview> effectPreviews
    )
    {
      var objectsById = objects.ToDictionary(item => item.Id);
      var tracks = new List<DynamicAnimationTrack>();
      foreach (var parent in objects.Where(item => item.Object.Extension.FramePeriodTicks == 0))
      {
        foreach (var childId in parent.ChildIds)
        {
          var child = objectsById[childId];
          var extension = child.Object.Extension;
          if (
            IsFinite(extension.ChildStartTranslation)
            && IsFinite(extension.ChildEndTranslation)
            && extension.ChildStartTranslation != extension.ChildEndTranslation
          )
          {
            tracks.Add(
              new DynamicAnimationTrack(
                child.Id,
                "translation",
                GlbDocument.ProjectToGltf(extension.ChildStartTranslation),
                GlbDocument.ProjectToGltf(extension.ChildEndTranslation)
              )
            );
          }
        }
      }
      foreach (
        var scope in objects.Where(item =>
          item.Object.Extension.KnownEffectType == DynamicEffectType.ScalableObject
          && item.Object.Extension.FramePeriodTicks == 0
        )
      )
      {
        var extension = scope.Object.Extension;
        if (!float.IsFinite(extension.StartModelScale) || !float.IsFinite(extension.EndModelScale))
        {
          throw new DynamicPreviewException(
            $"DynamicObjectScopes[{scope.Id}].Extension.ModelScale",
            "Dynamic scale animation requires finite start and end scales."
          );
        }
        if (extension.StartModelScale != extension.EndModelScale)
        {
          var preview = effectPreviews[scope.Id];
          tracks.Add(
            new DynamicAnimationTrack(
              scope.Id,
              "scale",
              new Vector3(preview.ModelScale),
              new Vector3(extension.EndModelScale)
            )
          );
        }
      }
      return tracks.AsReadOnly();
    }

    private static DynamicEffectPreview CreateEffectPreview(
      int id,
      DynamicEffectExtension extension,
      ReferencedMeshPreview? meshPreview
    )
    {
      if (extension.KnownEffectType == DynamicEffectType.ScalableObject)
      {
        if (
          !DynamicEffectSemantics.TrySelectFrame(
            extension,
            DynamicEffectEvaluationContext.Primary,
            PreviewTotalLifetimeTicks,
            PreviewRemainingLifetimeTicks,
            PreviewGlobalTick,
            out var scalableFrame,
            out var scalableFrameFailure
          )
        )
        {
          throw PreviewFailure(id, "Frames", scalableFrameFailure);
        }
        if (
          !DynamicEffectSemantics.TryInterpolateModelScale(
            extension,
            scalableFrame.Phase,
            out var modelScale,
            out var scaleFailure
          )
        )
        {
          throw PreviewFailure(id, "ModelScale", scaleFailure);
        }
        if (
          !DynamicEffectSemantics.TryInterpolateAlpha(
            extension,
            DynamicEffectEvaluationContext.Primary,
            scalableFrame.Phase,
            PreviewLifetimeProgress,
            out var scalableAlpha,
            out var scalableAlphaFailure
          )
        )
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
          IsUnitRange(scalableAlpha)
        );
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
          0
        );
      }

      if (extension.KnownEffectType == DynamicEffectType.Sphere)
      {
        return CreateSpherePreview(id, extension);
      }

      var context = IsAttachedPreview(extension.KnownEffectType)
        ? DynamicEffectEvaluationContext.AttachedParticle
        : DynamicEffectEvaluationContext.Primary;

      if (
        !DynamicEffectSemantics.TrySelectFrame(
          extension,
          context,
          PreviewTotalLifetimeTicks,
          PreviewRemainingLifetimeTicks,
          PreviewGlobalTick,
          out var frame,
          out var frameFailure
        )
      )
      {
        throw PreviewFailure(id, "Frames", frameFailure);
      }
      if (
        !DynamicEffectSemantics.TryInterpolateAlpha(
          extension,
          context,
          frame.Phase,
          PreviewLifetimeProgress,
          out var alpha,
          out var alphaFailure
        )
      )
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
      else if (
        DynamicEffectSemantics.TrySelectTextureRegion(
          extension,
          context,
          frame,
          PreviewTextureScale,
          out var region,
          out var textureFailure
        )
      )
      {
        var atlasCapacity = (long)extension.SpriteSheetColumnCount * extension.SpriteSheetRowCount;
        if (extension.SpriteSheetRowCount <= 0 || frame.SourceFrame < 0 || frameEnd > atlasCapacity)
        {
          throw PreviewFailure(id, "SpriteSheet", DynamicSemanticFailure.InvalidSpriteSheet);
        }
        textureCoordinates = new[]
        {
          new Vector2(region.U0, region.V1),
          new Vector2(region.U1, region.V1),
          new Vector2(region.U1, region.V0),
          new Vector2(region.U0, region.V0),
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
      else if (
        extension.KnownEffectType == DynamicEffectType.Smoke
        || IsAttachedPreview(extension.KnownEffectType)
      )
      {
        if (
          !DynamicEffectSemantics.TryEvaluateVisibleEffectColor(
            extension,
            context,
            Vector3.One,
            1,
            out color,
            out var colorFailure
          )
        )
        {
          throw PreviewFailure(id, "VisibleEffectColor", colorFailure);
        }
      }
      else
      {
        color = extension.VisibleEffectColor;
      }

      if (
        extension.KnownEffectType
        is DynamicEffectType.MappedExplosion
          or DynamicEffectType.FlatExplosion
          or DynamicEffectType.Laser
          or DynamicEffectType.Lightning
      )
      {
        if (!IsFinite(extension.TerrainLightColor))
        {
          throw new DynamicPreviewException(
            $"DynamicObjectScopes[{id}].Extension.TerrainLightColor",
            "The deterministic terrain-light preview requires a finite color."
          );
        }
        if (
          !DynamicEffectSemantics.TryEvaluateTerrainLightIntensity(
            extension.LightType,
            0,
            PreviewRemainingLifetimeTicks,
            PreviewTotalLifetimeTicks,
            0,
            out _,
            out var lightFailure
          )
        )
        {
          throw PreviewFailure(id, "TerrainLight", lightFailure);
        }
      }

      if (
        extension.KnownEffectType == DynamicEffectType.LaserWall
        && !IsFinite(extension.TerrainLightColor)
      )
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.TerrainLightColor",
          "The deterministic LaserWall terrain-light preview requires a finite color."
        );
      }

      ValidateFiniteMaterial(id, color, alpha);
      if (IsRibbonEffect(extension.KnownEffectType))
      {
        return CreateRibbonPreview(id, extension, color, alpha, textureCoordinates, frame.Phase);
      }

      if (
        !DynamicEffectSemantics.TryInterpolateEffectRectangle(
          extension,
          context,
          frame.Phase,
          out var rectangle,
          out var rectangleFailure
        )
      )
      {
        throw PreviewFailure(id, "EffectRectangle", rectangleFailure);
      }
      var horizontal =
        extension.KnownEffectType
        is DynamicEffectType.Track
          or DynamicEffectType.MappedExplosion
          or DynamicEffectType.FlatExplosion;
      var ownsDepth =
        extension.KnownEffectType is DynamicEffectType.FlatExplosion or DynamicEffectType.Smoke
        || IsAttachedPreview(extension.KnownEffectType);
      var ownsColor =
        extension.KnownEffectType is not DynamicEffectType.Track and not DynamicEffectType.Keelwater
        && IsUnitRange(color);
      var ownsAlpha = IsUnitRange(alpha);
      var depth = ownsDepth ? extension.EffectDepthOffset : 0;
      if (!float.IsFinite(depth))
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.EffectDepthOffset",
          "The deterministic sprite preview requires a finite depth offset."
        );
      }
      var alphaPhase =
        context == DynamicEffectEvaluationContext.AttachedParticle ? frame.Phase
        : extension.UsesLifetimeProgressAlpha ? PreviewLifetimeProgress
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
        alphaPhase
      );
    }

    private static DynamicEffectPreview CreateSpherePreview(
      int id,
      DynamicEffectExtension extension
    )
    {
      if (
        !DynamicEffectSemantics.TrySelectSphereFrame(
          PreviewTotalLifetimeTicks,
          PreviewRemainingLifetimeTicks,
          out _,
          out var frameFailure
        )
      )
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
            MathF.Sin(polar) * MathF.Sin(azimuth)
          );
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
        IsUnitRange(extension.VisibleEffectColor)
      );
    }

    private static DynamicEffectPreview CreateRibbonPreview(
      int id,
      DynamicEffectExtension extension,
      Vector3 color,
      float alpha,
      IReadOnlyList<Vector2> atlasCoordinates,
      float framePhase
    )
    {
      var ribbonHalfWidth = extension.RibbonHalfWidth;
      if (!float.IsFinite(ribbonHalfWidth) || ribbonHalfWidth == 0)
      {
        throw new DynamicPreviewException(
          $"DynamicObjectScopes[{id}].Extension.RibbonHalfWidth",
          "The deterministic ribbon preview requires a finite nonzero ribbon half-width."
        );
      }

      var centers = CreateRibbonCenters(extension.KnownEffectType!.Value);
      var side =
        extension.KnownEffectType == DynamicEffectType.Lightning ? Vector3.UnitX : Vector3.UnitY;
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
      var alphaPhase = extension.UsesLifetimeProgressAlpha ? PreviewLifetimeProgress : framePhase;
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
        alphaPhase
      );
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
      return effectType
        is DynamicEffectType.Laser
          or DynamicEffectType.LaserWall
          or DynamicEffectType.ElectricalCannon
          or DynamicEffectType.Lightning;
    }

    private static Vector2[] CreateLegacyExplosionTextureCoordinates(
      DynamicEffectExtension extension
    )
    {
      if (
        extension.SpriteSheetColumnCount <= 0
        || extension.SpriteSheetRowCount <= 0
        || extension.FirstSourceFrame < 0
      )
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
        new Vector2(left, top),
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
          "The deterministic preview color and alpha must be finite."
        );
      }
    }

    private static bool IsUnitRange(Vector3 value)
    {
      return IsUnitRange(value.X) && IsUnitRange(value.Y) && IsUnitRange(value.Z);
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
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
      DynamicSemanticFailure failure
    )
    {
      return new DynamicPreviewException(
        $"DynamicObjectScopes[{id}].Extension.{field}",
        $"The deterministic sprite preview cannot evaluate this domain ({failure})."
      );
    }

    internal static bool HasNativePreview(DynamicEffectType? effectType)
    {
      return effectType
        is DynamicEffectType.Explosion
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
      return effectType
        is DynamicEffectType.Shockwave
          or DynamicEffectType.Line
          or DynamicEffectType.Keelwater;
    }

    private static void ValidateSupportedEffects(IReadOnlyList<DynamicObjectScope> objects)
    {
      var unsupported = objects.FirstOrDefault(item =>
        item.Object.Extension.KnownEffectType.HasValue
        && item.Object.Extension.KnownEffectType is not DynamicEffectType.Group
        && !HasNativePreview(item.Object.Extension.KnownEffectType)
      );
      if (unsupported is not null)
      {
        throw new UnsupportedGltfDomainException(
          $"DynamicEffect.{unsupported.Object.Extension.KnownEffectType}"
        );
      }
    }

    private static void ValidateGraphBounds(JsonElement root, GltfOperationProfile profile)
    {
      if (
        !root.TryGetProperty("nodes", out var nodes)
        || nodes.ValueKind != JsonValueKind.Array
        || nodes.GetArrayLength() == 0
        || nodes.GetArrayLength() > profile.MaxNodes
      )
      {
        throw new ResourceLimitException(
          root.TryGetProperty("nodes", out nodes) && nodes.ValueKind == JsonValueKind.Array
            ? nodes.GetArrayLength()
            : 0,
          profile.MaxNodes
        );
      }
      if (
        !root.TryGetProperty("scenes", out var scenes)
        || scenes.ValueKind != JsonValueKind.Array
        || scenes.GetArrayLength() != 1
        || !root.TryGetProperty("scene", out var scene)
        || scene.GetInt32() != 0
        || !scenes[0].TryGetProperty("nodes", out var sceneNodes)
        || sceneNodes.ValueKind != JsonValueKind.Array
        || sceneNodes.GetArrayLength() != 1
      )
      {
        throw new InvalidDataException("Dynamic glTF requires one default scene.");
      }
      var sceneRootIndex = sceneNodes[0].GetInt32();
      if (sceneRootIndex < 0 || sceneRootIndex >= nodes.GetArrayLength())
      {
        throw new InvalidDataException("Dynamic glTF contains an invalid scene root.");
      }

      var placementRoot = false;
      for (var index = 0; index < nodes.GetArrayLength(); index++)
      {
        if (!TryGetPlacementRootMarker(nodes[index], out var hasMarker))
        {
          continue;
        }
        if (!hasMarker || placementRoot || index != sceneRootIndex)
        {
          throw new InvalidDataException("Dynamic glTF placement root is malformed.");
        }
        placementRoot = true;
      }
      if (placementRoot)
      {
        var placement = nodes[sceneRootIndex];
        if (
          placement.TryGetProperty("mesh", out _)
          || placement.TryGetProperty("camera", out _)
          || placement.TryGetProperty("skin", out _)
          || placement.TryGetProperty("weights", out _)
          || placement.TryGetProperty("extensions", out _)
          || !placement.TryGetProperty("children", out var placementChildren)
          || placementChildren.GetArrayLength() != 1
        )
        {
          throw new InvalidDataException("Dynamic glTF placement root is malformed.");
        }
        var dynamicRootIndex = placementChildren[0].GetInt32();
        if (dynamicRootIndex < 0 || dynamicRootIndex >= nodes.GetArrayLength())
        {
          throw new InvalidDataException("Dynamic glTF contains an invalid dynamic root.");
        }
      }
      var visited = new HashSet<int>();
      ValidateDepth(nodes, sceneRootIndex, 1, profile.MaxHierarchyDepth, visited);
      if (visited.Count != nodes.GetArrayLength())
      {
        throw new InvalidDataException("Every dynamic glTF node must be reachable exactly once.");
      }
    }

    private static bool TryGetPlacementRootMarker(JsonElement node, out bool value)
    {
      value = false;
      if (
        !node.TryGetProperty("extras", out var extras)
        || !extras.TryGetProperty(GlbDocument.PlacementRootMarker, out var marker)
      )
      {
        return false;
      }
      value = marker.ValueKind == JsonValueKind.True;
      return true;
    }

    private static void ValidateAuthoringMetadataBudgets(
      JsonElement root,
      GltfOperationProfile profile)
    {
      if (!root.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array)
      {
        return;
      }

      var envelopeCount = 0;
      long totalBytes = 0;
      var totalElements = 0;
      foreach (var node in nodes.EnumerateArray())
      {
        if (
          !node.TryGetProperty("extras", out var extras)
          || extras.ValueKind != JsonValueKind.Object
          || !extras.TryGetProperty("earthtoolAuthoring", out var metadata)
        )
        {
          continue;
        }
        if (metadata.ValueKind != JsonValueKind.String || metadata.GetString() is not { } text)
        {
          throw new InvalidDataException("Local EarthTool authoring metadata must be a string.");
        }

        envelopeCount = checked(envelopeCount + 1);
        if (envelopeCount > profile.MaxMetadataEnvelopes)
        {
          throw new MetadataResourceLimitException(envelopeCount, profile.MaxMetadataEnvelopes);
        }
        var bytes = Encoding.UTF8.GetByteCount(text);
        if (bytes > profile.MaxMetadataBytes)
        {
          throw new MetadataResourceLimitException(bytes, profile.MaxMetadataBytes);
        }
        totalBytes = checked(totalBytes + bytes);
        if (totalBytes > profile.MaxTotalMetadataBytes)
        {
          throw new MetadataResourceLimitException(totalBytes, profile.MaxTotalMetadataBytes);
        }
        totalElements = checked(
          totalElements + CountAuthoringMetadataElements(text, profile, totalElements)
        );
        if (totalElements > profile.MaxMetadataElements)
        {
          throw new MetadataResourceLimitException(totalElements, profile.MaxMetadataElements);
        }
      }
    }

    private static void ValidateDepth(
      JsonElement nodes,
      int nodeIndex,
      int depth,
      int maximumDepth,
      ISet<int> visited
    )
    {
      if (nodeIndex < 0 || nodeIndex >= nodes.GetArrayLength() || !visited.Add(nodeIndex))
      {
        throw new InvalidDataException(
          "Dynamic glTF hierarchy contains an invalid or repeated node."
        );
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

    private static string Hash(byte[] bytes)
    {
      using var sha256 = SHA256.Create();
      return BitConverter
        .ToString(sha256.ComputeHash(bytes))
        .Replace("-", string.Empty)
        .ToLowerInvariant();
    }

    private static string EffectName(DynamicEffectExtension extension)
    {
      return extension.KnownEffectType?.ToString() ?? $"Unknown_{extension.EffectType:X8}";
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
        float alphaPhase
      )
      {
        Rectangle = rectangle;
        Depth = depth;
        Color = color;
        Alpha = alpha;
        Positions = Array.AsReadOnly(
          horizontal
            ? new[]
            {
              new Vector3(rectangle.Left, depth, -rectangle.Bottom),
              new Vector3(rectangle.Right, depth, -rectangle.Bottom),
              new Vector3(rectangle.Right, depth, -rectangle.Top),
              new Vector3(rectangle.Left, depth, -rectangle.Top),
            }
            : new[]
            {
              new Vector3(rectangle.Left, rectangle.Bottom, depth),
              new Vector3(rectangle.Right, rectangle.Bottom, depth),
              new Vector3(rectangle.Right, rectangle.Top, depth),
              new Vector3(rectangle.Left, rectangle.Top, depth),
            }
        );
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
        float alphaPhase
      )
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
        bool ownsColor
      )
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
        bool ownsAlpha
      )
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

    private sealed class DynamicAnimationTrack
    {
      internal int ObjectId { get; }
      internal string Path { get; }
      internal Vector3 Start { get; }
      internal Vector3 End { get; }

      internal DynamicAnimationTrack(int objectId, string path, Vector3 start, Vector3 end)
      {
        ObjectId = objectId;
        Path = path;
        Start = start;
        End = end;
      }
    }

    private readonly struct DynamicAnimationLayout
    {
      internal int TimeOffset { get; }
      internal IReadOnlyList<int> OutputOffsets { get; }

      internal DynamicAnimationLayout(int timeOffset, IReadOnlyList<int> outputOffsets)
      {
        TimeOffset = timeOffset;
        OutputOffsets = outputOffsets;
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
        int indexComponentType
      )
      {
        PositionOffset = positionOffset;
        NormalOffset = normalOffset;
        TextureCoordinateOffset = textureCoordinateOffset;
        IndexOffset = indexOffset;
        IndexComponentType = indexComponentType;
      }
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

}
