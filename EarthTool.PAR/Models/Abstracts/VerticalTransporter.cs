using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.PAR.Models
{
  public abstract class VerticalTransporter : EquipableEntity
  {
    public VerticalTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      VehicleSpeed = GetInteger(data);
      VerticalVehicleAnimationType = GetInteger(data);
    }

    public int VehicleSpeed { get; }

    public int VerticalVehicleAnimationType { get; }
  }
}
