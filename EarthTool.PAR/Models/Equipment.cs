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
    public Equipment(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      RangeOfSight = GetInteger(data);
      PlugType = GetInteger(data);
      SlotType = GetInteger(data);
      MaxAlphaPerTick = GetInteger(data);
      MaxBetaPerTick = GetInteger(data);
    }

    public int RangeOfSight { get; }

    public int PlugType { get; }

    public int SlotType { get; }

    public int MaxAlphaPerTick { get; }

    public int MaxBetaPerTick { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(RangeOfSight);
          bw.Write(PlugType);
          bw.Write(SlotType);
          bw.Write(MaxAlphaPerTick);
          bw.Write(MaxBetaPerTick);
        }
        return output.ToArray();
      }
    }
  }
}
