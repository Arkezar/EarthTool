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
    public SupplyTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      AmmoCapacity = BitConverter.ToInt32(data.ReadBytes(4));
      AnimSupplyDownStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimSupplyDownEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimSupplyUpStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimSupplyUpEnd = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int AmmoCapacity { get; }

    public int AnimSupplyDownStart { get; }

    public int AnimSupplyDownEnd { get; }

    public int AnimSupplyUpStart { get; }

    public int AnimSupplyUpEnd { get; }
  }
}
