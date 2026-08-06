#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;

namespace EarthTool.MSH.Internal
{
  internal sealed class CanonicalStaticMeshAssemblyInput
  {
    internal Guid CreationGuid { get; }

    internal CanonicalStaticBaseHeaderInput BaseHeader { get; }

    internal CanonicalStaticSourceObject RootSourceObject { get; }

    internal IReadOnlyDictionary<int, Vector3> Pivots { get; }

    internal IReadOnlyDictionary<int, StaticAnimationReplacement> Animations { get; }

    internal IReadOnlyDictionary<int, string?> TextureResourceBindings { get; }

    internal CanonicalStaticMeshAssemblyInput(
      Guid creationGuid,
      CanonicalStaticBaseHeaderInput baseHeader,
      CanonicalStaticSourceObject rootSourceObject,
      IReadOnlyDictionary<int, Vector3>? pivots = null,
      IReadOnlyDictionary<int, StaticAnimationReplacement>? animations = null,
      IReadOnlyDictionary<int, string?>? textureResourceBindings = null
    )
    {
      CreationGuid = creationGuid;
      RootSourceObject =
        rootSourceObject ?? throw new ArgumentNullException(nameof(rootSourceObject));
      BaseHeader = baseHeader ?? throw new ArgumentNullException(nameof(baseHeader));
      Pivots = Copy(pivots);
      Animations = Copy(animations);
      TextureResourceBindings = Copy(textureResourceBindings);
    }

    private static IReadOnlyDictionary<int, T> Copy<T>(
      IReadOnlyDictionary<int, T>? values
    )
    {
      var result = new Dictionary<int, T>();
      if (values is null)
      {
        return new ReadOnlyDictionary<int, T>(result);
      }

      foreach (var value in values)
      {
        result.Add(value.Key, value.Value);
      }
      return new ReadOnlyDictionary<int, T>(result);
    }

  }

  internal static class CanonicalStaticMeshAssembler
  {
    internal static MshBuildResult<StaticMeshAsset> Assemble(
      CanonicalStaticMeshAssemblyInput input,
      MshOperationProfile? profile = null
    )
    {
      if (input is null)
      {
        throw new ArgumentNullException(nameof(input));
      }
      profile ??= MshOperationProfile.Default;

      var failure = AuthoringValidation.ValidateStaticTree(input.RootSourceObject, profile);
      if (failure is not null)
      {
        return Failed(failure);
      }
      var renderObjects = CanonicalStaticRenderObjectSequenceEncoder.GetCanonicalSequence(
        input.RootSourceObject
      );
      failure = ValidateBaseHeader(input.BaseHeader);
      if (failure is not null)
      {
        return Failed(failure);
      }
      failure = AuthoringValidation.ValidateStaticHeader(
        input.BaseHeader.Vertices,
        input.BaseHeader.Footprint,
        input.BaseHeader.HorizontalExtents
      );
      if (failure is not null)
      {
        return Failed(failure);
      }

      failure = ValidateCompleteInput(input, renderObjects.Count, profile);
      if (failure is not null)
      {
        return Failed(failure);
      }

      try
      {
        var sequenceLength = CanonicalStaticRenderObjectSequenceEncoder.GetSerializedLength(
          input.RootSourceObject,
          input.Animations,
          input.TextureResourceBindings
        );
        var outputLength = checked(
          sizeof(uint) + 16 + CommonMeshBaseHeader.SerializedSize + sequenceLength
        );
        if (outputLength > profile.MaxOutputBytes)
        {
          return Failed(AuthoringValidation.ResourceLimit(outputLength, profile.MaxOutputBytes));
        }

        var commonHeader = CanonicalBaseHeaderEncoder
          .EncodeStatic(input.BaseHeader)
          .SerializedRepresentation;
        var sequence = CanonicalStaticRenderObjectSequenceEncoder.Encode(
          input.RootSourceObject,
          input.Pivots,
          input.Animations,
          input.TextureResourceBindings
        );
        var bytes = MshCanonicalSerializer.CreateStatic(
          input.CreationGuid,
          commonHeader,
          sequence
        );
        var decoded = MshV1Decoder.Decode(
          bytes,
          profile,
          CancellationToken.None,
          MeshAssetOrigin.Canonical
        );
        return new MshBuildResult<StaticMeshAsset>(
          true,
          (StaticMeshAsset)decoded.Asset,
          decoded.Diagnostics
        );
      }
      catch (OverflowException)
      {
        return Failed(
          AuthoringValidation.Invalid(
            "CanonicalStaticMeshAssemblyInput",
            "A canonical semantic value is outside its serialized range."
          )
        );
      }
      catch (MshContentException ex)
      {
        return Failed(ex.Diagnostic);
      }
    }

    private static OperationDiagnostic? ValidateCompleteInput(
      CanonicalStaticMeshAssemblyInput input,
      int renderObjectCount,
      MshOperationProfile profile
    )
    {
      var unknownOrdinal = input.Pivots.Keys
        .Concat(input.Animations.Keys)
        .Concat(input.TextureResourceBindings.Keys)
        .Where(ordinal => ordinal < 0 || ordinal >= renderObjectCount)
        .Select(ordinal => (int?)ordinal)
        .FirstOrDefault();
      if (unknownOrdinal.HasValue)
      {
        return AuthoringValidation.Invalid(
          "CanonicalStaticMeshAssemblyInput.StaticRenderObjects",
          "Every per-render-object input ordinal must target the canonical sequence."
        );
      }

      foreach (var pivot in input.Pivots)
      {
        if (!IsFinite(pivot.Value))
        {
          return AuthoringValidation.Invalid(
            $"StaticRenderObjectSequence[{pivot.Key}].Pivot",
            "Pivot must be finite."
          );
        }
      }

      foreach (var animation in input.Animations)
      {
        var ordinal = animation.Key;
        if (animation.Value is null)
        {
          return AuthoringValidation.Invalid(
            $"StaticRenderObjectSequence[{ordinal}].AnimationTracks",
            "Animation input is required."
          );
        }
        if (animation.Value.ClassValue > (uint)StaticAnimationClass.D)
        {
          return AuthoringValidation.Invalid(
            $"StaticRenderObjectSequence[{ordinal}].AnimationClassValue",
            "Animation class must be A through D."
          );
        }
        var tracks = animation.Value.Tracks;
        if (
          tracks.ScaleFrames.Count > profile.MaxStaticAnimationFramesPerTrack
          || tracks.TranslationFrames.Count > profile.MaxStaticAnimationFramesPerTrack
          || tracks.Matrices.Count > profile.MaxStaticAnimationFramesPerTrack
        )
        {
          return AuthoringValidation.ResourceLimit(
            new[]
            {
              tracks.ScaleFrames.Count,
              tracks.TranslationFrames.Count,
              tracks.Matrices.Count,
            }.Max(),
            profile.MaxStaticAnimationFramesPerTrack
          );
        }
        if (
          tracks.ScaleFrames.Any(frame => !IsFinite(frame))
          || tracks.TranslationFrames.Any(frame => !IsFinite(frame))
          || tracks.Matrices.Any(matrix => !IsFinite(matrix))
        )
        {
          return AuthoringValidation.Invalid(
            $"StaticRenderObjectSequence[{ordinal}].AnimationTracks",
            "Animation values must be finite."
          );
        }
      }

      foreach (var binding in input.TextureResourceBindings)
      {
        if (binding.Value is null)
        {
          continue;
        }
        if (!AuthoringValidation.IsCanonicalTextureResourceKey(binding.Value))
        {
          return AuthoringValidation.Invalid(
            $"StaticRenderObjectSequence[{binding.Key}].TextureResourceKey",
            "TEX resource keys must use safe Textures\\...\\*.tex spelling."
          );
        }
        var byteCount = Encoding.ASCII.GetByteCount(binding.Value);
        if (byteCount > profile.MaxStaticTexturePathBytes)
        {
          return AuthoringValidation.ResourceLimit(byteCount, profile.MaxStaticTexturePathBytes);
        }
      }

      return null;
    }

    private static OperationDiagnostic? ValidateBaseHeader(CanonicalStaticBaseHeaderInput header)
    {
      if (
        header.Vertices.Count == 0
        || header.Vertices.Any(vertex => !IsFinite(vertex.Position))
        || header.AttachmentRecords.Values.Any(record => !IsFinite(record.Position))
        || header.CannonRenderPositions.Values.Any(record => !IsFinite(record.Position))
        || header.StaticSpotLights.Values.Any(record =>
          !IsFinite(record.Position)
          || !IsFinite(record.Color)
          || !IsFinite(record.ApproximateTargetDistance)
          || !IsFinite(record.ConeHalfAngleTangent)
          || !IsFinite(record.HalfFalloffAngleDistanceProduct)
          || !IsFinite(record.VerticalTargetSlope)
          || !IsFinite(record.TerrainLightAmplitude)
        )
        || header.StaticOmniLights.Values.Any(record =>
          !IsFinite(record.Position)
          || !IsFinite(record.Color)
          || !IsFinite(record.TerrainLightAmplitude)
        )
      )
      {
        return AuthoringValidation.Invalid(
          "CommonBaseHeader",
          "Canonical base-header values must be finite."
        );
      }
      return null;
    }

    private static MshBuildResult<StaticMeshAsset> Failed(OperationDiagnostic diagnostic)
    {
      return new MshBuildResult<StaticMeshAsset>(false, null, new[] { diagnostic });
    }

    private static bool IsFinite(Vector3 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }

    private static bool IsFinite(Matrix4x4 value)
    {
      return new[]
      {
        value.M11,
        value.M12,
        value.M13,
        value.M14,
        value.M21,
        value.M22,
        value.M23,
        value.M24,
        value.M31,
        value.M32,
        value.M33,
        value.M34,
        value.M41,
        value.M42,
        value.M43,
        value.M44,
      }.All(IsFinite);
    }

    private static bool IsFinite(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value);
    }
  }
}
