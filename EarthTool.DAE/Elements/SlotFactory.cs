using Collada141;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace EarthTool.DAE.Elements
{
  public class SlotFactory
  {
    public IEnumerable<(Light Slot, Node SlotNode)> GetSlots(IMesh model)
    {
      return model.Descriptor.Slots.BarrelMuzzels.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "BarrelMuzzle"), GetLightNode(s, i, "BarrelMuzzle")))
        .Concat(model.Descriptor.Slots.CenterPivot.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "CenterPivot"), GetLightNode(s, i, "CenterPivot"))))
        .Concat(model.Descriptor.Slots.Chimneys.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "Chimney"), GetLightNode(s, i, "Chimney"))))
        .Concat(model.Descriptor.Slots.Exhausts.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "Exhaust"), GetLightNode(s, i, "Exhaust"))))
        .Concat(model.Descriptor.Slots.HitSpots.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "HitSpot"), GetLightNode(s, i, "HitSpot"))))
        .Concat(model.Descriptor.Slots.InterfacePivot.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "InterfacePivot"), GetLightNode(s, i, "InterfacePivot"))))
        .Concat(model.Descriptor.Slots.KeelTraces.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "KeelTrace"), GetLightNode(s, i, "KeelTrace"))))
        .Concat(model.Descriptor.Slots.LandingSpot.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "LandingSpot"), GetLightNode(s, i, "LandingSpot"))))
        .Concat(model.Descriptor.Slots.ProductionSpotStart.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "ProductionSpotStart"), GetLightNode(s, i, "ProductionSpotStart"))))
        .Concat(model.Descriptor.Slots.ProductionSpotEnd.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "ProductionSpotEnd"), GetLightNode(s, i, "ProductionSpotEnd"))))
        .Concat(model.Descriptor.Slots.SmokeSpots.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "SmokeSpot"), GetLightNode(s, i, "SmokeSpot"))))
        .Concat(model.Descriptor.Slots.SmokeTraces.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "SmokeTrace"), GetLightNode(s, i, "SmokeTrace"))))
        .Concat(model.Descriptor.Slots.TurretMuzzels.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "TurretMuzzel"), GetLightNode(s, i, "TurretMuzzel"))))
        .Concat(model.Descriptor.Slots.Turrets.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "Turret"), GetLightNode(s, i, "Turret"))))
        .Concat(model.Descriptor.Slots.UnloadPoints.Where(s => s.IsValid).Select((s, i) => (GetLight(i, "UnloadPoint"), GetLightNode(s, i, "UnloadPoint"))));
    }

    private Node GetLightNode(ISlot slot, int i, string name)
    {
      var id = $"{name}-{i}";
      var node = new Node()
      {
        Id = id,
        Name = id
      };

      var translate = Matrix4x4.Identity;
      translate.Translation = slot.Position.Value;

      var rotationXdeg = slot.Direction;
      var rotationXcos = (float)Math.Cos(rotationXdeg);
      var rotationXsin = (float)Math.Sin(rotationXdeg);

      var rotationX = new Matrix4x4(1, 0, 0, 0,
                                    0, rotationXcos, rotationXsin, 0,
                                    0, -rotationXsin, rotationXcos, 0,
                                    0, 0, 0, 1);

      var rotationYdeg = -Math.PI / 2f;

      var rotationYcos = (float)Math.Cos(rotationYdeg);
      var rotationYsin = (float)Math.Sin(rotationYdeg);

      var rotationY = new Matrix4x4(rotationYcos, 0, -rotationYsin, 0,
                                    0, 1, 0, 0,
                                    rotationYsin, 0, rotationYcos, 0,
                                    0, 0, 0, 1);


      var transformMatrix = rotationX * rotationY;
      transformMatrix.Translation = slot.Position.Value;

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

    private Light GetLight(int i, string name)
    {
      return new Light()
      {
        Id = $"{name}-{i}",
        Name = $"{name}-{i}",
        Technique_Common = new LightTechnique_Common()
        {
          Directional = new LightTechnique_CommonDirectional()
          {
            Color = new TargetableFloat3()
            {
              Value = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", 0f, 0f, 0f)
            }
          }
        }
      };
    }
  }
}
