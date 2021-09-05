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
    public ResourceTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      ResourceVehicleType = BitConverter.ToInt32(data.ReadBytes(4));
      AnimatedTransporterStop = BitConverter.ToInt32(data.ReadBytes(4));
      ShowVideoPerTransportersCount = BitConverter.ToInt32(data.ReadBytes(4));
      TotalOrbitalMoney = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int ResourceVehicleType { get; }

    public int AnimatedTransporterStop { get; }

    public int ShowVideoPerTransportersCount { get; }

    public int TotalOrbitalMoney { get; }
  }
}
