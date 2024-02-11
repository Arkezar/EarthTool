using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class ResourceTransporter : VerticalTransporter
  {
    public ResourceTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      ResourceVehicleType = GetInteger(data);
      AnimatedTransporterStop = GetInteger(data);
      ShowVideoPerTransportersCount = GetInteger(data);
      TotalOrbitalMoney = GetInteger(data);
    }

    public int ResourceVehicleType { get; }

    public int AnimatedTransporterStop { get; }

    public int ShowVideoPerTransportersCount { get; }

    public int TotalOrbitalMoney { get; }
  }
}
