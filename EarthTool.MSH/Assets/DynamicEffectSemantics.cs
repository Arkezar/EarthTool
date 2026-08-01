#nullable enable

using System;
using System.Numerics;

namespace EarthTool.MSH.Assets
{
  /// <summary>Names structured failures from pure dynamic semantic evaluation.</summary>
  public enum DynamicSemanticFailure
  {
    /// <summary>Evaluation succeeded.</summary>
    None = 0,
    /// <summary>An explicit floating-point input was not finite.</summary>
    NonFiniteInput = 1,
    /// <summary>The explicit lifetime domain cannot be evaluated safely.</summary>
    InvalidLifetime = 2,
    /// <summary>The serialized frame declaration cannot be evaluated safely.</summary>
    InvalidFrameDeclaration = 3,
    /// <summary>The serialized sprite-sheet declaration cannot be evaluated safely.</summary>
    InvalidSpriteSheet = 4,
    /// <summary>Random light evaluation requires an explicit runtime-equivalent sample.</summary>
    RandomSampleRequired = 5,
    /// <summary>Defined 32-bit arithmetic reached an unsafe division case.</summary>
    ArithmeticOverflow = 6,
    /// <summary>The selected effect does not consume this semantic view in the requested context.</summary>
    InapplicableEffect = 7
  }

  /// <summary>Identifies whether an effect is evaluated as a primary object or attached particle.</summary>
  public enum DynamicEffectEvaluationContext
  {
    /// <summary>Evaluate the effect through primary dynamic-object dispatch.</summary>
    Primary = 0,
    /// <summary>Evaluate the effect through attached-particle dispatch.</summary>
    AttachedParticle = 1
  }

  /// <summary>Reports deterministic frame selection and its interpolation phase.</summary>
  public readonly struct DynamicFrameSelection : IEquatable<DynamicFrameSelection>
  {
    /// <summary>Gets the selected absolute source frame.</summary>
    public int SourceFrame { get; }
    /// <summary>Gets the selected zero-based frame index.</summary>
    public int FrameIndex { get; }
    /// <summary>Gets the selected frame phase.</summary>
    public float Phase { get; }

    /// <summary>Initializes one frame-selection result.</summary>
    public DynamicFrameSelection(int sourceFrame, int frameIndex, float phase)
    {
      SourceFrame = sourceFrame;
      FrameIndex = frameIndex;
      Phase = phase;
    }

    /// <inheritdoc />
    public bool Equals(DynamicFrameSelection other)
    {
      return SourceFrame == other.SourceFrame && FrameIndex == other.FrameIndex && Phase.Equals(other.Phase);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is DynamicFrameSelection other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (SourceFrame, FrameIndex, Phase).GetHashCode();
    }
  }

  /// <summary>Reports one selected texture region without reading renderer state.</summary>
  public readonly struct DynamicTextureRegion : IEquatable<DynamicTextureRegion>
  {
    /// <summary>Gets the signed atlas row.</summary>
    public int Row { get; }
    /// <summary>Gets the signed atlas column.</summary>
    public int Column { get; }
    /// <summary>Gets the first U coordinate.</summary>
    public float U0 { get; }
    /// <summary>Gets the first V coordinate.</summary>
    public float V0 { get; }
    /// <summary>Gets the second U coordinate.</summary>
    public float U1 { get; }
    /// <summary>Gets the second V coordinate.</summary>
    public float V1 { get; }

    /// <summary>Initializes one texture-region result.</summary>
    public DynamicTextureRegion(int row, int column, float u0, float v0, float u1, float v1)
    {
      Row = row;
      Column = column;
      U0 = u0;
      V0 = v0;
      U1 = u1;
      V1 = v1;
    }

    /// <inheritdoc />
    public bool Equals(DynamicTextureRegion other)
    {
      return Row == other.Row
        && Column == other.Column
        && U0.Equals(other.U0)
        && V0.Equals(other.V0)
        && U1.Equals(other.U1)
        && V1.Equals(other.V1);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      return obj is DynamicTextureRegion other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return (Row, Column, U0, V0, U1, V1).GetHashCode();
    }
  }

  /// <summary>Evaluates confirmed record-local dynamic semantics from explicit inputs.</summary>
  public static class DynamicEffectSemantics
  {
    /// <summary>Selects one frame from explicit lifetime and global-tick inputs.</summary>
    public static bool TrySelectFrame(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context,
      int totalLifetimeTicks,
      int remainingLifetimeTicks,
      uint globalTick,
      out DynamicFrameSelection selection,
      out DynamicSemanticFailure failure)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      selection = default;
      if (!UsesOrdinaryFrames(extension.KnownEffectType, context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (totalLifetimeTicks <= 0)
      {
        failure = DynamicSemanticFailure.InvalidLifetime;
        return false;
      }

      if (extension.FirstSourceFrame < 0
        || extension.FrameCount <= 0
        || extension.FramePeriodTicks < 0)
      {
        failure = DynamicSemanticFailure.InvalidFrameDeclaration;
        return false;
      }

      try
      {
        var elapsed = unchecked(totalLifetimeTicks - remainingLifetimeTicks);
        if (elapsed == 0)
        {
          elapsed = 1;
        }

        int frameIndex;
        float phase;
        if (extension.FramePeriodTicks == 0)
        {
          frameIndex = unchecked(extension.FrameCount * elapsed) / totalLifetimeTicks;
          phase = (float)elapsed / totalLifetimeTicks;
        }
        else
        {
          frameIndex = (int)((globalTick / (uint)extension.FramePeriodTicks)
            % (uint)extension.FrameCount);
          phase = (float)frameIndex / extension.FrameCount;
        }

        selection = new DynamicFrameSelection(
          unchecked(extension.FirstSourceFrame + frameIndex),
          frameIndex,
          phase);
        failure = DynamicSemanticFailure.None;
        return true;
      }
      catch (OverflowException)
      {
        failure = DynamicSemanticFailure.ArithmeticOverflow;
        return false;
      }
    }

    /// <summary>Selects one texture region from an explicit texture scale.</summary>
    public static bool TrySelectTextureRegion(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context,
      DynamicFrameSelection frame,
      float textureScale,
      out DynamicTextureRegion region,
      out DynamicSemanticFailure failure)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      region = default;
      if (!UsesOrdinaryFrames(extension.KnownEffectType, context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (extension.SpriteSheetColumnCount <= 0)
      {
        failure = DynamicSemanticFailure.InvalidSpriteSheet;
        return false;
      }

      if (!IsFinite(textureScale)
        || !IsFinite(extension.ReciprocalColumnCount)
        || !IsFinite(extension.ReciprocalRowCount))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var row = frame.SourceFrame / extension.SpriteSheetColumnCount;
      var column = frame.SourceFrame % extension.SpriteSheetColumnCount;
      var du = textureScale * extension.ReciprocalColumnCount;
      var dv = textureScale * extension.ReciprocalRowCount;
      var u0 = column * du;
      var v0 = row * dv;
      if (!IsFinite(du) || !IsFinite(dv) || !IsFinite(u0) || !IsFinite(v0))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var u1 = u0 + du;
      var v1 = v0 + dv;
      if (!IsFinite(u1) || !IsFinite(v1))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      region = new DynamicTextureRegion(row, column, u0, v0, u1, v1);
      failure = DynamicSemanticFailure.None;
      return true;
    }

    /// <summary>Interpolates all rectangle lanes from an explicit selected phase.</summary>
    public static bool TryInterpolateEffectRectangle(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context,
      float phase,
      out EffectRectangle rectangle,
      out DynamicSemanticFailure failure)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      rectangle = default;
      if (!UsesRectangle(extension.KnownEffectType, context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (!IsFinite(phase)
        || !IsFinite(extension.StartEffectRectangle)
        || !IsFinite(extension.EndEffectRectangle))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var start = extension.StartEffectRectangle;
      var end = extension.EndEffectRectangle;
      rectangle = new EffectRectangle(
        Lerp(start.X0, end.X0, phase),
        Lerp(start.Y1, end.Y1, phase),
        Lerp(start.X1, end.X1, phase),
        Lerp(start.Y0, end.Y0, phase));
      failure = IsFinite(rectangle)
        ? DynamicSemanticFailure.None
        : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    /// <summary>Interpolates alpha from explicit selected-frame and lifetime phases.</summary>
    public static bool TryInterpolateAlpha(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context,
      float framePhase,
      float lifetimeProgress,
      out float alpha,
      out DynamicSemanticFailure failure)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      alpha = default;
      if (!UsesAlpha(extension.KnownEffectType, context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      var phase = context == DynamicEffectEvaluationContext.AttachedParticle
        ? framePhase
        : extension.UsesLifetimeProgressAlpha
          ? lifetimeProgress
          : framePhase;
      if (!IsFinite(phase) || !IsFinite(extension.StartAlpha) || !IsFinite(extension.EndAlpha))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      alpha = Lerp(extension.StartAlpha, extension.EndAlpha, phase);
      failure = IsFinite(alpha) ? DynamicSemanticFailure.None : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    /// <summary>Interpolates model scale from an explicit selected phase.</summary>
    public static bool TryInterpolateModelScale(
      DynamicEffectExtension extension,
      float phase,
      out float modelScale,
      out DynamicSemanticFailure failure)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      modelScale = default;
      if (extension.KnownEffectType != DynamicEffectType.ScalableObject)
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (!IsFinite(phase)
        || !IsFinite(extension.StartModelScale)
        || !IsFinite(extension.EndModelScale))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      modelScale = Lerp(extension.StartModelScale, extension.EndModelScale, phase);
      failure = IsFinite(modelScale) ? DynamicSemanticFailure.None : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    /// <summary>Interpolates child translation from the explicit parent phase.</summary>
    public static bool TryInterpolateChildTranslation(
      DynamicEffectExtension extension,
      float parentPhase,
      out Vector3 translation,
      out DynamicSemanticFailure failure)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      translation = default;
      if (!IsFinite(parentPhase)
        || !IsFinite(extension.ChildStartTranslation)
        || !IsFinite(extension.ChildEndTranslation))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      translation = extension.ChildStartTranslation * (1 - parentPhase)
        + extension.ChildEndTranslation * parentPhase;
      failure = IsFinite(translation) ? DynamicSemanticFailure.None : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    /// <summary>Evaluates visible effect RGB from explicit sampled light and backend scale.</summary>
    public static bool TryEvaluateVisibleEffectColor(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context,
      Vector3 sampledTerrainLightColor,
      float outputColorScale,
      out Vector3 color,
      out DynamicSemanticFailure failure)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      color = default;
      if (!UsesVisibleLightModulation(extension.KnownEffectType, context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (!IsFinite(sampledTerrainLightColor)
        || !IsFinite(outputColorScale)
        || !IsFinite(extension.VisibleEffectColor)
        || !IsFinite(extension.VisibleTerrainLightGain))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var scaledLight = sampledTerrainLightColor * extension.VisibleTerrainLightGain;
      if (!IsFinite(scaledLight))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var light = Vector3.Min(Vector3.One, scaledLight);
      color = extension.VisibleEffectColor * light * outputColorScale;
      failure = IsFinite(color) ? DynamicSemanticFailure.None : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    /// <summary>Evaluates a known terrain-light mode from explicit timing and randomness inputs.</summary>
    public static bool TryEvaluateTerrainLightIntensity(
      DynamicLightType lightType,
      int elapsedTicks,
      int remainingTicks,
      int durationTicks,
      uint? runtimeRandomSample,
      out float intensity,
      out DynamicSemanticFailure failure)
    {
      return TryEvaluateTerrainLightIntensity((uint)lightType, elapsedTicks, remainingTicks,
        durationTicks, runtimeRandomSample, out intensity, out failure);
    }

    /// <summary>Evaluates an exact terrain-light value; unknown values use confirmed constant behavior.</summary>
    public static bool TryEvaluateTerrainLightIntensity(
      uint lightType,
      int elapsedTicks,
      int remainingTicks,
      int durationTicks,
      uint? runtimeRandomSample,
      out float intensity,
      out DynamicSemanticFailure failure)
    {
      intensity = default;
      if (durationTicks <= 0 || elapsedTicks < 0 || remainingTicks < 0)
      {
        failure = DynamicSemanticFailure.InvalidLifetime;
        return false;
      }

      var minimum = Math.Min(elapsedTicks, remainingTicks);
      switch (lightType)
      {
        case (uint)DynamicLightType.Pyramid:
          intensity = Math.Min(durationTicks, 2L * minimum) / (float)durationTicks;
          break;
        case (uint)DynamicLightType.Trapezium:
          intensity = Math.Min(durationTicks, 3L * minimum) / (float)durationTicks;
          break;
        case (uint)DynamicLightType.Random:
          if (!runtimeRandomSample.HasValue)
          {
            failure = DynamicSemanticFailure.RandomSampleRequired;
            return false;
          }

          intensity = 0.8f + ((runtimeRandomSample.Value % 4000) * 0.0001f);
          break;
        default:
          intensity = 1f;
          break;
      }

      failure = DynamicSemanticFailure.None;
      return true;
    }

    /// <summary>Selects the built-in Sphere frame from explicit lifetime input.</summary>
    public static bool TrySelectSphereFrame(
      int totalLifetimeTicks,
      int remainingLifetimeTicks,
      out int sourceFrame,
      out DynamicSemanticFailure failure)
    {
      sourceFrame = default;
      if (totalLifetimeTicks <= 0)
      {
        failure = DynamicSemanticFailure.InvalidLifetime;
        return false;
      }

      try
      {
        sourceFrame = unchecked((totalLifetimeTicks - remainingLifetimeTicks) << 4)
          / totalLifetimeTicks;
        if (sourceFrame > 15)
        {
          sourceFrame = 15;
        }

        failure = DynamicSemanticFailure.None;
        return true;
      }
      catch (OverflowException)
      {
        failure = DynamicSemanticFailure.ArithmeticOverflow;
        return false;
      }
    }

    private static float Lerp(float start, float end, float phase)
    {
      return start * (1 - phase) + end * phase;
    }

    private static bool UsesOrdinaryFrames(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      if (context == DynamicEffectEvaluationContext.AttachedParticle)
      {
        return true;
      }

      return effectType is DynamicEffectType.Explosion
        or DynamicEffectType.Track
        or DynamicEffectType.ScalableObject
        or DynamicEffectType.MappedExplosion
        or DynamicEffectType.FlatExplosion
        or DynamicEffectType.Laser
        or DynamicEffectType.LaserWall
        or DynamicEffectType.ElectricalCannon
        or DynamicEffectType.Lightning
        or DynamicEffectType.Smoke;
    }

    private static bool UsesRectangle(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      return context == DynamicEffectEvaluationContext.AttachedParticle
        || effectType is DynamicEffectType.Explosion
          or DynamicEffectType.Track
          or DynamicEffectType.MappedExplosion
          or DynamicEffectType.FlatExplosion
          or DynamicEffectType.Smoke;
    }

    private static bool UsesAlpha(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      return context == DynamicEffectEvaluationContext.AttachedParticle
        || UsesOrdinaryFrames(effectType, context);
    }

    private static bool UsesVisibleLightModulation(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      return effectType == DynamicEffectType.Smoke
        || (context == DynamicEffectEvaluationContext.AttachedParticle
          && effectType != DynamicEffectType.Keelwater);
    }

    private static bool IsFinite(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinite(Vector3 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }

    private static bool IsFinite(EffectRectangle value)
    {
      return IsFinite(value.X0) && IsFinite(value.Y1) && IsFinite(value.X1) && IsFinite(value.Y0);
    }
  }
}
