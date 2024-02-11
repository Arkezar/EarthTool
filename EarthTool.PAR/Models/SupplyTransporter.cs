using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class SupplyTransporter : Vehicle
  {
    public SupplyTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      AmmoCapacity = GetInteger(data);
      AnimSupplyDownStart = GetInteger(data);
      AnimSupplyDownEnd = GetInteger(data);
      AnimSupplyUpStart = GetInteger(data);
      AnimSupplyUpEnd = GetInteger(data);
    }

    public int AmmoCapacity { get; }

    public int AnimSupplyDownStart { get; }

    public int AnimSupplyDownEnd { get; }

    public int AnimSupplyUpStart { get; }

    public int AnimSupplyUpEnd { get; }
  }
}
