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
    public ShieldGenerator(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) :
      base(name, requiredResearch, type)
    {
      ShieldCost = GetInteger(data);
      ShieldValue = GetInteger(data);
      ReloadTime = GetInteger(data);
      ShieldMeshName = GetString(data);
      ShieldMeshViewIndex = GetInteger(data);
    }

    public int ShieldCost { get; }

    public int ShieldValue { get; }

    public int ReloadTime { get; }

    public string ShieldMeshName { get; }

    public int ShieldMeshViewIndex { get; }
  }
}