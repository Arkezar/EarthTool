using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class ShieldGenerator : Entity
  {
    public ShieldGenerator(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type)
    {
      ShieldCost = BitConverter.ToInt32(data.ReadBytes(4));
      ShieldValue = BitConverter.ToInt32(data.ReadBytes(4));
      ReloadTime = BitConverter.ToInt32(data.ReadBytes(4));
      ShieldMeshName = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ShieldMeshViewIndex = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int ShieldCost { get; }

    public int ShieldValue { get; }

    public int ReloadTime { get; }

    public string ShieldMeshName { get; }

    public int ShieldMeshViewIndex { get; }
  }
}
