using Collada141;
using EarthTool.DAE.Extensions;
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
      return GetSlots(model.BaseHeader.Slots.BarrelMuzzels, "BarrelMuzzle")
        .Concat(GetSlots(model.BaseHeader.Slots.CenterPivot, "CenterPivot"))
        .Concat(GetSlots(model.BaseHeader.Slots.Chimneys, "Chimney"))
        .Concat(GetSlots(model.BaseHeader.Slots.Exhausts, "Exhaust"))
        .Concat(GetSlots(model.BaseHeader.Slots.HitSpots, "HitSpot"))
        .Concat(GetSlots(model.BaseHeader.Slots.InterfacePivot, "InterfacePivot"))
        .Concat(GetSlots(model.BaseHeader.Slots.KeelTraces, "KeelTrace"))
        .Concat(GetSlots(model.BaseHeader.Slots.LandingSpot, "LandingSpot"))
        .Concat(GetSlots(model.BaseHeader.Slots.ProductionSpotStart, "ProductionSpotStart"))
        .Concat(GetSlots(model.BaseHeader.Slots.ProductionSpotEnd, "ProductionSpotEnd"))
        .Concat(GetSlots(model.BaseHeader.Slots.SmokeSpots, "SmokeSpot"))
        .Concat(GetSlots(model.BaseHeader.Slots.SmokeTraces, "SmokeTrace"))
        .Concat(GetSlots(model.BaseHeader.Slots.TurretMuzzels, "TurretMuzzel"))
        .Concat(GetSlots(model.BaseHeader.Slots.Turrets, "Turret"))
        .Concat(GetSlots(model.BaseHeader.Slots.Unknown, "Unknown"))
        .Concat(GetSlots(model.BaseHeader.Slots.UnloadPoints, "UnloadPoint"));
    }

    private IEnumerable<(Light Slot, Node SlotNode)> GetSlots(IEnumerable<ISlot> slots, string name)
      => slots.Select((slot, index) => (Slot: slot, Number: index + 1))
        .Where(item => item.Slot.IsValid)
        .Select(item => (GetLight(item.Number, name), GetLightNode(item.Slot, item.Number, name)));

    private Node GetLightNode(ISlot slot, int i, string name)
    {
      var id = $"{name}-{i}";
      var node = new Node()
      {
        Id = id,
        Name = id
      };
      node.AddAttachmentMetadata(slot);

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
