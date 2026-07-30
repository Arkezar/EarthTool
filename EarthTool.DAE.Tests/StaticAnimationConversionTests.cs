using EarthTool.DAE.Elements;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using System.Globalization;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace EarthTool.DAE.Tests;

public class StaticAnimationConversionTests
{
  [Fact]
  public void ScaleOnlyTrackIsExportedAsTransformAnimation()
  {
    var part = Part(
      scales: new[] { new Vector(2, 3, 4) },
      translations: Array.Empty<IVector>(),
      rotations: Array.Empty<IRotationFrame>());

    var animation = Assert.Single(new AnimationsFactory()
      .GetAnimations(new[] { new PartNode(0, part) }, "fixture"));

    var transform = Assert.Single(animation.AnimationProperty);
    var output = transform.Source.Single(source => source.Id.EndsWith("-output", StringComparison.Ordinal));
    Assert.Equal(16ul, output.Float_Array.Count);
    Assert.Equal(new float[]
    {
      2, 0, 0, 0,
      0, 3, 0, 0,
      0, 0, 4, 0,
      0, 0, 0, 1
    }, ParseFloats(output.Float_Array.Value));
  }

  [Fact]
  public void UnequalTrackCountsExportOneTransformPerLongestTrack()
  {
    var part = Part(
      scales: new IVector[] { new Vector(1, 1, 1), new Vector(2, 2, 2), new Vector(3, 3, 3) },
      translations: new IVector[] { new Vector(10, 20, 30), new Vector(40, 50, 60) },
      rotations: new IRotationFrame[]
      {
        new RotationFrame { TransformationMatrix = Matrix4x4.Identity }
      });

    var animation = Assert.Single(new AnimationsFactory()
      .GetAnimations(new[] { new PartNode(0, part) }, "fixture"));

    var transform = Assert.Single(animation.AnimationProperty);
    var input = transform.Source.Single(source => source.Id.EndsWith("-input", StringComparison.Ordinal));
    var output = transform.Source.Single(source => source.Id.EndsWith("-output", StringComparison.Ordinal));
    Assert.Equal(3ul, input.Float_Array.Count);
    Assert.Equal(48ul, output.Float_Array.Count);
    Assert.Equal(48, ParseFloats(output.Float_Array.Value).Length);
  }

  private static ModelPart Part(
    IEnumerable<IVector> scales,
    IEnumerable<IVector> translations,
    IEnumerable<IRotationFrame> rotations)
  {
    return new ModelPart
    {
      Animations = new Animations
      {
        ScaleFrames = scales,
        TranslationFrames = translations,
        RotationFrames = rotations
      },
      Offset = new Vector()
    };
  }

  private static float[] ParseFloats(string value)
  {
    return value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .Select(item => float.Parse(item, CultureInfo.InvariantCulture))
      .ToArray();
  }
}
