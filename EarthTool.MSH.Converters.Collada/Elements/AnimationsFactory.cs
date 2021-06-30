using Collada141;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class AnimationsFactory
  {
    const float FRAMERATE = 24f;

    public IEnumerable<Animation> GetAnimations(IEnumerable<ModelPart> parts, string modelName)
    {
      return parts.Select((p, i) => GetAnimation(p, i, modelName)).Where(a => a != null);
    }

    private Animation GetAnimation(ModelPart part, int i, string modelName)
    {
      var frames = Math.Max(part.Animations.MovementFrames.Count, part.Animations.RotationFrames.Count);

      if (frames == 0)
      {
        return null;
      }

      var id = $"{modelName}-Part-{i}";
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

    private Source GetOutputSource(string id, ModelPart part, int count)
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

    private string GetOutputValue(ModelPart part)
    {
      var transforms = part.Animations.RotationFrames.Select(f => f.TransformationMatrix).ToArray();
      if (!transforms.Any())
      {
        transforms = Enumerable.Repeat(Matrix4x4.Identity, part.Animations.MovementFrames.Count).ToArray();
      }

      for (var i = 0; i < transforms.Count(); i++)
      {
        if (part.Animations.MovementFrames.Count == transforms.Length)
        {
          transforms[i].M14 = part.Animations.MovementFrames[i].X;
          transforms[i].M24 = part.Animations.MovementFrames[i].Y;
          transforms[i].M34 = part.Animations.MovementFrames[i].Z;
        }
        else if (part.Animations.MovementFrames.Count == 0)
        {
          transforms[i].M14 = part.Offset.X;
          transforms[i].M24 = part.Offset.Y;
          transforms[i].M34 = part.Offset.Z;
        }
      }

      return string.Join(" ", transforms.Select(t => MatrixToString(t)));
    }

    private string MatrixToString(Matrix4x4 transformMatrix)
    {
      return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}", transformMatrix.M11, transformMatrix.M12, transformMatrix.M13, transformMatrix.M14,
                                                                                                                                  transformMatrix.M21, transformMatrix.M22, transformMatrix.M23, transformMatrix.M24,
                                                                                                                                  transformMatrix.M31, transformMatrix.M32, transformMatrix.M33, transformMatrix.M34,
                                                                                                                                  transformMatrix.M41, transformMatrix.M42, transformMatrix.M43, transformMatrix.M44);
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
        Value = string.Join(" ", Enumerable.Range(0, count).Select(i => (i / FRAMERATE).ToString(CultureInfo.InvariantCulture)))
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
