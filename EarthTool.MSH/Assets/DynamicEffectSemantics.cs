#nullable enable

using EarthTool.MSH.Internal;
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
      return DynamicEffectBehavior.Evaluate(extension, context).TrySelectFrame(
        totalLifetimeTicks,
        remainingLifetimeTicks,
        globalTick,
        out selection,
        out failure);
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
      return DynamicEffectBehavior.Evaluate(extension, context).TrySelectTextureRegion(
        frame,
        textureScale,
        out region,
        out failure);
    }

    /// <summary>Interpolates all rectangle lanes from an explicit selected phase.</summary>
    public static bool TryInterpolateEffectRectangle(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context,
      float phase,
      out EffectRectangle rectangle,
      out DynamicSemanticFailure failure)
    {
      return DynamicEffectBehavior.Evaluate(extension, context).TryInterpolateEffectRectangle(
        phase,
        out rectangle,
        out failure);
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
      return DynamicEffectBehavior.Evaluate(extension, context).TryInterpolateAlpha(
        framePhase,
        lifetimeProgress,
        out alpha,
        out failure);
    }

    /// <summary>Interpolates model scale from an explicit selected phase.</summary>
    public static bool TryInterpolateModelScale(
      DynamicEffectExtension extension,
      float phase,
      out float modelScale,
      out DynamicSemanticFailure failure)
    {
      return DynamicEffectBehavior.Evaluate(extension, DynamicEffectEvaluationContext.Primary)
        .TryInterpolateModelScale(phase, out modelScale, out failure);
    }

    /// <summary>Interpolates child translation from the explicit parent phase.</summary>
    public static bool TryInterpolateChildTranslation(
      DynamicEffectExtension extension,
      float parentPhase,
      out Vector3 translation,
      out DynamicSemanticFailure failure)
    {
      return DynamicEffectBehavior.Evaluate(extension, DynamicEffectEvaluationContext.Primary)
        .TryInterpolateChildTranslation(parentPhase, out translation, out failure);
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
      return DynamicEffectBehavior.Evaluate(extension, context).TryEvaluateVisibleEffectColor(
        sampledTerrainLightColor,
        outputColorScale,
        out color,
        out failure);
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
      return DynamicEffectBehavior.TryEvaluateTerrainLightIntensity(
        (uint)lightType,
        elapsedTicks,
        remainingTicks,
        durationTicks,
        runtimeRandomSample,
        out intensity,
        out failure);
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
      return DynamicEffectBehavior.TryEvaluateTerrainLightIntensity(
        lightType,
        elapsedTicks,
        remainingTicks,
        durationTicks,
        runtimeRandomSample,
        out intensity,
        out failure);
    }

    /// <summary>Selects the built-in Sphere frame from explicit lifetime input.</summary>
    public static bool TrySelectSphereFrame(
      int totalLifetimeTicks,
      int remainingLifetimeTicks,
      out int sourceFrame,
      out DynamicSemanticFailure failure)
    {
      return DynamicEffectBehavior.TrySelectSphereFrame(
        totalLifetimeTicks,
        remainingLifetimeTicks,
        out sourceFrame,
        out failure);
    }
  }
}
