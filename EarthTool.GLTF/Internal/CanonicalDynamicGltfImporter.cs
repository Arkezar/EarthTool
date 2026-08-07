#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;

namespace EarthTool.GLTF.Internal
{
  internal static class CanonicalDynamicGltfImporter
  {
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunkType = 0x4E4F534A;
    private const uint BinaryChunkType = 0x004E4942;

    internal static bool HasClaim(ReadOnlyMemory<byte> jsonBytes, int maximumDepth)
    {
      try
      {
        using var document = JsonDocument.Parse(
          jsonBytes,
          new JsonDocumentOptions { MaxDepth = maximumDepth }
        );
        return document.RootElement.TryGetProperty("nodes", out var nodes)
          && nodes.ValueKind == JsonValueKind.Array
          && nodes.EnumerateArray().Any(node =>
            node.TryGetProperty("name", out var name)
            && name.ValueKind == JsonValueKind.String
            && name.GetString()!.StartsWith("ET_Dynamic_", StringComparison.OrdinalIgnoreCase)
          );
      }
      catch (JsonException)
      {
        return false;
      }
    }

    internal static OperationResult<DynamicMeshAsset> ImportGlb(
      byte[] glb,
      GltfNewModelImportOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken,
      Guid? creationGuid = null
    )
    {
      cancellationToken.ThrowIfCancellationRequested();
      ValidateGlb(glb, profile);
      var jsonLength = checked((int)ReadUInt32(glb, 12));
      var binaryHeader = checked(20 + jsonLength);
      var binaryLength = checked((int)ReadUInt32(glb, binaryHeader));
      return Import(
        glb.AsMemory(20, jsonLength),
        glb.AsMemory(binaryHeader + 8, binaryLength),
        options,
        profile,
        cancellationToken,
        creationGuid
      );
    }

    internal static OperationResult<DynamicMeshAsset> ImportSeparate(
      byte[] json,
      byte[] binary,
      GltfNewModelImportOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken,
      Guid? creationGuid = null
    )
    {
      if (json.Length + (long)binary.Length > profile.MaxInputBytes)
      {
        throw new ResourceLimitException(json.Length + (long)binary.Length, profile.MaxInputBytes);
      }
      return Import(json, binary, options, profile, cancellationToken, creationGuid);
    }

    private static OperationResult<DynamicMeshAsset> Import(
      ReadOnlyMemory<byte> jsonBytes,
      ReadOnlyMemory<byte> binary,
      GltfNewModelImportOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken,
      Guid? creationGuid = null
    )
    {
      cancellationToken.ThrowIfCancellationRequested();
      using var document = JsonDocument.Parse(
        jsonBytes,
        new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth }
      );
      var root = document.RootElement;
      var graph = ReadGraph(root, profile);
      var nodes = root.GetProperty("nodes");
      var metadata = CanonicalAuthoringMetadata.Read(
        nodes.EnumerateArray().Select((node, index) => new AuthoringMetadataCarrier(
          $"nodes[{index}]",
          node.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()!
            : string.Empty,
          ReadMetadata(node)
        )),
        profile
      );
      if (!metadata.Succeeded)
      {
        return new OperationResult<DynamicMeshAsset>(
          OperationStatus.Failed,
          diagnostics: metadata.Diagnostics
        );
      }

      var canonicalRoot = BuildObject(
        graph.Root,
        metadata.Value!,
        root,
        binary.Span,
        options,
        profile,
        isRoot: true
      );
      var build = DynamicMeshBuilder.Create(creationGuid ?? Guid.NewGuid())
        .SetRoot(canonicalRoot)
        .Build(CreateMshProfile(profile));
      if (!build.TryGetValue(out var asset))
      {
        return new OperationResult<DynamicMeshAsset>(
          OperationStatus.Failed,
          diagnostics: metadata.Diagnostics.Concat(build.Diagnostics)
        );
      }
      return new OperationResult<DynamicMeshAsset>(
        OperationStatus.Succeeded,
        asset,
        metadata.Diagnostics.Concat(build.Diagnostics)
      );
    }

    private static CanonicalDynamicObject BuildObject(
      CanonicalDynamicNode node,
      CanonicalAuthoringMetadataDocument metadata,
      JsonElement root,
      ReadOnlySpan<byte> binary,
      GltfNewModelImportOptions options,
      GltfOperationProfile profile,
      bool isRoot
    )
    {
      var owner = CanonicalAuthoringOwner.Parse(node.Name);
      ValidateNodeTransform(node, owner.EffectType!.Value, isRoot);
      var children = new CanonicalDynamicObject[node.Children.Count];
      for (var index = 0; index < children.Length; index++)
      {
        children[index] = BuildObject(
          node.Children[index],
          metadata,
          root,
          binary,
          options,
          profile,
          isRoot: false
        );
      }
      var values = metadata.Get<DynamicAuthoringValues>(owner);
      var result = owner.EffectType switch
      {
        DynamicEffectType.Group when !node.Element.TryGetProperty("mesh", out _) =>
          DynamicEffectRecipes.Group(children),
        DynamicEffectType.Group => throw new InvalidDataException(
          $"nodes[{node.Index}] Group cannot own visible effect geometry."
        ),
        _ => BuildVisibleEffect(
          node,
          owner.EffectType!.Value,
          values,
          children,
          root,
          binary,
          options,
          profile
        )
      };
      if (!isRoot)
      {
        var translation = ReadTranslation(node.Element);
        var animatedEnd = ReadAnimationEnd(root, binary, node.Index, "translation");
        result.SetChildTranslation(
          translation,
          animatedEnd.HasValue ? FromGltf(animatedEnd.Value) : translation
        );
      }
      return result;
    }

    private static CanonicalDynamicObject BuildVisibleEffect(
      CanonicalDynamicNode node,
      DynamicEffectType effectType,
      DynamicAuthoringValues values,
      IReadOnlyList<CanonicalDynamicObject> children,
      JsonElement root,
      ReadOnlySpan<byte> binary,
      GltfNewModelImportOptions options,
      GltfOperationProfile profile
    )
    {
      var preview = ReadPreview(node, effectType, root, binary, profile);
      var materialHandle = new GltfMaterialHandle(preview.MaterialIndex + 1);
      if (
        !options.TextureResourceBindings.TryGetValue(materialHandle, out var textureResourceKey)
        || textureResourceKey is null
      )
      {
        textureResourceKey = ReadEmbeddedMaterialTextureResourceKey(root, preview.MaterialIndex);
      }
      if (string.IsNullOrEmpty(textureResourceKey))
      {
        throw new RequiredTextureResourceBindingException(
          preview.MaterialIndex,
          materialHandle
        );
      }
      if (!EarthTool.MSH.Authoring.AuthoringValidation.IsCanonicalTextureResourceKey(
        textureResourceKey
      ))
      {
        throw new UnsupportedGltfDomainException("TexResourceBinding");
      }

      var framePhase = values.Frames.HasValue ? PreviewFramePhase(values.Frames.Value) : 0;
      var alpha = values.Frames.HasValue
        ? new CanonicalDynamicAlpha(
          SolveStartValue(
            preview.Alpha,
            values.EndAlpha,
            PreviewAlphaPhase(effectType, values),
            node.Index,
            "alpha"
          ),
          values.EndAlpha,
          values.AlphaTiming
        )
        : new CanonicalDynamicAlpha(preview.Alpha, values.EndAlpha, values.AlphaTiming);
      var startRectangle = values.Frames.HasValue
        ? SolveStartRectangle(
          preview.Rectangle,
          values.EndEffectRectangle,
          framePhase,
          node.Index
        )
        : preview.Rectangle;
      var shape = new CanonicalDynamicEffectShape(
        startRectangle,
        values.EndEffectRectangle,
        preview.Depth
      );
      var visibleColor = effectType
        is DynamicEffectType.Smoke or DynamicEffectType.Shockwave or DynamicEffectType.Line
          ? SolveModulatedColor(preview.Color, values.VisibleTerrainLightGain, node.Index)
          : preview.Color;

      return effectType switch
      {
        DynamicEffectType.Explosion => DynamicEffectRecipes.Explosion(
          values.SpriteSheet!.Value,
          new CanonicalDynamicEffectShape(
            preview.Rectangle,
            values.EndEffectRectangle,
            preview.Depth
          ),
          textureResourceKey,
          preview.Color,
          new CanonicalDynamicAlpha(
            preview.Alpha,
            values.EndAlpha,
            values.AlphaTiming
          ),
          values.Additive,
          values.TerrainLight,
          children
        ),
        DynamicEffectType.Track => DynamicEffectRecipes.Track(
          values.Frames!.Value,
          startRectangle,
          values.EndEffectRectangle,
          textureResourceKey,
          alpha,
          values.Additive,
          children
        ),
        DynamicEffectType.ScalableObject => DynamicEffectRecipes.ScalableObject(
          values.Frames!.Value,
          GetMeshResourceKey(node, values, options),
          textureResourceKey,
          SolveStartValue(
            preview.ModelScale,
            ReadAnimatedScaleEnd(root, binary, node),
            framePhase,
            node.Index,
            "model scale"
          ),
          ReadAnimatedScaleEnd(root, binary, node),
          preview.Color,
          alpha,
          values.Additive,
          values.TerrainLight,
          children
        ),
        DynamicEffectType.MappedExplosion => DynamicEffectRecipes.MappedExplosion(
          values.Frames!.Value,
          startRectangle,
          values.EndEffectRectangle,
          textureResourceKey,
          preview.Color,
          alpha,
          values.Additive,
          values.TerrainLight,
          children
        ),
        DynamicEffectType.FlatExplosion => DynamicEffectRecipes.FlatExplosion(
          values.SpriteSheet!.Value,
          shape,
          textureResourceKey,
          preview.Color,
          alpha,
          values.Additive,
          values.TerrainLight,
          children
        ),
        DynamicEffectType.Laser => DynamicEffectRecipes.Laser(
          values.SpriteSheet!.Value,
          preview.RibbonHalfWidth,
          textureResourceKey,
          preview.Color,
          alpha,
          values.Additive,
          values.TerrainLight,
          children
        ),
        DynamicEffectType.LaserWall => DynamicEffectRecipes.LaserWall(
          values.SpriteSheet!.Value,
          preview.RibbonHalfWidth,
          textureResourceKey,
          preview.Color,
          alpha,
          values.Additive,
          values.TerrainLight.Color,
          children
        ),
        DynamicEffectType.Shockwave => DynamicEffectRecipes.Shockwave(
          values.SpriteSheet!.Value,
          shape,
          textureResourceKey,
          visibleColor,
          values.VisibleTerrainLightGain,
          alpha.StartAlpha,
          alpha.EndAlpha,
          values.Additive,
          children
        ),
        DynamicEffectType.Line => DynamicEffectRecipes.Line(
          values.SpriteSheet!.Value,
          shape,
          textureResourceKey,
          visibleColor,
          values.VisibleTerrainLightGain,
          alpha.StartAlpha,
          alpha.EndAlpha,
          values.Additive,
          children
        ),
        DynamicEffectType.Sphere => DynamicEffectRecipes.Sphere(
          textureResourceKey,
          preview.Color,
          values.Additive,
          children
        ),
        DynamicEffectType.ElectricalCannon => DynamicEffectRecipes.ElectricalCannon(
          values.SpriteSheet!.Value,
          preview.RibbonHalfWidth,
          textureResourceKey,
          preview.Color,
          alpha,
          values.Additive,
          children
        ),
        DynamicEffectType.Lightning => DynamicEffectRecipes.Lightning(
          values.SpriteSheet!.Value,
          preview.RibbonHalfWidth,
          textureResourceKey,
          preview.Color,
          alpha,
          values.Additive,
          values.TerrainLight,
          children
        ),
        DynamicEffectType.Smoke => DynamicEffectRecipes.Smoke(
          values.SpriteSheet!.Value,
          shape,
          textureResourceKey,
          visibleColor,
          values.VisibleTerrainLightGain,
          alpha,
          values.Additive,
          children
        ),
        DynamicEffectType.Keelwater => DynamicEffectRecipes.Keelwater(
          values.SpriteSheet!.Value,
          shape,
          textureResourceKey,
          alpha.StartAlpha,
          alpha.EndAlpha,
          values.Additive,
          children
        ),
        _ => throw new UnsupportedGltfDomainException($"DynamicEffect.{effectType}")
      };
    }

    private static string GetMeshResourceKey(
      CanonicalDynamicNode node,
      DynamicAuthoringValues values,
      GltfNewModelImportOptions options
    )
    {
      var handle = new GltfNodeHandle(node.Index + 1);
      if (options.MeshResourceBindings.TryGetValue(handle, out var overrideValue)
        && !string.IsNullOrEmpty(overrideValue))
      {
        return overrideValue;
      }
      if (!string.IsNullOrEmpty(values.MeshResourceKey))
      {
        return values.MeshResourceKey;
      }
      throw new UnsupportedGltfDomainException("MeshResourceBinding");
    }

    private static float ReadAnimatedScaleEnd(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      CanonicalDynamicNode node
    )
    {
      var animated = ReadAnimationEnd(root, binary, node.Index, "scale");
      if (animated.HasValue)
      {
        return ReadUniformScale(animated.Value, $"nodes[{node.Index}] scale animation");
      }
      return ReadNodeScale(node.Element, node.Index);
    }

    private static Vector3? ReadAnimationEnd(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int nodeIndex,
      string path
    )
    {
      if (!root.TryGetProperty("animations", out var animations))
      {
        return null;
      }
      Vector3? result = null;
      foreach (var animation in animations.EnumerateArray())
      {
        var samplers = animation.GetProperty("samplers");
        foreach (var channel in animation.GetProperty("channels").EnumerateArray())
        {
          var target = channel.GetProperty("target");
          if (target.GetProperty("node").GetInt32() != nodeIndex
            || target.GetProperty("path").GetString() != path)
          {
            continue;
          }
          if (result.HasValue)
          {
            throw new InvalidDataException(
              $"nodes[{nodeIndex}] has duplicate {path} animation evidence."
            );
          }
          var samplerIndex = channel.GetProperty("sampler").GetInt32();
          if (samplerIndex < 0 || samplerIndex >= samplers.GetArrayLength())
          {
            throw new InvalidDataException("A canonical dynamic animation sampler is invalid.");
          }
          var sampler = samplers[samplerIndex];
          if (sampler.TryGetProperty("interpolation", out var interpolation)
            && interpolation.GetString() != "LINEAR")
          {
            throw new InvalidDataException(
              "Canonical dynamic animation evidence must use linear interpolation."
            );
          }
          var values = ReadVector3Accessor(
            root,
            binary,
            sampler.GetProperty("output").GetInt32()
          );
          if (values.Length != 2 || values.Any(value => !IsFinite(value)))
          {
            throw new InvalidDataException(
              "Canonical dynamic animation evidence requires two finite values."
            );
          }
          result = values[1];
        }
      }
      return result;
    }

    private static Vector3 SolveModulatedColor(Vector3 value, float gain, int nodeIndex)
    {
      var factor = Math.Min(1, gain);
      if (!float.IsFinite(factor) || factor == 0)
      {
        throw new InvalidDataException(
          $"nodes[{nodeIndex}] visible color cannot be inverted through its typed gain."
        );
      }
      var result = value / factor;
      if (!IsFinite(result))
      {
        throw new InvalidDataException(
          $"nodes[{nodeIndex}] visible color produces a non-finite authored value."
        );
      }
      return result;
    }

    private static float PreviewFramePhase(CanonicalDynamicFrameSequence frames)
    {
      return frames.FramePeriodTicks == 0 ? 1f / 100f : 0;
    }

    private static float PreviewAlphaPhase(
      DynamicEffectType effectType,
      DynamicAuthoringValues values
    )
    {
      if (
        effectType
        is DynamicEffectType.Shockwave
          or DynamicEffectType.Line
          or DynamicEffectType.Keelwater
      )
      {
        return PreviewFramePhase(values.Frames!.Value);
      }
      return values.AlphaTiming == DynamicAlphaTiming.LifetimeProgress
        ? 0
        : PreviewFramePhase(values.Frames!.Value);
    }

    private static EffectRectangle SolveStartRectangle(
      EffectRectangle value,
      EffectRectangle end,
      float phase,
      int nodeIndex
    )
    {
      return new EffectRectangle(
        SolveStartValue(value.X0, end.X0, phase, nodeIndex, "rectangle"),
        SolveStartValue(value.Y1, end.Y1, phase, nodeIndex, "rectangle"),
        SolveStartValue(value.X1, end.X1, phase, nodeIndex, "rectangle"),
        SolveStartValue(value.Y0, end.Y0, phase, nodeIndex, "rectangle")
      );
    }

    private static float SolveStartValue(
      float value,
      float end,
      float phase,
      int nodeIndex,
      string field
    )
    {
      var denominator = 1 - phase;
      var result = (value - end * phase) / denominator;
      if (denominator == 0 || !float.IsFinite(result))
      {
        throw new InvalidDataException(
          $"nodes[{nodeIndex}] visible {field} cannot be mapped to an authored start value."
        );
      }
      return result;
    }

    private static CanonicalDynamicPreview ReadPreview(
      CanonicalDynamicNode node,
      DynamicEffectType effectType,
      JsonElement root,
      ReadOnlySpan<byte> binary,
      GltfOperationProfile profile
    )
    {
      if (
        !node.Element.TryGetProperty("mesh", out var meshElement)
        || !root.TryGetProperty("meshes", out var meshes)
      )
      {
        throw new InvalidDataException($"nodes[{node.Index}] requires visible effect geometry.");
      }
      var meshIndex = meshElement.GetInt32();
      if (meshIndex < 0 || meshIndex >= meshes.GetArrayLength())
      {
        throw new InvalidDataException($"nodes[{node.Index}].mesh is invalid.");
      }
      var primitives = meshes[meshIndex].GetProperty("primitives");
      if (primitives.GetArrayLength() != 1)
      {
        throw new InvalidDataException(
          $"nodes[{node.Index}] requires exactly one visible effect primitive."
        );
      }
      var primitive = primitives[0];
      var materialIndex = primitive.GetProperty("material").GetInt32();
      var color = ReadMaterialColor(root, materialIndex);
      var positionAccessor = primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32();
      var positionCount = ReadAccessorCount(root, positionAccessor, "VEC3");
      var maximumPositions = effectType == DynamicEffectType.ScalableObject
        ? profile.MaxMeshPreviewVertices
        : profile.MaxActiveRenderVertices;
      if (positionCount > maximumPositions)
      {
        throw new ResourceLimitException(positionCount, maximumPositions);
      }
      if (effectType == DynamicEffectType.ScalableObject)
      {
        return CanonicalDynamicPreview.Scalable(
          materialIndex,
          new Vector3(color[0], color[1], color[2]),
          color[3],
          ReadNodeScale(node.Element, node.Index)
        );
      }
      if (effectType == DynamicEffectType.Sphere)
      {
        return CanonicalDynamicPreview.Material(
          materialIndex,
          new Vector3(color[0], color[1], color[2]),
          color[3]
        );
      }
      var positions = ReadVector3Accessor(
        root,
        binary,
        positionAccessor
      );
      if (IsRibbonEffect(effectType))
      {
        if (positions.Length < 4 || positions.Length % 2 != 0
          || positions.Any(position => !IsFinite(position)))
        {
          throw new InvalidDataException(
            $"nodes[{node.Index}] ribbon evidence requires finite vertex pairs."
          );
        }
        var indices = ReadIndices(
          root,
          binary,
          primitive.GetProperty("indices").GetInt32()
        );
        if (indices.Length < 3
          || indices[0] >= positions.Length
          || indices[1] >= positions.Length
          || indices[2] >= positions.Length)
        {
          throw new InvalidDataException($"nodes[{node.Index}] ribbon indices are invalid.");
        }
        var winding = Vector3.Cross(
          positions[indices[1]] - positions[indices[0]],
          positions[indices[2]] - positions[indices[0]]
        ).Z;
        if (!float.IsFinite(winding) || winding == 0)
        {
          throw new InvalidDataException($"nodes[{node.Index}] ribbon winding is degenerate.");
        }
        var halfWidth = Vector3.Distance(positions[0], positions[1]) / 2
          * -Math.Sign(winding);
        return CanonicalDynamicPreview.Ribbon(
          materialIndex,
          new Vector3(color[0], color[1], color[2]),
          color[3],
          halfWidth
        );
      }
      if (positions.Length != 4 || positions.Any(position => !IsFinite(position)))
      {
        throw new InvalidDataException(
          $"nodes[{node.Index}] visible effect geometry must be one finite quad."
        );
      }

      var horizontal = effectType is DynamicEffectType.Track
        or DynamicEffectType.MappedExplosion
        or DynamicEffectType.FlatExplosion;
      EffectRectangle rectangle;
      float depth;
      if (horizontal)
      {
        if (
          positions[0].X != positions[3].X
          || positions[1].X != positions[2].X
          || positions[0].Z != positions[1].Z
          || positions[2].Z != positions[3].Z
          || positions.Any(position => position.Y != positions[0].Y)
        )
        {
          throw new InvalidDataException(
            $"nodes[{node.Index}] visible terrain quad must remain axis aligned."
          );
        }
        rectangle = new EffectRectangle(
          positions[0].X,
          -positions[2].Z,
          positions[1].X,
          -positions[0].Z
        );
        depth = positions[0].Y;
      }
      else
      {
        if (
          positions[0].X != positions[3].X
          || positions[1].X != positions[2].X
          || positions[0].Y != positions[1].Y
          || positions[2].Y != positions[3].Y
          || positions.Any(position => position.Z != positions[0].Z)
        )
        {
          throw new InvalidDataException(
            $"nodes[{node.Index}] visible billboard quad must remain axis aligned."
          );
        }
        rectangle = new EffectRectangle(
          positions[0].X,
          positions[2].Y,
          positions[1].X,
          positions[0].Y
        );
        depth = positions[0].Z;
      }
      return new CanonicalDynamicPreview(
        materialIndex,
        rectangle,
        depth,
        new Vector3(color[0], color[1], color[2]),
        color[3]
      );
    }

    private static bool IsRibbonEffect(DynamicEffectType effectType)
    {
      return effectType
        is DynamicEffectType.Laser
          or DynamicEffectType.LaserWall
          or DynamicEffectType.ElectricalCannon
          or DynamicEffectType.Lightning;
    }

    private static float ReadNodeScale(JsonElement node, int nodeIndex)
    {
      if (!node.TryGetProperty("scale", out var scale))
      {
        return 1;
      }
      var values = scale.EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (values.Length != 3 || values.Any(value => !float.IsFinite(value)))
      {
        throw new InvalidDataException($"nodes[{nodeIndex}] scale must be finite and uniform.");
      }
      return ReadUniformScale(new Vector3(values[0], values[1], values[2]), $"nodes[{nodeIndex}]");
    }

    private static float ReadUniformScale(Vector3 scale, string path)
    {
      if (!IsFinite(scale) || scale.X != scale.Y || scale.X != scale.Z)
      {
        throw new InvalidDataException(path + " scale must be finite and uniform.");
      }
      return scale.X;
    }

    private static void ValidateNodeTransform(
      CanonicalDynamicNode node,
      DynamicEffectType effectType,
      bool isRoot
    )
    {
      if (node.Element.TryGetProperty("matrix", out _))
      {
        throw new InvalidDataException(
          $"nodes[{node.Index}] matrix cannot author canonical dynamic semantics."
        );
      }
      if (node.Element.TryGetProperty("rotation", out var rotation))
      {
        var values = rotation.EnumerateArray().Select(item => item.GetSingle()).ToArray();
        if (values.Length != 4
          || !values.SequenceEqual(new[] { 0f, 0f, 0f, 1f }))
        {
          throw new InvalidDataException(
            $"nodes[{node.Index}] rotation cannot author canonical dynamic semantics."
          );
        }
      }
      if (effectType != DynamicEffectType.ScalableObject
        && node.Element.TryGetProperty("scale", out var scale))
      {
        var values = scale.EnumerateArray().Select(item => item.GetSingle()).ToArray();
        if (values.Length != 3 || !values.SequenceEqual(new[] { 1f, 1f, 1f }))
        {
          throw new InvalidDataException(
            $"nodes[{node.Index}] scale is unsupported for {effectType}."
          );
        }
      }
      if (isRoot && ReadTranslation(node.Element) != Vector3.Zero)
      {
        throw new InvalidDataException(
          $"nodes[{node.Index}] root translation must be authored on the placement root."
        );
      }
    }

    private static string? ReadEmbeddedMaterialTextureResourceKey(
      JsonElement root,
      int materialIndex)
    {
      if (
        !root.TryGetProperty("materials", out var materials)
        || materialIndex < 0
        || materialIndex >= materials.GetArrayLength()
        || !materials[materialIndex].TryGetProperty("extras", out var extras)
        || extras.ValueKind != JsonValueKind.Object
        || !extras.TryGetProperty("earthtoolAuthoring", out var metadata)
        || metadata.ValueKind != JsonValueKind.String)
      {
        return null;
      }
      return CanonicalAuthoringMetadata.ReadMaterialTextureResourceKey(metadata.GetString());
    }

    private static float[] ReadMaterialColor(JsonElement root, int materialIndex)
    {
      if (
        !root.TryGetProperty("materials", out var materials)
        || materialIndex < 0
        || materialIndex >= materials.GetArrayLength()
      )
      {
        throw new InvalidDataException("A canonical dynamic material reference is invalid.");
      }
      var values = materials[materialIndex]
        .GetProperty("pbrMetallicRoughness")
        .GetProperty("baseColorFactor")
        .EnumerateArray()
        .Select(item => item.GetSingle())
        .ToArray();
      if (values.Length != 4 || values.Any(value => !float.IsFinite(value)))
      {
        throw new InvalidDataException(
          "A canonical dynamic material color must contain four finite values."
        );
      }
      return values;
    }

    private static Vector3[] ReadVector3Accessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex
    )
    {
      var values = ReadFloatAccessor(root, binary, accessorIndex, "VEC3", 3);
      var result = new Vector3[values.Length / 3];
      for (var index = 0; index < result.Length; index++)
      {
        result[index] = new Vector3(
          values[index * 3],
          values[index * 3 + 1],
          values[index * 3 + 2]
        );
      }
      return result;
    }

    private static int ReadAccessorCount(JsonElement root, int accessorIndex, string expectedType)
    {
      var accessors = root.GetProperty("accessors");
      if (accessorIndex < 0 || accessorIndex >= accessors.GetArrayLength())
      {
        throw new InvalidDataException("A canonical dynamic accessor reference is invalid.");
      }
      var accessor = accessors[accessorIndex];
      if (accessor.GetProperty("componentType").GetInt32() != 5126
        || accessor.GetProperty("type").GetString() != expectedType)
      {
        throw new InvalidDataException("A canonical dynamic accessor representation is invalid.");
      }
      var count = accessor.GetProperty("count").GetInt32();
      if (count <= 0)
      {
        throw new InvalidDataException("A canonical dynamic accessor must not be empty.");
      }
      return count;
    }

    private static int[] ReadIndices(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex
    )
    {
      var accessors = root.GetProperty("accessors");
      if (accessorIndex < 0 || accessorIndex >= accessors.GetArrayLength())
      {
        throw new InvalidDataException("A canonical dynamic index accessor is invalid.");
      }
      var accessor = accessors[accessorIndex];
      var componentType = accessor.GetProperty("componentType").GetInt32();
      var componentSize = componentType == 5123 ? sizeof(ushort)
        : componentType == 5125 ? sizeof(uint)
        : throw new InvalidDataException("A canonical dynamic index type is unsupported.");
      if (accessor.GetProperty("type").GetString() != "SCALAR")
      {
        throw new InvalidDataException("A canonical dynamic index accessor must be scalar.");
      }
      var views = root.GetProperty("bufferViews");
      var view = views[accessor.GetProperty("bufferView").GetInt32()];
      var count = accessor.GetProperty("count").GetInt32();
      var stride = view.TryGetProperty("byteStride", out var strideElement)
        ? strideElement.GetInt32()
        : componentSize;
      var offset = (view.TryGetProperty("byteOffset", out var viewOffset)
          ? viewOffset.GetInt32()
          : 0)
        + (accessor.TryGetProperty("byteOffset", out var accessorOffset)
          ? accessorOffset.GetInt32()
          : 0);
      var end = checked(offset + (count - 1) * stride + componentSize);
      if (count <= 0 || stride < componentSize || offset < 0 || end > binary.Length)
      {
        throw new InvalidDataException("Canonical dynamic index bytes are out of bounds.");
      }
      var result = new int[count];
      for (var index = 0; index < count; index++)
      {
        var value = binary.Slice(offset + index * stride, componentSize);
        result[index] = componentType == 5123
          ? BinaryPrimitives.ReadUInt16LittleEndian(value)
          : checked((int)BinaryPrimitives.ReadUInt32LittleEndian(value));
      }
      return result;
    }

    private static float[] ReadFloatAccessor(
      JsonElement root,
      ReadOnlySpan<byte> binary,
      int accessorIndex,
      string expectedType,
      int componentCount
    )
    {
      var accessors = root.GetProperty("accessors");
      if (accessorIndex < 0 || accessorIndex >= accessors.GetArrayLength())
      {
        throw new InvalidDataException("A canonical dynamic accessor reference is invalid.");
      }
      var accessor = accessors[accessorIndex];
      if (
        accessor.GetProperty("componentType").GetInt32() != 5126
        || accessor.GetProperty("type").GetString() != expectedType
      )
      {
        throw new InvalidDataException("A canonical dynamic accessor representation is invalid.");
      }
      var views = root.GetProperty("bufferViews");
      var viewIndex = accessor.GetProperty("bufferView").GetInt32();
      if (viewIndex < 0 || viewIndex >= views.GetArrayLength())
      {
        throw new InvalidDataException("A canonical dynamic buffer-view reference is invalid.");
      }
      var view = views[viewIndex];
      if (view.GetProperty("buffer").GetInt32() != 0)
      {
        throw new InvalidDataException("Canonical dynamic evidence must use the package buffer.");
      }
      var count = accessor.GetProperty("count").GetInt32();
      var elementSize = componentCount * sizeof(float);
      var stride = view.TryGetProperty("byteStride", out var strideElement)
        ? strideElement.GetInt32()
        : elementSize;
      var offset = (view.TryGetProperty("byteOffset", out var viewOffset)
          ? viewOffset.GetInt32()
          : 0)
        + (accessor.TryGetProperty("byteOffset", out var accessorOffset)
          ? accessorOffset.GetInt32()
          : 0);
      var end = checked(offset + (count - 1) * stride + elementSize);
      if (count <= 0 || stride < elementSize || offset < 0 || end > binary.Length)
      {
        throw new InvalidDataException("Canonical dynamic accessor bytes are out of bounds.");
      }
      var result = new float[count * componentCount];
      for (var element = 0; element < count; element++)
      {
        for (var component = 0; component < componentCount; component++)
        {
          var bits = BinaryPrimitives.ReadInt32LittleEndian(
            binary.Slice(offset + element * stride + component * sizeof(float), sizeof(float))
          );
          result[element * componentCount + component] = BitConverter.Int32BitsToSingle(bits);
        }
      }
      return result;
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    private static CanonicalDynamicGraph ReadGraph(JsonElement root, GltfOperationProfile profile)
    {
      if (
        !root.TryGetProperty("nodes", out var nodes)
        || nodes.ValueKind != JsonValueKind.Array
        || nodes.GetArrayLength() == 0
        || nodes.GetArrayLength() > profile.MaxNodes
        || !root.TryGetProperty("scenes", out var scenes)
        || scenes.ValueKind != JsonValueKind.Array
        || scenes.GetArrayLength() != 1
        || !root.TryGetProperty("scene", out var scene)
        || scene.GetInt32() != 0
        || !scenes[0].TryGetProperty("nodes", out var sceneNodes)
        || sceneNodes.ValueKind != JsonValueKind.Array
        || sceneNodes.GetArrayLength() != 1
      )
      {
        throw new InvalidDataException("Canonical dynamic glTF requires one bounded default scene.");
      }

      var sceneRootIndex = sceneNodes[0].GetInt32();
      ValidateNodeIndex(nodes, sceneRootIndex);
      var sceneRoot = nodes[sceneRootIndex];
      var dynamicRootIndex = sceneRootIndex;
      var placementRoot = IsPlacementRoot(sceneRoot);
      if (placementRoot)
      {
        if (
          !sceneRoot.TryGetProperty("children", out var placementChildren)
          || placementChildren.ValueKind != JsonValueKind.Array
          || placementChildren.GetArrayLength() != 1
        )
        {
          throw new InvalidDataException("Canonical dynamic glTF placement root is malformed.");
        }
        dynamicRootIndex = placementChildren[0].GetInt32();
      }

      var visited = new HashSet<int>();
      var all = new List<CanonicalDynamicNode>();
      var dynamicRoot = ReadNode(
        nodes,
        dynamicRootIndex,
        1,
        profile.MaxHierarchyDepth,
        visited,
        all
      );
      var expectedCount = nodes.GetArrayLength() - (placementRoot ? 1 : 0);
      if (visited.Count != expectedCount)
      {
        throw new InvalidDataException(
          "Every canonical dynamic glTF object must be reachable exactly once."
        );
      }
      return new CanonicalDynamicGraph(dynamicRoot, all.AsReadOnly());
    }

    private static CanonicalDynamicNode ReadNode(
      JsonElement nodes,
      int index,
      int depth,
      int maximumDepth,
      ISet<int> visited,
      ICollection<CanonicalDynamicNode> all
    )
    {
      ValidateNodeIndex(nodes, index);
      if (!visited.Add(index))
      {
        throw new InvalidDataException(
          "Canonical dynamic glTF hierarchy contains a repeated object."
        );
      }
      if (depth > maximumDepth)
      {
        throw new ResourceLimitException(depth, maximumDepth);
      }

      var element = nodes[index];
      var name = element.TryGetProperty("name", out var nameElement)
        && nameElement.ValueKind == JsonValueKind.String
          ? nameElement.GetString()!
          : string.Empty;
      if (!CanonicalAuthoringOwner.TryParse(name, out var owner)
        || owner.Kind != CanonicalAuthoringOwnerKind.DynamicObject)
      {
        throw new InvalidDataException(
          $"nodes[{index}] requires an exact recognized canonical dynamic name."
        );
      }

      var children = new List<CanonicalDynamicNode>();
      if (element.TryGetProperty("children", out var childElements))
      {
        if (childElements.ValueKind != JsonValueKind.Array)
        {
          throw new InvalidDataException($"nodes[{index}].children must be an array.");
        }
        foreach (var child in childElements.EnumerateArray())
        {
          children.Add(
            ReadNode(
              nodes,
              child.GetInt32(),
              depth + 1,
              maximumDepth,
              visited,
              all
            )
          );
        }
      }
      var result = new CanonicalDynamicNode(index, name, element, children.AsReadOnly());
      all.Add(result);
      return result;
    }

    private static Vector3 ReadTranslation(JsonElement node)
    {
      if (!node.TryGetProperty("translation", out var translation))
      {
        return Vector3.Zero;
      }
      var values = translation.EnumerateArray().Select(item => item.GetSingle()).ToArray();
      if (values.Length != 3 || values.Any(value => !float.IsFinite(value)))
      {
        throw new InvalidDataException("A canonical dynamic child translation must be finite.");
      }
      return FromGltf(new Vector3(values[0], values[1], values[2]));
    }

    private static Vector3 FromGltf(Vector3 value)
    {
      return new Vector3(value.X, -value.Z, value.Y);
    }

    private static string? ReadMetadata(JsonElement node)
    {
      if (
        !node.TryGetProperty("extras", out var extras)
        || extras.ValueKind != JsonValueKind.Object
        || !extras.TryGetProperty("earthtoolAuthoring", out var metadata)
      )
      {
        return null;
      }
      return metadata.ValueKind == JsonValueKind.String
        ? metadata.GetString()
        : throw new InvalidDataException("Local EarthTool authoring metadata must be a string.");
    }

    private static bool IsPlacementRoot(JsonElement node)
    {
      return node.TryGetProperty("extras", out var extras)
        && extras.ValueKind == JsonValueKind.Object
        && extras.TryGetProperty(GlbDocument.PlacementRootMarker, out var marker)
        && marker.ValueKind == JsonValueKind.True;
    }

    private static void ValidateNodeIndex(JsonElement nodes, int index)
    {
      if (index < 0 || index >= nodes.GetArrayLength())
      {
        throw new InvalidDataException("Canonical dynamic glTF contains an invalid node reference.");
      }
    }

    private static void ValidateGlb(byte[] glb, GltfOperationProfile profile)
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
        throw new InvalidDataException("Invalid canonical dynamic GLB header.");
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
        throw new InvalidDataException("Invalid canonical dynamic GLB chunks.");
      }
    }

    private static uint ReadUInt32(byte[] source, int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(offset, sizeof(uint)));
    }

    private static MshOperationProfile CreateMshProfile(GltfOperationProfile profile)
    {
      return new MshOperationProfile(
        maxInputBytes: profile.MaxInputBytes,
        maxOutputBytes: profile.MaxOutputBytes,
        maxDynamicDepth: profile.MaxHierarchyDepth,
        maxDynamicObjects: profile.MaxNodes,
        maxDynamicChildrenPerObject: profile.MaxNodes
      );
    }

    private sealed class CanonicalDynamicGraph
    {
      internal CanonicalDynamicNode Root { get; }
      internal IReadOnlyList<CanonicalDynamicNode> Nodes { get; }

      internal CanonicalDynamicGraph(
        CanonicalDynamicNode root,
        IReadOnlyList<CanonicalDynamicNode> nodes
      )
      {
        Root = root;
        Nodes = nodes;
      }
    }

    private sealed class CanonicalDynamicNode
    {
      internal int Index { get; }
      internal string Name { get; }
      internal JsonElement Element { get; }
      internal IReadOnlyList<CanonicalDynamicNode> Children { get; }

      internal CanonicalDynamicNode(
        int index,
        string name,
        JsonElement element,
        IReadOnlyList<CanonicalDynamicNode> children
      )
      {
        Index = index;
        Name = name;
        Element = element;
        Children = children;
      }
    }

    private sealed class CanonicalDynamicPreview
    {
      internal int MaterialIndex { get; }
      internal EffectRectangle Rectangle { get; }
      internal float Depth { get; }
      internal Vector3 Color { get; }
      internal float Alpha { get; }
      internal float RibbonHalfWidth { get; }
      internal float ModelScale { get; }

      internal CanonicalDynamicPreview(
        int materialIndex,
        EffectRectangle rectangle,
        float depth,
        Vector3 color,
        float alpha,
        float ribbonHalfWidth = 0,
        float modelScale = 0
      )
      {
        MaterialIndex = materialIndex;
        Rectangle = rectangle;
        Depth = depth;
        Color = color;
        Alpha = alpha;
        RibbonHalfWidth = ribbonHalfWidth;
        ModelScale = modelScale;
      }

      internal static CanonicalDynamicPreview Material(
        int materialIndex,
        Vector3 color,
        float alpha
      )
      {
        return new CanonicalDynamicPreview(
          materialIndex,
          default,
          0,
          color,
          alpha
        );
      }

      internal static CanonicalDynamicPreview Ribbon(
        int materialIndex,
        Vector3 color,
        float alpha,
        float ribbonHalfWidth
      )
      {
        return new CanonicalDynamicPreview(
          materialIndex,
          default,
          0,
          color,
          alpha,
          ribbonHalfWidth
        );
      }

      internal static CanonicalDynamicPreview Scalable(
        int materialIndex,
        Vector3 color,
        float alpha,
        float modelScale
      )
      {
        return new CanonicalDynamicPreview(
          materialIndex,
          default,
          0,
          color,
          alpha,
          modelScale: modelScale
        );
      }
    }
  }
}
