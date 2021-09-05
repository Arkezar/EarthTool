using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Equipment : InteractableEntity
  {
    public Equipment(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      RangeOfSight = BitConverter.ToInt32(data.ReadBytes(4));
      PlugType = BitConverter.ToInt32(data.ReadBytes(4));
      SlotType = BitConverter.ToInt32(data.ReadBytes(4));
      MaxAlphaPerTick = BitConverter.ToInt32(data.ReadBytes(4));
      MaxBetaPerTick = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int RangeOfSight { get; }

    public int PlugType { get; }

    public int SlotType { get; }

    public int MaxAlphaPerTick { get; }

    public int MaxBetaPerTick { get; }
  }
}
