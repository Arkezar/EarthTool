using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.PAR.Models
{
  public abstract class VerticalTransporter : EquipableEntity
  {
    public VerticalTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      VehicleSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      VerticalVehicleAnimationType = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int VehicleSpeed { get; }

    public int VerticalVehicleAnimationType { get; }
  }
}
