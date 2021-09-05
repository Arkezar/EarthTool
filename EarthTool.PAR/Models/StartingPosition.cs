using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class StartingPosition : EquipableEntity
  {
    public StartingPosition(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      PositionType = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int PositionType { get; }
  }
}
