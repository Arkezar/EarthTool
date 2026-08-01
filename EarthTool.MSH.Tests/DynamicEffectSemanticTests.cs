using AwesomeAssertions;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System.Numerics;

namespace EarthTool.MSH.Tests;

public class DynamicEffectSemanticTests
{
  private static readonly Guid _creationGuid = new("12345678-9abc-def0-1234-56789abcdef0");
  private static readonly MeshAssetLineageId _lineageId = new(
    new Guid("11111111-2222-3333-4444-555555555555"));

  [Fact]
  public void HelpersRequireExplicitTimePhaseScaleLightAndRandomnessInputs()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 6, 2), 4, 2);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(0, 2, 4, -2),
      new EffectRectangle(2, 4, 8, -4),
      0.5f);
    var alpha = new CanonicalDynamicAlpha(1, 0, DynamicAlphaTiming.LifetimeProgress);
    var recipe = DynamicEffectRecipes.Smoke(
      sprite,
      shape,
      "Textures\\fx\\smoke.tex",
      new Vector3(0.5f, 0.25f, 1f),
      2,
      alpha,
      false)
      .SetChildTranslation(Vector3.Zero, new Vector3(10, 20, 30));
    var extension = BuildAsChild(recipe);

    DynamicEffectSemantics.TrySelectFrame(extension, DynamicEffectEvaluationContext.Primary,
      10, 7, 5, out var frame, out var frameFailure)
      .Should().BeTrue();
    frameFailure.Should().Be(DynamicSemanticFailure.None);
    frame.SourceFrame.Should().Be(4);
    frame.FrameIndex.Should().Be(2);
    frame.Phase.Should().BeApproximately(2f / 6f, 0.000001f);
    DynamicEffectSemantics.TrySelectTextureRegion(extension, DynamicEffectEvaluationContext.Primary,
      frame, 2, out var texture, out _)
      .Should().BeTrue();
    texture.Should().Be(new DynamicTextureRegion(1, 0, 0, 1, 0.5f, 2));

    DynamicEffectSemantics.TryInterpolateEffectRectangle(extension, DynamicEffectEvaluationContext.Primary,
      0.25f, out var rectangle, out _)
      .Should().BeTrue();
    rectangle.Should().Be(new EffectRectangle(0.5f, 2.5f, 5, -2.5f));
    DynamicEffectSemantics.TryInterpolateAlpha(extension, DynamicEffectEvaluationContext.Primary,
      0.2f, 0.75f, out var interpolatedAlpha, out _)
      .Should().BeTrue();
    interpolatedAlpha.Should().Be(0.25f);
    DynamicEffectSemantics.TryInterpolateAlpha(extension,
      DynamicEffectEvaluationContext.AttachedParticle,
      0.2f,
      0.75f,
      out var attachedAlpha,
      out _).Should().BeTrue();
    attachedAlpha.Should().Be(0.8f);
    DynamicEffectSemantics.TryInterpolateChildTranslation(extension, 0.25f, out var translation, out _)
      .Should().BeTrue();
    translation.Should().Be(new Vector3(2.5f, 5, 7.5f));
    DynamicEffectSemantics.TryEvaluateVisibleEffectColor(
      extension,
      DynamicEffectEvaluationContext.Primary,
      new Vector3(0.25f, 0.75f, 2),
      255,
      out var color,
      out _).Should().BeTrue();
    color.Should().Be(new Vector3(63.75f, 63.75f, 255));

    DynamicEffectSemantics.TryEvaluateTerrainLightIntensity(
      DynamicLightType.Random,
      2,
      8,
      10,
      3999,
      out var intensity,
      out _).Should().BeTrue();
    intensity.Should().BeApproximately(1.1999f, 0.000001f);
  }

  [Fact]
  public void HelpersReturnStructuredFailuresForUnsafeDomains()
  {
    var extension = BuildAsChild(DynamicEffectRecipes.Group());

    DynamicEffectSemantics.TrySelectFrame(extension, DynamicEffectEvaluationContext.AttachedParticle,
      0, 0, 0, out _, out var frameFailure)
      .Should().BeFalse();
    frameFailure.Should().Be(DynamicSemanticFailure.InvalidLifetime);
    DynamicEffectSemantics.TryEvaluateTerrainLightIntensity(
      DynamicLightType.Random,
      1,
      1,
      2,
      null,
      out _,
      out var randomFailure).Should().BeFalse();
    randomFailure.Should().Be(DynamicSemanticFailure.RandomSampleRequired);
  }

  [Fact]
  public void HelpersPreserveExtrapolationAndUnknownLightBehavior()
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 0);
    var alpha = new CanonicalDynamicAlpha(1, 1, DynamicAlphaTiming.FramePhase);
    var extension = BuildAsChild(DynamicEffectRecipes.ScalableObject(
      frames,
      "Objects\\effect.msh",
      "Textures\\fx\\object.tex",
      -2,
      2,
      Vector3.One,
      alpha,
      false,
      new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.Zero)));

    DynamicEffectSemantics.TryInterpolateModelScale(extension, 1.5f, out var scale, out _)
      .Should().BeTrue();
    scale.Should().Be(4);
    DynamicEffectSemantics.TryEvaluateTerrainLightIntensity(
      uint.MaxValue,
      2,
      8,
      10,
      null,
      out var unknownIntensity,
      out _).Should().BeTrue();
    unknownIntensity.Should().Be(1);
    DynamicEffectSemantics.TryEvaluateTerrainLightIntensity(
      DynamicLightType.Pyramid,
      2,
      8,
      10,
      null,
      out var pyramidIntensity,
      out _).Should().BeTrue();
    pyramidIntensity.Should().Be(0.4f);
    DynamicEffectSemantics.TrySelectSphereFrame(10, 7, out var sphereFrame, out _)
      .Should().BeTrue();
    sphereFrame.Should().Be(4);

    var sphere = BuildAsChild(DynamicEffectRecipes.Sphere(
      "Textures\\fx\\sphere.tex", Vector3.One, false));
    DynamicEffectSemantics.TrySelectFrame(sphere, DynamicEffectEvaluationContext.Primary,
      10, 7, 5, out _, out var sphereFailure).Should().BeFalse();
    sphereFailure.Should().Be(DynamicSemanticFailure.InapplicableEffect);
  }

  [Fact]
  public void VisibleColorHelperRejectsOverflowBeforeItsUpperClamp()
  {
    var sprite = new CanonicalDynamicSpriteSheet(
      new CanonicalDynamicFrameSequence(0, 1, 0),
      1,
      1);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 1, 1, -1),
      new EffectRectangle(-1, 1, 1, -1),
      0.25f);
    var extension = BuildAsChild(DynamicEffectRecipes.Smoke(
      sprite,
      shape,
      "Textures\\fx\\smoke.tex",
      Vector3.One,
      2,
      new CanonicalDynamicAlpha(1, 1, DynamicAlphaTiming.FramePhase),
      false));

    DynamicEffectSemantics.TryEvaluateVisibleEffectColor(
      extension,
      DynamicEffectEvaluationContext.Primary,
      new Vector3(float.MaxValue),
      1,
      out _,
      out var failure).Should().BeFalse();
    failure.Should().Be(DynamicSemanticFailure.NonFiniteInput);
  }

  private static DynamicEffectExtension BuildAsChild(CanonicalDynamicObject child)
  {
    var root = DynamicEffectRecipes.Group([child]);
    var build = DynamicMeshBuilder.Create(_creationGuid, _lineageId).SetRoot(root).Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!.RootDynamicObject.Children.Should().ContainSingle().Subject.Extension;
  }
}
