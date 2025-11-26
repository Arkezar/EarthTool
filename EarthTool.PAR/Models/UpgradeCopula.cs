using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.PAR.Models
{
  public class UpgradeCopula : Equipment
  {
    public UpgradeCopula()
    {
    }

    public UpgradeCopula(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
    }
  }
}
