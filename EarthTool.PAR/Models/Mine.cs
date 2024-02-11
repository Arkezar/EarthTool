using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Mine : DestructibleEntity
  {
    public Mine(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      MineSize = GetInteger(data);
      MineTypeOfDamage = GetInteger(data);
      MineDamage = GetInteger(data);
    }

    public int MineSize { get; }

    public int MineTypeOfDamage { get; }

    public int MineDamage { get; }
  }
}
