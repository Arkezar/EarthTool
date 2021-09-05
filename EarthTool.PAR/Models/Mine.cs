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
    public Mine(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      MineSize = BitConverter.ToInt32(data.ReadBytes(4));
      MineTypeOfDamage = BitConverter.ToInt32(data.ReadBytes(4));
      MineDamage = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int MineSize { get; }

    public int MineTypeOfDamage { get; }

    public int MineDamage { get; }
  }
}
