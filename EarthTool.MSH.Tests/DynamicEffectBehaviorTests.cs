using AwesomeAssertions;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
using System.Buffers.Binary;
using System.Numerics;

namespace EarthTool.MSH.Tests;

public class DynamicEffectBehaviorTests
{
  [Fact]
  public void EveryKnownEffectReceivesCanonicalAuthoringDefaults()
  {
    foreach (var effectType in Enum.GetValues<DynamicEffectType>())
    {
      var recipe = DynamicEffectBehavior.NewRecipe(effectType);

      recipe.EffectType.Should().Be(effectType);
      recipe.LightType.Should().Be(DynamicLightType.Constant);
      recipe.FirstSourceFrame.Should().Be(0);
      recipe.FrameCount.Should().Be(0);
      recipe.SpriteSheetColumnCount.Should().Be(0);
      recipe.SpriteSheetRowCount.Should().Be(0);
      recipe.FramePeriodTicks.Should().Be(0);
      recipe.StartEffectRectangle.Should().Be(new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f));
      recipe.EndEffectRectangle.Should().Be(new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f));
      recipe.EffectDepthOffset.Should().Be(0.25f);
      recipe.RibbonHalfWidth.Should().Be(0.25f);
      recipe.Additive.Should().BeFalse();
      recipe.TerrainLightColor.Should().Be(Vector3.Zero);
      recipe.VisibleEffectColor.Should().Be(Vector3.One);
      recipe.VisibleTerrainLightGain.Should().Be(1f);
      recipe.AlphaTiming.Should().Be(DynamicAlphaTiming.FramePhase);
      recipe.StartAlpha.Should().Be(1f);
      recipe.EndAlpha.Should().Be(1f);
      recipe.StartModelScale.Should().Be(0f);
      recipe.EndModelScale.Should().Be(0f);
      recipe.ChildStartTranslation.Should().Be(Vector3.Zero);
      recipe.ChildEndTranslation.Should().Be(Vector3.Zero);
      recipe.MeshResourceKey.Should().BeEmpty();
      recipe.TextureResourceKey.Should().BeEmpty();
    }
  }

  [Fact]
  public void UnknownEffectRequiresExplicitAttachedParticleEvaluation()
  {
    var extension = CreateExtension(uint.MaxValue);

    DynamicEffectBehavior.Evaluate(extension, DynamicEffectEvaluationContext.Primary)
      .TrySelectFrame(10, 7, 5, out _, out var primaryFailure)
      .Should().BeFalse();
    primaryFailure.Should().Be(DynamicSemanticFailure.InapplicableEffect);

    DynamicEffectBehavior.Evaluate(extension, DynamicEffectEvaluationContext.AttachedParticle)
      .TrySelectFrame(10, 7, 5, out var selection, out var attachedFailure)
      .Should().BeTrue();
    attachedFailure.Should().Be(DynamicSemanticFailure.None);
    selection.Should().Be(new DynamicFrameSelection(4, 2, 2f / 6f));

    var attached = DynamicEffectBehavior.Evaluate(
      extension,
      DynamicEffectEvaluationContext.AttachedParticle);
    attached.TrySelectTextureRegion(selection, 1, out _, out _).Should().BeTrue();
    attached.TryInterpolateEffectRectangle(0.5f, out _, out _).Should().BeTrue();
    attached.TryInterpolateAlpha(0.5f, 0.5f, out _, out _).Should().BeTrue();
    attached.TryEvaluateVisibleEffectColor(Vector3.One, 1, out _, out _).Should().BeTrue();
    attached.TryInterpolateModelScale(0.5f, out _, out var scaleFailure).Should().BeFalse();
    scaleFailure.Should().Be(DynamicSemanticFailure.InapplicableEffect);

    DynamicEffectBehavior.Evaluate(
      CreateExtension((uint)DynamicEffectType.Keelwater),
      DynamicEffectEvaluationContext.AttachedParticle)
      .TryEvaluateVisibleEffectColor(Vector3.One, 1, out _, out var keelwaterColorFailure)
      .Should().BeFalse();
    keelwaterColorFailure.Should().Be(DynamicSemanticFailure.InapplicableEffect);
    extension.EffectType.Should().Be(uint.MaxValue);
    extension.KnownEffectType.Should().BeNull();
  }

  [Fact]
  public void PrimaryApplicabilityMatchesEveryKnownEffectBehavior()
  {
    var expectations = new (
      DynamicEffectType EffectType,
      bool Frames,
      bool Rectangle,
      bool Alpha,
      bool ModelScale,
      bool VisibleColor)[]
    {
      (DynamicEffectType.Group, false, false, false, false, false),
      (DynamicEffectType.Explosion, true, true, true, false, false),
      (DynamicEffectType.Track, true, true, true, false, false),
      (DynamicEffectType.ScalableObject, true, false, true, true, false),
      (DynamicEffectType.MappedExplosion, true, true, true, false, false),
      (DynamicEffectType.FlatExplosion, true, true, true, false, false),
      (DynamicEffectType.Laser, true, false, true, false, false),
      (DynamicEffectType.LaserWall, true, false, true, false, false),
      (DynamicEffectType.Shockwave, false, false, false, false, false),
      (DynamicEffectType.Line, false, false, false, false, false),
      (DynamicEffectType.Sphere, false, false, false, false, false),
      (DynamicEffectType.ElectricalCannon, true, false, true, false, false),
      (DynamicEffectType.Lightning, true, false, true, false, false),
      (DynamicEffectType.Smoke, true, true, true, false, true),
      (DynamicEffectType.Keelwater, false, false, false, false, false)
    };

    foreach (var expected in expectations)
    {
      var evaluation = DynamicEffectBehavior.Evaluate(
        CreateExtension((uint)expected.EffectType),
        DynamicEffectEvaluationContext.Primary);

      evaluation.TrySelectFrame(10, 7, 5, out _, out _).Should().Be(expected.Frames);
      evaluation.TryInterpolateEffectRectangle(0.5f, out _, out _).Should().Be(expected.Rectangle);
      evaluation.TryInterpolateAlpha(0.5f, 0.5f, out _, out _).Should().Be(expected.Alpha);
      evaluation.TryInterpolateModelScale(0.5f, out _, out _).Should().Be(expected.ModelScale);
      evaluation.TryEvaluateVisibleEffectColor(Vector3.One, 1, out _, out _)
        .Should().Be(expected.VisibleColor);
    }
  }

  [Fact]
  public void AuthoringValidationUsesEffectAndPlacementRules()
  {
    DynamicEffectBehavior.ValidateAuthoring(
      DynamicEffectBehavior.NewRecipe(DynamicEffectType.Group),
      DynamicObjectPlacement.Root).Should().BeNull();

    var scalableObject = DynamicEffectBehavior.NewRecipe(DynamicEffectType.ScalableObject);
    scalableObject.FirstSourceFrame = 0;
    scalableObject.FrameCount = 1;
    scalableObject.TextureResourceKey = "Textures\\fx\\object.tex";
    DynamicEffectBehavior.ValidateAuthoring(scalableObject, DynamicObjectPlacement.Root)!
      .Field.Should().Be(DynamicBehaviorField.MeshResourceKey);

    var laser = DynamicEffectBehavior.NewRecipe(DynamicEffectType.Laser);
    laser.FirstSourceFrame = 0;
    laser.FrameCount = 1;
    laser.SpriteSheetColumnCount = 1;
    laser.SpriteSheetRowCount = 1;
    laser.RibbonHalfWidth = 0;
    laser.TextureResourceKey = "Textures\\fx\\laser.tex";
    DynamicEffectBehavior.ValidateAuthoring(laser, DynamicObjectPlacement.Root)!
      .Field.Should().Be(DynamicBehaviorField.RibbonHalfWidth);

    var translatedGroup = DynamicEffectBehavior.NewRecipe(DynamicEffectType.Group);
    translatedGroup.ChildEndTranslation = Vector3.One;
    DynamicEffectBehavior.ValidateAuthoring(translatedGroup, DynamicObjectPlacement.Root)!
      .Field.Should().Be(DynamicBehaviorField.ChildTranslation);
    DynamicEffectBehavior.ValidateAuthoring(translatedGroup, DynamicObjectPlacement.Child)
      .Should().BeNull();
  }

  [Fact]
  public void AuthoringValidationCoversEveryDescriptorRequirement()
  {
    foreach (var effectType in Enum.GetValues<DynamicEffectType>())
    {
      DynamicEffectBehavior.ValidateAuthoring(
        CreateValidRecipe(effectType),
        DynamicObjectPlacement.Child).Should().BeNull();
    }

    foreach (var effectType in Enum.GetValues<DynamicEffectType>()
      .Except(new[] { DynamicEffectType.Group, DynamicEffectType.Sphere }))
    {
      var recipe = CreateValidRecipe(effectType);
      recipe.FrameCount = 0;
      DynamicEffectBehavior.ValidateAuthoring(recipe, DynamicObjectPlacement.Child)!
        .Field.Should().Be(DynamicBehaviorField.Frames);
    }

    foreach (var effectType in new[]
      {
        DynamicEffectType.Explosion,
        DynamicEffectType.FlatExplosion,
        DynamicEffectType.Laser,
        DynamicEffectType.LaserWall,
        DynamicEffectType.Shockwave,
        DynamicEffectType.Line,
        DynamicEffectType.ElectricalCannon,
        DynamicEffectType.Lightning,
        DynamicEffectType.Smoke,
        DynamicEffectType.Keelwater
      })
    {
      var recipe = CreateValidRecipe(effectType);
      recipe.SpriteSheetColumnCount = 0;
      DynamicEffectBehavior.ValidateAuthoring(recipe, DynamicObjectPlacement.Child)!
        .Field.Should().Be(DynamicBehaviorField.SpriteSheet);
    }

    foreach (var effectType in new[]
      {
        DynamicEffectType.Laser,
        DynamicEffectType.LaserWall,
        DynamicEffectType.ElectricalCannon,
        DynamicEffectType.Lightning
      })
    {
      var recipe = CreateValidRecipe(effectType);
      recipe.RibbonHalfWidth = 0;
      DynamicEffectBehavior.ValidateAuthoring(recipe, DynamicObjectPlacement.Child)!
        .Field.Should().Be(DynamicBehaviorField.RibbonHalfWidth);
    }

    foreach (var effectType in Enum.GetValues<DynamicEffectType>()
      .Except(new[] { DynamicEffectType.Group }))
    {
      var recipe = CreateValidRecipe(effectType);
      recipe.TextureResourceKey = string.Empty;
      DynamicEffectBehavior.ValidateAuthoring(recipe, DynamicObjectPlacement.Child)!
        .Field.Should().Be(DynamicBehaviorField.TextureResourceKey);
    }
  }

  [Fact]
  public void LoadedDiagnosticsPreserveUnknownEffectsAndUseStructuralPlacement()
  {
    var data = new byte[0x9C];
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x00), uint.MaxValue);
    WriteSingle(data, 0x90, 1f);
    var extension = new DynamicEffectExtension(data, Array.Empty<byte>(), Array.Empty<byte>());

    DynamicEffectBehavior.Diagnose(extension, DynamicObjectPlacement.Root)
      .Select(finding => finding.Field)
      .Should().Equal(DynamicBehaviorField.EffectType, DynamicBehaviorField.ChildTranslation);
    DynamicEffectBehavior.Diagnose(extension, DynamicObjectPlacement.Child)
      .Select(finding => finding.Field)
      .Should().Equal(DynamicBehaviorField.EffectType);
    var unknownFinding = DynamicEffectBehavior.Diagnose(extension, DynamicObjectPlacement.Child)[0];
    unknownFinding.Code.Should().Be("ETM1009");
    unknownFinding.EventId.Should().Be(1009);
    unknownFinding.Severity.Should().Be(EarthTool.Common.Operations.DiagnosticSeverity.Warning);
    unknownFinding.PathSuffix.Should().Be(".Extension.EffectType");
    unknownFinding.Data["actual"].Should().Be("0xFFFFFFFF");
    extension.EffectType.Should().Be(uint.MaxValue);
  }

  [Fact]
  public void ConsumedRepresentationDiagnosticsCoverEveryKnownEffect()
  {
    var cases = new[]
    {
      RepresentationCase(data => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x04), 3),
        DynamicEffectType.Explosion, DynamicEffectType.ScalableObject, DynamicEffectType.MappedExplosion,
        DynamicEffectType.FlatExplosion, DynamicEffectType.Laser, DynamicEffectType.Lightning),
      RepresentationCase(data => WriteSingle(data, 0x54, 1f),
        DynamicEffectType.Explosion, DynamicEffectType.ScalableObject, DynamicEffectType.MappedExplosion,
        DynamicEffectType.FlatExplosion, DynamicEffectType.Laser, DynamicEffectType.LaserWall,
        DynamicEffectType.Lightning),
      RepresentationCase(data => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x0C), 1),
        Enum.GetValues<DynamicEffectType>().Except(new[]
          { DynamicEffectType.Group, DynamicEffectType.Sphere }).ToArray()),
      RepresentationCase(data =>
        {
          BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x10), 1);
          BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x14), 1);
          WriteSingle(data, 0x1C, 1f);
          WriteSingle(data, 0x20, 1f);
        }, DynamicEffectType.Explosion, DynamicEffectType.FlatExplosion, DynamicEffectType.Laser,
        DynamicEffectType.LaserWall, DynamicEffectType.Shockwave, DynamicEffectType.Line,
        DynamicEffectType.ElectricalCannon, DynamicEffectType.Lightning, DynamicEffectType.Smoke,
        DynamicEffectType.Keelwater),
      RepresentationCase(data => WriteSingle(data, 0x24, -1f),
        DynamicEffectType.Explosion, DynamicEffectType.Track, DynamicEffectType.MappedExplosion,
        DynamicEffectType.FlatExplosion, DynamicEffectType.Shockwave, DynamicEffectType.Line,
        DynamicEffectType.Smoke, DynamicEffectType.Keelwater),
      RepresentationCase(data => WriteSingle(data, 0x44, 0.5f),
        DynamicEffectType.Explosion, DynamicEffectType.FlatExplosion, DynamicEffectType.Shockwave,
        DynamicEffectType.Line, DynamicEffectType.Smoke, DynamicEffectType.Keelwater),
      RepresentationCase(data => WriteSingle(data, 0x48, 0.5f),
        DynamicEffectType.Laser, DynamicEffectType.LaserWall, DynamicEffectType.ElectricalCannon,
        DynamicEffectType.Lightning),
      RepresentationCase(data => WriteSingle(data, 0x60, 0.5f),
        Enum.GetValues<DynamicEffectType>().Except(new[]
          { DynamicEffectType.Group, DynamicEffectType.Track, DynamicEffectType.Keelwater }).ToArray()),
      RepresentationCase(data => WriteSingle(data, 0x6C, 0.5f),
        DynamicEffectType.Shockwave, DynamicEffectType.Line, DynamicEffectType.Smoke),
      RepresentationCase(data => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x70), 1),
        DynamicEffectType.Explosion, DynamicEffectType.Track, DynamicEffectType.ScalableObject,
        DynamicEffectType.MappedExplosion, DynamicEffectType.FlatExplosion, DynamicEffectType.Laser,
        DynamicEffectType.LaserWall, DynamicEffectType.ElectricalCannon, DynamicEffectType.Lightning,
        DynamicEffectType.Smoke),
      RepresentationCase(data => WriteSingle(data, 0x74, 0.5f),
        Enum.GetValues<DynamicEffectType>().Except(new[]
          { DynamicEffectType.Group, DynamicEffectType.Sphere }).ToArray()),
      RepresentationCase(data => WriteSingle(data, 0x7C, 2f), DynamicEffectType.ScalableObject),
      RepresentationCase(
        hasMeshResource: true,
        consumedBy: new[] { DynamicEffectType.ScalableObject }),
      RepresentationCase(data => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x50), 1),
        Enum.GetValues<DynamicEffectType>().Except(new[] { DynamicEffectType.Group }).ToArray()),
      RepresentationCase(
        hasTextureResource: true,
        consumedBy: Enum.GetValues<DynamicEffectType>()
          .Except(new[] { DynamicEffectType.Group }).ToArray())
    };

    foreach (var representationCase in cases)
    {
      foreach (var effectType in Enum.GetValues<DynamicEffectType>())
      {
        var data = CreateCanonicalExtensionData(effectType);
        representationCase.Mutate(data);
        var extension = new DynamicEffectExtension(
          data,
          representationCase.HasMeshResource ? new byte[] { 1 } : Array.Empty<byte>(),
          representationCase.HasTextureResource ? new byte[] { 1 } : Array.Empty<byte>());

        var hasInertFinding = DynamicEffectBehavior.Diagnose(extension, DynamicObjectPlacement.Child)
          .Any(finding => finding.Field == DynamicBehaviorField.InertRepresentations);
        hasInertFinding.Should().Be(!representationCase.ConsumedBy.Contains(effectType));
      }
    }
  }

  private static DynamicEffectExtension CreateExtension(uint effectType)
  {
    var data = CreateCanonicalExtensionData((DynamicEffectType)effectType);
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x00), effectType);
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x08), 2);
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x0C), 6);
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x10), 4);
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x14), 2);
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(0x18), 2);
    WriteSingle(data, 0x1C, 0.25f);
    WriteSingle(data, 0x20, 0.5f);
    return new DynamicEffectExtension(data, Array.Empty<byte>(), Array.Empty<byte>());
  }

  private static CanonicalDynamicRecipe CreateValidRecipe(DynamicEffectType effectType)
  {
    var recipe = DynamicEffectBehavior.NewRecipe(effectType);
    recipe.FrameCount = 1;
    recipe.SpriteSheetColumnCount = 1;
    recipe.SpriteSheetRowCount = 1;
    recipe.MeshResourceKey = effectType == DynamicEffectType.ScalableObject
      ? "Objects\\effect.msh"
      : string.Empty;
    recipe.TextureResourceKey = effectType == DynamicEffectType.Group
      ? string.Empty
      : "Textures\\fx\\effect.tex";
    return recipe;
  }

  private static byte[] CreateCanonicalExtensionData(DynamicEffectType effectType)
  {
    var data = new byte[0x9C];
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x00), (uint)effectType);
    WriteRectangle(data, 0x24);
    WriteRectangle(data, 0x34);
    WriteSingle(data, 0x44, 0.25f);
    WriteSingle(data, 0x48, 0.25f);
    WriteSingle(data, 0x60, 1f);
    WriteSingle(data, 0x64, 1f);
    WriteSingle(data, 0x68, 1f);
    WriteSingle(data, 0x6C, 1f);
    WriteSingle(data, 0x74, 1f);
    WriteSingle(data, 0x78, 1f);
    return data;
  }

  private static RepresentationTestCase RepresentationCase(
    Action<byte[]> mutate,
    params DynamicEffectType[] consumedBy)
  {
    return new RepresentationTestCase(mutate, false, false, consumedBy);
  }

  private static RepresentationTestCase RepresentationCase(
    bool hasMeshResource = false,
    bool hasTextureResource = false,
    params DynamicEffectType[] consumedBy)
  {
    return new RepresentationTestCase(_ => { }, hasMeshResource, hasTextureResource, consumedBy);
  }

  private static void WriteRectangle(byte[] data, int offset)
  {
    WriteSingle(data, offset, -0.25f);
    WriteSingle(data, offset + 4, 0.25f);
    WriteSingle(data, offset + 8, 0.25f);
    WriteSingle(data, offset + 12, -0.25f);
  }

  private static void WriteSingle(byte[] data, int offset, float value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(
      data.AsSpan(offset),
      BitConverter.SingleToInt32Bits(value));
  }

  private sealed class RepresentationTestCase
  {
    internal Action<byte[]> Mutate { get; }
    internal bool HasMeshResource { get; }
    internal bool HasTextureResource { get; }
    internal IReadOnlySet<DynamicEffectType> ConsumedBy { get; }

    internal RepresentationTestCase(
      Action<byte[]> mutate,
      bool hasMeshResource,
      bool hasTextureResource,
      IEnumerable<DynamicEffectType> consumedBy)
    {
      Mutate = mutate;
      HasMeshResource = hasMeshResource;
      HasTextureResource = hasTextureResource;
      ConsumedBy = consumedBy.ToHashSet();
    }
  }
}
