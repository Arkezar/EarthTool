using Collada141;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models.Elements;
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
      return model.Descriptor.SpotLights.Where(l => l.IsAvailable).Select((l, i) => (GetLight(l, i), GetLightNode(l, i)))
        .Concat(model.Descriptor.OmnidirectionalLights.Where(l => l.IsAvailable).Select((l, i) => (GetLight(l, i), GetLightNode(l, i))));
    }

    private Node GetLightNode(ILight light, int i)
    {
      var id = GetLightName(light, i);
      var node = new Node()
      {
        Id = id,
        Name = id
      };

      var rotationZrad = light switch
      {
        SpotLight sl => sl.Direction / 255f * Math.PI / 2,
        _ => 0
      };

      var rotationYrad = light switch
      {
        SpotLight sl => -Math.PI / 2f - sl.Tilt,
        _ => 0
      };

      var translation = Matrix4x4.CreateTranslation(light.Value);
      var rotZ = Matrix4x4.CreateRotationZ((float)rotationZrad);
      var rotY = Matrix4x4.CreateRotationY((float)rotationYrad);
      var transformMatrix = rotZ * rotY * translation;

      node.Matrix.Add(new Matrix()
      {
        Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}", transformMatrix.M11,
                                                                                                                                   transformMatrix.M21,
                                                                                                                                   transformMatrix.M31,
                                                                                                                                   transformMatrix.M41,
                                                                                                                                   transformMatrix.M12,
                                                                                                                                   transformMatrix.M22,
                                                                                                                                   transformMatrix.M32,
                                                                                                                                   transformMatrix.M42,
                                                                                                                                   transformMatrix.M13,
                                                                                                                                   transformMatrix.M23,
                                                                                                                                   transformMatrix.M33,
                                                                                                                                   transformMatrix.M43,
                                                                                                                                   transformMatrix.M14,
                                                                                                                                   transformMatrix.M24,
                                                                                                                                   transformMatrix.M34,
                                                                                                                                   transformMatrix.M44)
      });

      var instanceGeometry = new Instance_Light()
      {
        Url = $"#{id}"
      };

      node.Instance_Light.Add(instanceGeometry);

      return node;
    }

    private Light GetLight(ILight light, int i)
    {
      return new Light()
      {
        Id = GetLightName(light, i),
        Name = GetLightName(light, i),
        Technique_Common = light switch
        {
          SpotLight sl => GetSpotLight(sl),
          OmniLight ol => GetPointLight(ol),
          _ => null
        }
      };
    }

    private string GetLightName(ILight light, int i)
      => $"{light.GetType().Name}-{i}";

    private LightTechnique_Common GetSpotLight(SpotLight light)
    {
      return new LightTechnique_Common()
      {
        Spot = new LightTechnique_CommonSpot()
        {
          Color = new TargetableFloat3()
          {
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", light.Color.R / 255f, light.Color.G / 255f, light.Color.B / 255f)
          },
          Constant_Attenuation = new TargetableFloat()
          {
            Value = light.Length
          },
          Linear_Attenuation = new TargetableFloat()
          {
            Value = light.Ambience
          },
          Quadratic_Attenuation = new TargetableFloat()
          {
            Value = 0
          },
          Falloff_Angle = new TargetableFloat()
          {
            Value = light.Width * 180.0 / Math.PI
          }
        }
      };
    }

    private LightTechnique_Common GetPointLight(OmniLight light)
    {
      return new LightTechnique_Common()
      {
        Point = new LightTechnique_CommonPoint()
        {
          Color = new TargetableFloat3()
          {
            Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", light.Color.R / 255f, light.Color.G / 255f, light.Color.B / 255f)
          },
          Constant_Attenuation = new TargetableFloat()
          {
            Value = light.Radius
          },
          Linear_Attenuation = new TargetableFloat()
          {
            Value = 0
          },
          Quadratic_Attenuation = new TargetableFloat()
          {
            Value = 0
          }
        }
      };
    }
  }

}