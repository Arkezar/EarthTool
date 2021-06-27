using Collada141;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace EarthTool.MSH.Converters.Collada.Elements
{
  public class LightingFactory
  {
    public IEnumerable<(Light Light, Node LightNode)> GetLights(Model model)
    {
      return model.Lights.Where(l => l.IsAvailable).Select((l, i) => (GetLight(l, i), GetLightNode(l, i)));
    }

    private Node GetLightNode(Models.Elements.Light light, int i)
    {
      var id = $"Light-{i}";
      var node = new Node()
      {
        Id = id,
        Name = id
      };

      var rotationZdeg = Math.PI / 180.0 * (light.Direction * 360.0 / 255.0);
      var rotationYdeg = Math.PI / 180.0 * (-90 - 180 / Math.PI * light.Tilt);

      var rotationZcos = (float)Math.Cos(rotationZdeg);
      var rotationZsin = (float)Math.Sin(rotationZdeg);

      var rotationZ = new Matrix4x4(rotationZcos, -rotationZsin, 0, 0,
                                    rotationZsin, rotationZcos, 0, 0,
                                    0, 0, 1, 0,
                                    0, 0, 0, 1);

      var rotationYcos = (float)Math.Cos(rotationYdeg);
      var rotationYsin = (float)Math.Sin(rotationYdeg);

      var rotationY = new Matrix4x4(rotationYcos, 0, rotationYsin, 0,
                                    0, 1, 0, 0,
                                    -rotationYsin, 0, rotationYcos, 0,
                                    0, 0, 0, 1);

      var rotation = rotationZ * rotationY;

      var transformMatrix = rotation;
      transformMatrix.M14 = light.X;
      transformMatrix.M24 = light.Y;
      transformMatrix.M34 = light.Z;

      node.Matrix.Add(new Matrix()
      {
        Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}", transformMatrix.M11,
                                                                                                                                     transformMatrix.M12,
                                                                                                                                     transformMatrix.M13,
                                                                                                                                     transformMatrix.M14,
                                                                                                                                     transformMatrix.M21,
                                                                                                                                     transformMatrix.M22,
                                                                                                                                     transformMatrix.M23,
                                                                                                                                     transformMatrix.M24,
                                                                                                                                     transformMatrix.M31,
                                                                                                                                     transformMatrix.M32,
                                                                                                                                     transformMatrix.M33,
                                                                                                                                     transformMatrix.M34,
                                                                                                                                     transformMatrix.M41,
                                                                                                                                     transformMatrix.M42,
                                                                                                                                     transformMatrix.M43,
                                                                                                                                     transformMatrix.M44)
      });

      var instanceGeometry = new Instance_Light()
      {
        Url = $"#{id}"
      };

      node.Instance_Light.Add(instanceGeometry);

      return node;
    }

    private Light GetLight(Models.Elements.Light light, int i)
    {
      return new Light()
      {
        Id = $"Light-{i}",
        Name = $"Light-{i}",
        Technique_Common = i != 4 ? GetSpotLight(light) : GetPointLight(light)
      };
    }

    private LightTechnique_Common GetSpotLight(Models.Elements.Light light)
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

    private LightTechnique_Common GetPointLight(Models.Elements.Light light)
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
            Value = light.Length
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
