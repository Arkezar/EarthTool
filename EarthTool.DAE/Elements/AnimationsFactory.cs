using Collada141;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace EarthTool.DAE.Elements
{
  public class AnimationsFactory
  {
    const float FRAMERATE = 23.976f;

    public IEnumerable<Animation> GetAnimations(IEnumerable<PartNode> parts, string modelName)
    {
      return parts.SelectMany((p, i) => p.Parts.Select((sp, idx) => GetAnimation(sp, i, idx, modelName)) ).Where(a => a != null);
    }

    private Animation GetAnimation(IModelPart part, int i, int idx, string modelName)
    {
      var frames = Math.Max(part.Animations.TranslationFrames.Count(), part.Animations.RotationFrames.Count());

      if (frames == 0)
      {
        return null;
      }

      var id = $"Part-{i}-{idx}";
      var animationContainer = new Animation
      {
        Id = $"{id}-animation",
        Name = $"{id}-animation"
      };

      var animation = new Animation
      {
        Id = $"{id}-transform",
        Name = $"{id}-transform"
      };

      var inputSource = GetInputSource(id, frames);
      var outputSource = GetOutputSource(id, part, frames);
      var interpolationSource = GetInterpolationSource(id, frames);

      animation.Source.Add(inputSource);
      animation.Source.Add(outputSource);
      animation.Source.Add(interpolationSource);

      var sampler = new Sampler
      {
        Id = $"{id}-transform-sampler"
      };
      sampler.Input.Add(new InputLocal
      {
        Semantic = "INPUT",
        Source = $"#{id}-transform-input"
      });
      sampler.Input.Add(new InputLocal
      {
        Semantic = "OUTPUT",
        Source = $"#{id}-transform-output"
      });
      sampler.Input.Add(new InputLocal
      {
        Semantic = "INTERPOLATION",
        Source = $"#{id}-transform-interpolation"
      });
      animation.Sampler.Add(sampler);

      animation.Channel.Add(new Channel
      {
        Source = $"#{id}-transform-sampler",
        Target = $"{id}/transform"
      });

      animationContainer.AnimationProperty.Add(animation);
      return animationContainer;
    }

    private Source GetOutputSource(string id, IModelPart part, int count)
    {
      var source = new Source
      {
        Id = $"{id}-transform-output"
      };

      source.Float_Array = new Float_Array
      {
        Count = (ulong)count * 16,
        Value = GetOutputValue(part)
      };

      var accessor = new Accessor
      {
        Count = (ulong)count,
        Source = $"#{source.Id}-array",
        Stride = 16
      };

      accessor.Param.Add(new Param
      {
        Name = "TRANSFORM",
        Type = "float4x4"
      });

      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = accessor
      };

      return source;
    }

    private string GetOutputValue(IModelPart part)
    {
      var transforms = part.Animations.RotationFrames.Select(f => f.TransformationMatrix).ToArray();
      if (!transforms.Any())
      {
        transforms = Enumerable.Repeat(Matrix4x4.Identity, part.Animations.TranslationFrames.Count()).ToArray();
      }

      for (var i = 0; i < transforms.Count(); i++)
      {
        Matrix4x4.Decompose(transforms[i], out _, out var rotation, out _);
        rotation.Y = -rotation.Y;
        var matrix = Matrix4x4.CreateFromQuaternion(rotation);
        var translationMatrix = Matrix4x4.CreateTranslation(part.Animations.TranslationFrames.ElementAtOrDefault(i)?.Value ?? part.Offset.Value);
        transforms[i] = matrix * translationMatrix;
      }

      return string.Join(" ", transforms.Select(t => MatrixToString(t)));
    }

    private string MatrixToString(Matrix4x4 transformMatrix)
    {
      return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}", transformMatrix.M11, transformMatrix.M21, transformMatrix.M31, transformMatrix.M41,
                                                                                                                                  transformMatrix.M12, transformMatrix.M22, transformMatrix.M32, transformMatrix.M42,
                                                                                                                                  transformMatrix.M13, transformMatrix.M23, transformMatrix.M33, transformMatrix.M43,
                                                                                                                                  transformMatrix.M14, transformMatrix.M24, transformMatrix.M34, transformMatrix.M44);
    }

    private Source GetInterpolationSource(string id, int count)
    {
      var source = new Source
      {
        Id = $"{id}-transform-interpolation"
      };

      source.Name_Array = new Name_Array
      {
        Id = $"{source.Id}-array",
        Count = (ulong)count,
        Value = string.Join(" ", Enumerable.Range(0, count).Select(i => "LINEAR"))
      };

      var accessor = new Accessor
      {
        Count = (ulong)count,
        Source = $"#{source.Id}-array"
      };

      accessor.Param.Add(new Param
      {
        Name = "INTERPOLATION",
        Type = "name"
      });

      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = accessor
      };

      return source;
    }

    private Source GetInputSource(string id, int count)
    {
      var source = new Source
      {
        Id = $"{id}-transform-input"
      };

      source.Float_Array = new Float_Array
      {
        Id = $"{source.Id}-array",
        Count = (ulong)count,
        Value = string.Join(" ", Enumerable.Range(1, count).Select(i => (i / FRAMERATE).ToString(CultureInfo.InvariantCulture)))
      };

      var accessor = new Accessor
      {
        Count = (ulong)count,
        Source = $"#{source.Id}-array"
      };

      accessor.Param.Add(new Param
      {
        Name = "TIME",
        Type = "float"
      });

      source.Technique_Common = new SourceTechnique_Common
      {
        Accessor = accessor
      };

      return source;
    }
  }
}
