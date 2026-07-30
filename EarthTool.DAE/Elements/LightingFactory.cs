using Collada141;
using EarthTool.DAE.Extensions;
using EarthTool.MSH.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Light = Collada141.Light;

namespace EarthTool.DAE.Elements
{
  public class LightingFactory
  {
    public IEnumerable<(Light Light, Node LightNode)> GetLights(IMesh model)
    {
      var spotSlots = model.BaseHeader.Slots.Headlights.ToArray();
      var omniSlots = model.BaseHeader.Slots.Omnilights.ToArray();
      return model.BaseHeader.SpotLights
        .Select((light, index) => (Light: (IStaticLight)light, Index: index, Active: spotSlots[index].IsValid))
        .Concat(model.BaseHeader.OmnidirectionalLights
          .Select((light, index) => (Light: (IStaticLight)light, Index: index, Active: omniSlots[index].IsValid)))
        .Where(item => item.Active)
        .Select(item => (GetLight(item.Light, item.Index + 1), GetLightNode(item.Light, item.Index + 1)));
    }

    private Node GetLightNode(IStaticLight light, int sourceNumber)
    {
      var id = GetLightName(light, sourceNumber);
      var node = new Node
      {
        Id = id,
        Name = id
      };
      var transformMatrix = Matrix4x4.CreateTranslation(light.Position);
      node.Matrix.Add(new Matrix
      {
        Value = string.Format(CultureInfo.InvariantCulture,
          "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}",
          transformMatrix.M11, transformMatrix.M21, transformMatrix.M31, transformMatrix.M41,
          transformMatrix.M12, transformMatrix.M22, transformMatrix.M32, transformMatrix.M42,
          transformMatrix.M13, transformMatrix.M23, transformMatrix.M33, transformMatrix.M43,
          transformMatrix.M14, transformMatrix.M24, transformMatrix.M34, transformMatrix.M44)
      });
      node.Instance_Light.Add(new Instance_Light { Url = $"#{id}" });
      return node;
    }

    private Light GetLight(IStaticLight light, int sourceNumber)
    {
      var result = new Light
      {
        Id = GetLightName(light, sourceNumber),
        Name = GetLightName(light, sourceNumber),
        Technique_Common = light switch
        {
          ISpotLight spot => GetSpotLight(spot),
          IOmniLight _ => GetPointLight(),
          _ => throw new InvalidOperationException($"Unsupported static light type {light.GetType().Name}.")
        }
      };
      result.AddStaticLightMetadata(light, sourceNumber);
      return result;
    }

    private static string GetLightName(IStaticLight light, int sourceNumber)
      => light switch
      {
        ISpotLight _ => $"SpotLight-{sourceNumber}",
        IOmniLight _ => $"OmniLight-{sourceNumber}",
        _ => throw new InvalidOperationException($"Unsupported static light type {light.GetType().Name}.")
      };

    private static LightTechnique_Common GetSpotLight(ISpotLight light)
      => new LightTechnique_Common
      {
        Spot = new LightTechnique_CommonSpot
        {
          Color = new TargetableFloat3 { Value = "1 1 1" },
          Falloff_Angle = new TargetableFloat
          {
            Value = 2 * Math.Atan(light.ConeHalfAngleTangent) * 180 / Math.PI
          }
        }
      };

    private static LightTechnique_Common GetPointLight()
      => new LightTechnique_Common
      {
        Point = new LightTechnique_CommonPoint
        {
          Color = new TargetableFloat3 { Value = "1 1 1" }
        }
      };
  }
}
