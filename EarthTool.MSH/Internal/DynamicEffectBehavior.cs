#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace EarthTool.MSH.Internal
{
  internal enum DynamicObjectPlacement
  {
    Root,
    Child
  }

  internal enum DynamicBehaviorField
  {
    EffectType,
    LightType,
    Frames,
    SpriteSheet,
    Extension,
    RibbonHalfWidth,
    AdditiveFlag,
    AlphaTimingMode,
    MeshResourceKey,
    TextureResourceKey,
    InertRepresentations,
    ChildTranslation
  }

  internal sealed class DynamicBehaviorFinding
  {
    internal DynamicBehaviorField Field { get; }
    internal string PathSuffix { get; }
    internal string Code { get; }
    internal int EventId { get; }
    internal DiagnosticSeverity Severity { get; }
    internal string Message { get; }
    internal IReadOnlyDictionary<string, string> Data { get; }

    internal DynamicBehaviorFinding(
      DynamicBehaviorField field,
      string pathSuffix,
      string code,
      int eventId,
      DiagnosticSeverity severity,
      string message,
      IReadOnlyDictionary<string, string>? data = null)
    {
      Field = field;
      PathSuffix = pathSuffix;
      Code = code;
      EventId = eventId;
      Severity = severity;
      Message = message;
      Data = data ?? new Dictionary<string, string>();
    }

    internal OperationDiagnostic At(string path, long? byteOffset = null)
    {
      return new OperationDiagnostic(
        Code,
        EventId,
        Severity,
        path + PathSuffix,
        Message,
        byteOffset,
        Data);
    }
  }

  [Flags]
  internal enum DynamicSemanticUse
  {
    None = 0,
    OrdinaryFrames = 1 << 0,
    EffectRectangle = 1 << 1,
    Alpha = 1 << 2,
    ModelScale = 1 << 3,
    VisibleLightModulation = 1 << 4
  }

  [Flags]
  internal enum DynamicAuthoringRequirement
  {
    None = 0,
    Frames = 1 << 0,
    SpriteSheet = 1 << 1,
    RibbonHalfWidth = 1 << 2,
    MeshResourceKey = 1 << 3,
    TextureResourceKey = 1 << 4
  }

  [Flags]
  internal enum DynamicRepresentationUse
  {
    None = 0,
    LightType = 1 << 0,
    TerrainLightColor = 1 << 1,
    Frames = 1 << 2,
    SpriteSheet = 1 << 3,
    EffectRectangles = 1 << 4,
    EffectDepthOffset = 1 << 5,
    RibbonHalfWidth = 1 << 6,
    VisibleEffectColor = 1 << 7,
    VisibleTerrainLightGain = 1 << 8,
    AlphaTiming = 1 << 9,
    AlphaEndpoints = 1 << 10,
    ModelScale = 1 << 11,
    MeshResourceKey = 1 << 12,
    AdditiveFlag = 1 << 13,
    TextureResourceKey = 1 << 14
  }

  internal sealed class DynamicEffectDescriptor
  {
    internal DynamicEffectType EffectType { get; }
    internal DynamicSemanticUse PrimarySemantics { get; }
    internal DynamicAuthoringRequirement AuthoringRequirements { get; }
    internal DynamicRepresentationUse ConsumedRepresentations { get; }
    internal DynamicAuthoringDefaults AuthoringDefaults { get; }
    internal bool UsesAttachedVisibleLightModulation { get; }

    internal DynamicEffectDescriptor(
      DynamicEffectType effectType,
      DynamicSemanticUse primarySemantics,
      DynamicAuthoringRequirement authoringRequirements,
      DynamicRepresentationUse consumedRepresentations,
      DynamicAuthoringDefaults authoringDefaults,
      bool usesAttachedVisibleLightModulation = true)
    {
      EffectType = effectType;
      PrimarySemantics = primarySemantics;
      AuthoringRequirements = authoringRequirements;
      ConsumedRepresentations = consumedRepresentations;
      AuthoringDefaults = authoringDefaults;
      UsesAttachedVisibleLightModulation = usesAttachedVisibleLightModulation;
    }

    internal bool Uses(DynamicSemanticUse use)
    {
      return (PrimarySemantics & use) == use;
    }

    internal bool Requires(DynamicAuthoringRequirement requirement)
    {
      return (AuthoringRequirements & requirement) == requirement;
    }

    internal bool Consumes(DynamicRepresentationUse representation)
    {
      return (ConsumedRepresentations & representation) == representation;
    }
  }

  internal sealed class DynamicAuthoringDefaults
  {
    private readonly EffectRectangle _effectRectangle =
      new(-0.25f, 0.25f, 0.25f, -0.25f);

    internal CanonicalDynamicRecipe CreateRecipe(DynamicEffectType effectType)
    {
      return new CanonicalDynamicRecipe(string.Empty, string.Empty)
      {
        EffectType = effectType,
        LightType = DynamicLightType.Constant,
        StartEffectRectangle = _effectRectangle,
        EndEffectRectangle = _effectRectangle,
        EffectDepthOffset = 0.25f,
        RibbonHalfWidth = 0.25f,
        VisibleEffectColor = Vector3.One,
        VisibleTerrainLightGain = 1f,
        AlphaTiming = DynamicAlphaTiming.FramePhase,
        StartAlpha = 1f,
        EndAlpha = 1f
      };
    }
  }

  internal static class DynamicEffectBehavior
  {
    private const DynamicSemanticUse PrimaryFramesAndAlpha = DynamicSemanticUse.OrdinaryFrames
      | DynamicSemanticUse.Alpha;
    private const DynamicSemanticUse PrimaryFramesRectangleAndAlpha = PrimaryFramesAndAlpha
      | DynamicSemanticUse.EffectRectangle;
    private const DynamicAuthoringRequirement RequiresFramesAndTexture = DynamicAuthoringRequirement.Frames
      | DynamicAuthoringRequirement.TextureResourceKey;
    private const DynamicAuthoringRequirement RequiresSpriteAndTexture = RequiresFramesAndTexture
      | DynamicAuthoringRequirement.SpriteSheet;
    private const DynamicAuthoringRequirement RequiresRibbonSpriteAndTexture = RequiresSpriteAndTexture
      | DynamicAuthoringRequirement.RibbonHalfWidth;
    private const DynamicRepresentationUse ConsumesFramedVisible = DynamicRepresentationUse.Frames
      | DynamicRepresentationUse.VisibleEffectColor
      | DynamicRepresentationUse.AlphaTiming
      | DynamicRepresentationUse.AlphaEndpoints
      | DynamicRepresentationUse.AdditiveFlag
      | DynamicRepresentationUse.TextureResourceKey;
    private const DynamicRepresentationUse ConsumesSpriteVisible = ConsumesFramedVisible
      | DynamicRepresentationUse.SpriteSheet;
    private const DynamicRepresentationUse ConsumesTerrainLight = DynamicRepresentationUse.LightType
      | DynamicRepresentationUse.TerrainLightColor;
    private const DynamicRepresentationUse ConsumesRectangularVisible = ConsumesFramedVisible
      | DynamicRepresentationUse.EffectRectangles;
    private static readonly DynamicAuthoringDefaults _authoringDefaults = new();

    private static readonly DynamicEffectDescriptor[] _descriptors =
    {
      new(DynamicEffectType.Group, DynamicSemanticUse.None, DynamicAuthoringRequirement.None,
        DynamicRepresentationUse.None, _authoringDefaults),
      new(DynamicEffectType.Explosion, PrimaryFramesRectangleAndAlpha, RequiresSpriteAndTexture,
        ConsumesSpriteVisible | ConsumesTerrainLight | DynamicRepresentationUse.EffectRectangles
          | DynamicRepresentationUse.EffectDepthOffset, _authoringDefaults),
      new(DynamicEffectType.Track, PrimaryFramesRectangleAndAlpha, RequiresFramesAndTexture,
        DynamicRepresentationUse.Frames | DynamicRepresentationUse.EffectRectangles
          | DynamicRepresentationUse.AlphaTiming | DynamicRepresentationUse.AlphaEndpoints
          | DynamicRepresentationUse.AdditiveFlag | DynamicRepresentationUse.TextureResourceKey,
        _authoringDefaults),
      new(DynamicEffectType.ScalableObject, PrimaryFramesAndAlpha | DynamicSemanticUse.ModelScale,
        RequiresFramesAndTexture | DynamicAuthoringRequirement.MeshResourceKey,
        ConsumesFramedVisible | ConsumesTerrainLight | DynamicRepresentationUse.ModelScale
          | DynamicRepresentationUse.MeshResourceKey, _authoringDefaults),
      new(DynamicEffectType.MappedExplosion, PrimaryFramesRectangleAndAlpha, RequiresFramesAndTexture,
        ConsumesRectangularVisible | ConsumesTerrainLight, _authoringDefaults),
      new(DynamicEffectType.FlatExplosion, PrimaryFramesRectangleAndAlpha, RequiresSpriteAndTexture,
        ConsumesSpriteVisible | ConsumesTerrainLight | DynamicRepresentationUse.EffectRectangles
          | DynamicRepresentationUse.EffectDepthOffset, _authoringDefaults),
      new(DynamicEffectType.Laser, PrimaryFramesAndAlpha, RequiresRibbonSpriteAndTexture,
        ConsumesSpriteVisible | ConsumesTerrainLight | DynamicRepresentationUse.RibbonHalfWidth,
        _authoringDefaults),
      new(DynamicEffectType.LaserWall, PrimaryFramesAndAlpha, RequiresRibbonSpriteAndTexture,
        ConsumesSpriteVisible | DynamicRepresentationUse.TerrainLightColor
          | DynamicRepresentationUse.RibbonHalfWidth, _authoringDefaults),
      new(DynamicEffectType.Shockwave, DynamicSemanticUse.None, RequiresSpriteAndTexture,
        DynamicRepresentationUse.Frames | DynamicRepresentationUse.SpriteSheet
          | DynamicRepresentationUse.EffectRectangles | DynamicRepresentationUse.EffectDepthOffset
          | DynamicRepresentationUse.VisibleEffectColor
          | DynamicRepresentationUse.VisibleTerrainLightGain
          | DynamicRepresentationUse.AlphaEndpoints | DynamicRepresentationUse.AdditiveFlag
          | DynamicRepresentationUse.TextureResourceKey, _authoringDefaults),
      new(DynamicEffectType.Line, DynamicSemanticUse.None, RequiresSpriteAndTexture,
        DynamicRepresentationUse.Frames | DynamicRepresentationUse.SpriteSheet
          | DynamicRepresentationUse.EffectRectangles | DynamicRepresentationUse.EffectDepthOffset
          | DynamicRepresentationUse.VisibleEffectColor
          | DynamicRepresentationUse.VisibleTerrainLightGain
          | DynamicRepresentationUse.AlphaEndpoints | DynamicRepresentationUse.AdditiveFlag
          | DynamicRepresentationUse.TextureResourceKey, _authoringDefaults),
      new(DynamicEffectType.Sphere, DynamicSemanticUse.None,
        DynamicAuthoringRequirement.TextureResourceKey,
        DynamicRepresentationUse.VisibleEffectColor | DynamicRepresentationUse.AdditiveFlag
          | DynamicRepresentationUse.TextureResourceKey, _authoringDefaults),
      new(DynamicEffectType.ElectricalCannon, PrimaryFramesAndAlpha, RequiresRibbonSpriteAndTexture,
        ConsumesSpriteVisible | DynamicRepresentationUse.RibbonHalfWidth, _authoringDefaults),
      new(DynamicEffectType.Lightning, PrimaryFramesAndAlpha, RequiresRibbonSpriteAndTexture,
        ConsumesSpriteVisible | ConsumesTerrainLight | DynamicRepresentationUse.RibbonHalfWidth,
        _authoringDefaults),
      new(DynamicEffectType.Smoke,
        PrimaryFramesRectangleAndAlpha | DynamicSemanticUse.VisibleLightModulation,
        RequiresSpriteAndTexture,
        ConsumesSpriteVisible | DynamicRepresentationUse.EffectRectangles
          | DynamicRepresentationUse.EffectDepthOffset
          | DynamicRepresentationUse.VisibleTerrainLightGain, _authoringDefaults),
      new(DynamicEffectType.Keelwater, DynamicSemanticUse.None, RequiresSpriteAndTexture,
        DynamicRepresentationUse.Frames | DynamicRepresentationUse.SpriteSheet
          | DynamicRepresentationUse.EffectRectangles | DynamicRepresentationUse.EffectDepthOffset
          | DynamicRepresentationUse.AlphaEndpoints | DynamicRepresentationUse.AdditiveFlag
          | DynamicRepresentationUse.TextureResourceKey, _authoringDefaults,
        usesAttachedVisibleLightModulation: false)
    };

    internal static CanonicalDynamicRecipe NewRecipe(DynamicEffectType effectType)
    {
      var descriptor = GetDescriptor(effectType);
      return descriptor.AuthoringDefaults.CreateRecipe(effectType);
    }

    internal static DynamicBehaviorFinding? ValidateAuthoring(
      CanonicalDynamicRecipe recipe,
      DynamicObjectPlacement placement)
    {
      if (recipe is null)
      {
        throw new ArgumentNullException(nameof(recipe));
      }

      if (!TryGetDescriptor(recipe.EffectType, out var descriptor))
      {
        return Invalid(
          DynamicBehaviorField.EffectType,
          ".Extension.EffectType",
          "Canonical authoring requires a recognized dynamic effect.");
      }
      if (!Enum.IsDefined(typeof(DynamicLightType), recipe.LightType))
      {
        return Invalid(
          DynamicBehaviorField.LightType,
          ".Extension.LightType",
          "Canonical authoring requires a recognized light type.");
      }
      if (!Enum.IsDefined(typeof(DynamicAlphaTiming), recipe.AlphaTiming))
      {
        return Invalid(
          DynamicBehaviorField.AlphaTimingMode,
          ".Extension.AlphaTimingMode",
          "Canonical authoring requires a recognized alpha timing mode.");
      }
      if (!IsFinite(recipe.StartEffectRectangle)
        || !IsFinite(recipe.EndEffectRectangle)
        || !IsFinite(recipe.EffectDepthOffset)
        || !IsFinite(recipe.RibbonHalfWidth)
        || !IsFinite(recipe.TerrainLightColor)
        || !IsFinite(recipe.VisibleEffectColor)
        || !IsFinite(recipe.VisibleTerrainLightGain)
        || !IsFinite(recipe.StartAlpha)
        || !IsFinite(recipe.EndAlpha)
        || !IsFinite(recipe.StartModelScale)
        || !IsFinite(recipe.EndModelScale)
        || !IsFinite(recipe.ChildStartTranslation)
        || !IsFinite(recipe.ChildEndTranslation))
      {
        return Invalid(
          DynamicBehaviorField.Extension,
          ".Extension",
          "Canonical dynamic numeric inputs must be finite.");
      }
      if (placement == DynamicObjectPlacement.Root
        && (recipe.ChildStartTranslation != Vector3.Zero
          || recipe.ChildEndTranslation != Vector3.Zero))
      {
        return Invalid(
          DynamicBehaviorField.ChildTranslation,
          ".Extension.ChildTranslation",
          "A canonical root dynamic object cannot apply its own child translation.");
      }
      if (descriptor.Requires(DynamicAuthoringRequirement.Frames)
        && (recipe.FirstSourceFrame < 0 || recipe.FrameCount <= 0 || recipe.FramePeriodTicks < 0))
      {
        return Invalid(
          DynamicBehaviorField.Frames,
          ".Extension.Frames",
          "Canonical frame values are outside the supported domain.");
      }
      if (descriptor.Requires(DynamicAuthoringRequirement.SpriteSheet))
      {
        if (recipe.SpriteSheetColumnCount <= 0 || recipe.SpriteSheetRowCount <= 0)
        {
          return Invalid(
            DynamicBehaviorField.SpriteSheet,
            ".Extension.SpriteSheet",
            "Canonical sprite-sheet dimensions must be positive.");
        }

        try
        {
          if (checked(recipe.FirstSourceFrame + recipe.FrameCount)
            > checked(recipe.SpriteSheetColumnCount * recipe.SpriteSheetRowCount))
          {
            return Invalid(
              DynamicBehaviorField.SpriteSheet,
              ".Extension.SpriteSheet",
              "Canonical frames must fit in the sprite sheet.");
          }
        }
        catch (OverflowException)
        {
          return Invalid(
            DynamicBehaviorField.SpriteSheet,
            ".Extension.SpriteSheet",
            "Canonical sprite-sheet bounds overflow.");
        }
      }
      if (descriptor.Requires(DynamicAuthoringRequirement.RibbonHalfWidth)
        && recipe.RibbonHalfWidth == 0)
      {
        return Invalid(
          DynamicBehaviorField.RibbonHalfWidth,
          ".Extension.RibbonHalfWidth",
          "Canonical ribbon half-width must be nonzero and retains its sign.");
      }
      if (descriptor.Requires(DynamicAuthoringRequirement.MeshResourceKey)
        && string.IsNullOrEmpty(recipe.MeshResourceKey))
      {
        return Invalid(
          DynamicBehaviorField.MeshResourceKey,
          ".Extension.MeshNameBytes",
          "ScalableObject requires a mesh resource key.");
      }
      if (descriptor.Requires(DynamicAuthoringRequirement.TextureResourceKey)
        && string.IsNullOrEmpty(recipe.TextureResourceKey))
      {
        return Invalid(
          DynamicBehaviorField.TextureResourceKey,
          ".Extension.TexturePathBytes",
          "The selected effect requires a texture resource key.");
      }

      return null;
    }

    internal static IReadOnlyList<DynamicBehaviorFinding> Diagnose(
      DynamicEffectExtension extension,
      DynamicObjectPlacement placement)
    {
      if (extension is null)
      {
        throw new ArgumentNullException(nameof(extension));
      }

      var findings = new List<DynamicBehaviorFinding>();
      if (!extension.KnownEffectType.HasValue)
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.EffectType,
          ".Extension.EffectType",
          "An unrecognized dynamic effect value was preserved.",
          new Dictionary<string, string> { ["actual"] = $"0x{extension.EffectType:X8}" }));
      }
      if (!extension.KnownLightType.HasValue)
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.LightType,
          ".Extension.LightType",
          "An unrecognized dynamic light value was preserved.",
          new Dictionary<string, string> { ["actual"] = $"0x{extension.LightType:X8}" }));
      }
      if (extension.AdditiveFlag is not 0 and not 1)
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.AdditiveFlag,
          ".Extension.AdditiveFlag",
          "A noncanonical additive representation was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = extension.AdditiveFlag.ToString(System.Globalization.CultureInfo.InvariantCulture)
          }));
      }
      if (extension.AlphaTimingMode is not 0 and not 1)
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.AlphaTimingMode,
          ".Extension.AlphaTimingMode",
          "A noncanonical alpha timing representation was preserved.",
          new Dictionary<string, string>
          {
            ["actual"] = extension.AlphaTimingMode.ToString(System.Globalization.CultureInfo.InvariantCulture)
          }));
      }
      if (HasUnsafeFrameDeclaration(extension))
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.Frames,
          ".Extension.Frames",
          "A dynamic frame declaration outside the safe semantic-helper domain was preserved."));
      }
      if (!HasCanonicalReciprocal(extension.SpriteSheetColumnCount, extension.ReciprocalColumnCount)
        || !HasCanonicalReciprocal(extension.SpriteSheetRowCount, extension.ReciprocalRowCount))
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.SpriteSheet,
          ".Extension.SpriteSheet",
          "Independent sprite dimensions and reciprocal values disagree with canonical authoring."));
      }
      if (HasNonFiniteSemanticValue(extension))
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.Extension,
          ".Extension",
          "A non-finite dynamic semantic representation was preserved."));
      }
      if (HasNondefaultInertRepresentation(extension))
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.InertRepresentations,
          ".Extension.InertRepresentations",
          "Nondefault representations ignored by the selected effect were preserved."));
      }
      if (placement == DynamicObjectPlacement.Root
        && (extension.ChildStartTranslation != Vector3.Zero
          || extension.ChildEndTranslation != Vector3.Zero))
      {
        findings.Add(Compatibility(
          DynamicBehaviorField.ChildTranslation,
          ".Extension.ChildTranslation",
          "A root child-translation representation that is not applied by the renderer was preserved."));
      }

      return findings.AsReadOnly();
    }

    internal static DynamicEffectEvaluation Evaluate(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context)
    {
      return new DynamicEffectEvaluation(
        extension ?? throw new ArgumentNullException(nameof(extension)),
        context);
    }

    internal static bool UsesOrdinaryFrames(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      if (context == DynamicEffectEvaluationContext.AttachedParticle)
      {
        return true;
      }

      return effectType.HasValue
        && GetDescriptor(effectType.Value).Uses(DynamicSemanticUse.OrdinaryFrames);
    }

    internal static bool UsesRectangle(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      return context == DynamicEffectEvaluationContext.AttachedParticle
        || effectType.HasValue
          && GetDescriptor(effectType.Value).Uses(DynamicSemanticUse.EffectRectangle);
    }

    internal static bool UsesAlpha(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      return context == DynamicEffectEvaluationContext.AttachedParticle
        || UsesOrdinaryFrames(effectType, context);
    }

    internal static bool UsesVisibleLightModulation(
      DynamicEffectType? effectType,
      DynamicEffectEvaluationContext context)
    {
      return context == DynamicEffectEvaluationContext.AttachedParticle
        ? !effectType.HasValue || GetDescriptor(effectType.Value).UsesAttachedVisibleLightModulation
        : effectType.HasValue
          && GetDescriptor(effectType.Value).Uses(DynamicSemanticUse.VisibleLightModulation);
    }

    internal static bool UsesPrimarySemantic(
      DynamicEffectType effectType,
      DynamicSemanticUse semanticUse)
    {
      return GetDescriptor(effectType).Uses(semanticUse);
    }

    private static DynamicEffectDescriptor GetDescriptor(DynamicEffectType effectType)
    {
      return TryGetDescriptor(effectType, out var descriptor)
        ? descriptor
        : throw new ArgumentOutOfRangeException(nameof(effectType));
    }

    private static bool TryGetDescriptor(
      DynamicEffectType effectType,
      out DynamicEffectDescriptor descriptor)
    {
      var index = (int)effectType;
      if (index >= 0 && index < _descriptors.Length
        && _descriptors[index].EffectType == effectType)
      {
        descriptor = _descriptors[index];
        return true;
      }

      descriptor = null!;
      return false;
    }

    private static DynamicBehaviorFinding Invalid(
      DynamicBehaviorField field,
      string pathSuffix,
      string message)
    {
      return new DynamicBehaviorFinding(
        field,
        pathSuffix,
        Operations.MshDiagnosticCodes.InvalidAuthoringInput,
        1011,
        DiagnosticSeverity.Error,
        message);
    }

    private static DynamicBehaviorFinding Compatibility(
      DynamicBehaviorField field,
      string pathSuffix,
      string message,
      IReadOnlyDictionary<string, string>? data = null)
    {
      return new DynamicBehaviorFinding(
        field,
        pathSuffix,
        Operations.MshDiagnosticCodes.CompatibilityAnomaly,
        1009,
        DiagnosticSeverity.Warning,
        message,
        data);
    }

    private static bool HasUnsafeFrameDeclaration(DynamicEffectExtension extension)
    {
      var allZero = extension.FirstSourceFrame == 0
        && extension.FrameCount == 0
        && extension.SpriteSheetColumnCount == 0
        && extension.SpriteSheetRowCount == 0
        && extension.FramePeriodTicks == 0;
      return !allZero && (extension.FirstSourceFrame < 0
        || extension.FrameCount <= 0
        || extension.FramePeriodTicks < 0);
    }

    private static bool HasCanonicalReciprocal(int count, float reciprocal)
    {
      var expected = count == 0 ? 0 : 1f / count;
      return BitConverter.SingleToInt32Bits(reciprocal) == BitConverter.SingleToInt32Bits(expected);
    }

    private static bool HasNonFiniteSemanticValue(DynamicEffectExtension extension)
    {
      return !IsFinite(extension.ReciprocalColumnCount)
        || !IsFinite(extension.ReciprocalRowCount)
        || !IsFinite(extension.StartEffectRectangle)
        || !IsFinite(extension.EndEffectRectangle)
        || !IsFinite(extension.EffectDepthOffset)
        || !IsFinite(extension.RibbonHalfWidth)
        || !IsFinite(extension.TerrainLightColor)
        || !IsFinite(extension.VisibleEffectColor)
        || !IsFinite(extension.VisibleTerrainLightGain)
        || !IsFinite(extension.StartAlpha)
        || !IsFinite(extension.EndAlpha)
        || !IsFinite(extension.StartModelScale)
        || !IsFinite(extension.EndModelScale)
        || !IsFinite(extension.ChildStartTranslation)
        || !IsFinite(extension.ChildEndTranslation);
    }

    private static bool HasNondefaultInertRepresentation(DynamicEffectExtension extension)
    {
      if (!extension.KnownEffectType.HasValue)
      {
        return false;
      }

      var descriptor = GetDescriptor(extension.KnownEffectType.Value);
      var defaultRectangle = new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f);
      return (!descriptor.Consumes(DynamicRepresentationUse.LightType) && extension.LightType != 0)
        || (!descriptor.Consumes(DynamicRepresentationUse.TerrainLightColor)
          && extension.TerrainLightColor != Vector3.Zero)
        || (!descriptor.Consumes(DynamicRepresentationUse.Frames)
          && (extension.FirstSourceFrame != 0 || extension.FrameCount != 0
            || extension.FramePeriodTicks != 0))
        || (!descriptor.Consumes(DynamicRepresentationUse.SpriteSheet)
          && (extension.SpriteSheetColumnCount != 0 || extension.SpriteSheetRowCount != 0
            || extension.ReciprocalColumnCount != 0 || extension.ReciprocalRowCount != 0))
        || (!descriptor.Consumes(DynamicRepresentationUse.EffectRectangles)
          && (!extension.StartEffectRectangle.Equals(defaultRectangle)
            || !extension.EndEffectRectangle.Equals(defaultRectangle)))
        || (!descriptor.Consumes(DynamicRepresentationUse.EffectDepthOffset)
          && extension.EffectDepthOffset != 0.25f)
        || (!descriptor.Consumes(DynamicRepresentationUse.RibbonHalfWidth)
          && extension.RibbonHalfWidth != 0.25f)
        || (!descriptor.Consumes(DynamicRepresentationUse.VisibleEffectColor)
          && extension.VisibleEffectColor != Vector3.One)
        || (!descriptor.Consumes(DynamicRepresentationUse.VisibleTerrainLightGain)
          && extension.VisibleTerrainLightGain != 1f)
        || (!descriptor.Consumes(DynamicRepresentationUse.AlphaTiming)
          && extension.AlphaTimingMode != 0)
        || (!descriptor.Consumes(DynamicRepresentationUse.AlphaEndpoints)
          && (extension.StartAlpha != 1f || extension.EndAlpha != 1f))
        || (!descriptor.Consumes(DynamicRepresentationUse.ModelScale)
          && (extension.StartModelScale != 0 || extension.EndModelScale != 0))
        || (!descriptor.Consumes(DynamicRepresentationUse.MeshResourceKey)
          && extension.MeshNameBytes.Count != 0)
        || (!descriptor.Consumes(DynamicRepresentationUse.AdditiveFlag)
          && extension.AdditiveFlag != 0)
        || (!descriptor.Consumes(DynamicRepresentationUse.TextureResourceKey)
          && extension.TexturePathBytes.Count != 0);
    }

    internal static bool TryEvaluateTerrainLightIntensity(
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

    internal static bool TrySelectSphereFrame(
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

    internal static float Lerp(float start, float end, float phase)
    {
      return start * (1 - phase) + end * phase;
    }

    internal static bool IsFinite(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal static bool IsFinite(Vector3 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }

    internal static bool IsFinite(EffectRectangle value)
    {
      return IsFinite(value.X0) && IsFinite(value.Y1) && IsFinite(value.X1) && IsFinite(value.Y0);
    }
  }

  internal readonly struct DynamicEffectEvaluation
  {
    private readonly DynamicEffectExtension _extension;
    private readonly DynamicEffectEvaluationContext _context;

    internal DynamicEffectEvaluation(
      DynamicEffectExtension extension,
      DynamicEffectEvaluationContext context)
    {
      _extension = extension;
      _context = context;
    }

    internal bool TrySelectFrame(
      int totalLifetimeTicks,
      int remainingLifetimeTicks,
      uint globalTick,
      out DynamicFrameSelection selection,
      out DynamicSemanticFailure failure)
    {
      selection = default;
      if (!DynamicEffectBehavior.UsesOrdinaryFrames(_extension.KnownEffectType, _context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (totalLifetimeTicks <= 0)
      {
        failure = DynamicSemanticFailure.InvalidLifetime;
        return false;
      }

      if (_extension.FirstSourceFrame < 0
        || _extension.FrameCount <= 0
        || _extension.FramePeriodTicks < 0)
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
        if (_extension.FramePeriodTicks == 0)
        {
          frameIndex = unchecked(_extension.FrameCount * elapsed) / totalLifetimeTicks;
          phase = (float)elapsed / totalLifetimeTicks;
        }
        else
        {
          frameIndex = (int)((globalTick / (uint)_extension.FramePeriodTicks)
            % (uint)_extension.FrameCount);
          phase = (float)frameIndex / _extension.FrameCount;
        }

        selection = new DynamicFrameSelection(
          unchecked(_extension.FirstSourceFrame + frameIndex),
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

    internal bool TrySelectTextureRegion(
      DynamicFrameSelection frame,
      float textureScale,
      out DynamicTextureRegion region,
      out DynamicSemanticFailure failure)
    {
      region = default;
      if (!DynamicEffectBehavior.UsesOrdinaryFrames(_extension.KnownEffectType, _context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (_extension.SpriteSheetColumnCount <= 0)
      {
        failure = DynamicSemanticFailure.InvalidSpriteSheet;
        return false;
      }

      if (!DynamicEffectBehavior.IsFinite(textureScale)
        || !DynamicEffectBehavior.IsFinite(_extension.ReciprocalColumnCount)
        || !DynamicEffectBehavior.IsFinite(_extension.ReciprocalRowCount))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var row = frame.SourceFrame / _extension.SpriteSheetColumnCount;
      var column = frame.SourceFrame % _extension.SpriteSheetColumnCount;
      var du = textureScale * _extension.ReciprocalColumnCount;
      var dv = textureScale * _extension.ReciprocalRowCount;
      var u0 = column * du;
      var v0 = row * dv;
      if (!DynamicEffectBehavior.IsFinite(du)
        || !DynamicEffectBehavior.IsFinite(dv)
        || !DynamicEffectBehavior.IsFinite(u0)
        || !DynamicEffectBehavior.IsFinite(v0))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var u1 = u0 + du;
      var v1 = v0 + dv;
      if (!DynamicEffectBehavior.IsFinite(u1) || !DynamicEffectBehavior.IsFinite(v1))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      region = new DynamicTextureRegion(row, column, u0, v0, u1, v1);
      failure = DynamicSemanticFailure.None;
      return true;
    }

    internal bool TryInterpolateEffectRectangle(
      float phase,
      out EffectRectangle rectangle,
      out DynamicSemanticFailure failure)
    {
      rectangle = default;
      if (!DynamicEffectBehavior.UsesRectangle(_extension.KnownEffectType, _context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (!DynamicEffectBehavior.IsFinite(phase)
        || !DynamicEffectBehavior.IsFinite(_extension.StartEffectRectangle)
        || !DynamicEffectBehavior.IsFinite(_extension.EndEffectRectangle))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var start = _extension.StartEffectRectangle;
      var end = _extension.EndEffectRectangle;
      rectangle = new EffectRectangle(
        DynamicEffectBehavior.Lerp(start.X0, end.X0, phase),
        DynamicEffectBehavior.Lerp(start.Y1, end.Y1, phase),
        DynamicEffectBehavior.Lerp(start.X1, end.X1, phase),
        DynamicEffectBehavior.Lerp(start.Y0, end.Y0, phase));
      failure = DynamicEffectBehavior.IsFinite(rectangle)
        ? DynamicSemanticFailure.None
        : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    internal bool TryInterpolateAlpha(
      float framePhase,
      float lifetimeProgress,
      out float alpha,
      out DynamicSemanticFailure failure)
    {
      alpha = default;
      if (!DynamicEffectBehavior.UsesAlpha(_extension.KnownEffectType, _context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      var phase = _context == DynamicEffectEvaluationContext.AttachedParticle
        ? framePhase
        : _extension.UsesLifetimeProgressAlpha
          ? lifetimeProgress
          : framePhase;
      if (!DynamicEffectBehavior.IsFinite(phase)
        || !DynamicEffectBehavior.IsFinite(_extension.StartAlpha)
        || !DynamicEffectBehavior.IsFinite(_extension.EndAlpha))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      alpha = DynamicEffectBehavior.Lerp(_extension.StartAlpha, _extension.EndAlpha, phase);
      failure = DynamicEffectBehavior.IsFinite(alpha)
        ? DynamicSemanticFailure.None
        : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    internal bool TryInterpolateModelScale(
      float phase,
      out float modelScale,
      out DynamicSemanticFailure failure)
    {
      modelScale = default;
      if (!_extension.KnownEffectType.HasValue
        || !DynamicEffectBehavior.UsesPrimarySemantic(
          _extension.KnownEffectType.Value,
          DynamicSemanticUse.ModelScale))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (!DynamicEffectBehavior.IsFinite(phase)
        || !DynamicEffectBehavior.IsFinite(_extension.StartModelScale)
        || !DynamicEffectBehavior.IsFinite(_extension.EndModelScale))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      modelScale = DynamicEffectBehavior.Lerp(
        _extension.StartModelScale,
        _extension.EndModelScale,
        phase);
      failure = DynamicEffectBehavior.IsFinite(modelScale)
        ? DynamicSemanticFailure.None
        : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    internal bool TryInterpolateChildTranslation(
      float parentPhase,
      out Vector3 translation,
      out DynamicSemanticFailure failure)
    {
      translation = default;
      if (!DynamicEffectBehavior.IsFinite(parentPhase)
        || !DynamicEffectBehavior.IsFinite(_extension.ChildStartTranslation)
        || !DynamicEffectBehavior.IsFinite(_extension.ChildEndTranslation))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      translation = _extension.ChildStartTranslation * (1 - parentPhase)
        + _extension.ChildEndTranslation * parentPhase;
      failure = DynamicEffectBehavior.IsFinite(translation)
        ? DynamicSemanticFailure.None
        : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }

    internal bool TryEvaluateVisibleEffectColor(
      Vector3 sampledTerrainLightColor,
      float outputColorScale,
      out Vector3 color,
      out DynamicSemanticFailure failure)
    {
      color = default;
      if (!DynamicEffectBehavior.UsesVisibleLightModulation(_extension.KnownEffectType, _context))
      {
        failure = DynamicSemanticFailure.InapplicableEffect;
        return false;
      }

      if (!DynamicEffectBehavior.IsFinite(sampledTerrainLightColor)
        || !DynamicEffectBehavior.IsFinite(outputColorScale)
        || !DynamicEffectBehavior.IsFinite(_extension.VisibleEffectColor)
        || !DynamicEffectBehavior.IsFinite(_extension.VisibleTerrainLightGain))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var scaledLight = sampledTerrainLightColor * _extension.VisibleTerrainLightGain;
      if (!DynamicEffectBehavior.IsFinite(scaledLight))
      {
        failure = DynamicSemanticFailure.NonFiniteInput;
        return false;
      }

      var light = Vector3.Min(Vector3.One, scaledLight);
      color = _extension.VisibleEffectColor * light * outputColorScale;
      failure = DynamicEffectBehavior.IsFinite(color)
        ? DynamicSemanticFailure.None
        : DynamicSemanticFailure.NonFiniteInput;
      return failure == DynamicSemanticFailure.None;
    }
  }
}
