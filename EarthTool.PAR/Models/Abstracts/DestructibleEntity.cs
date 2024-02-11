using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public abstract class DestructibleEntity : InteractableEntity
  {
    public DestructibleEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      HP = GetInteger(data);
      HpRegeneration = GetInteger(data);
      Armor = GetInteger(data);
      CalorificCapacity = GetInteger(data);
      DisableResist = GetInteger(data);
      StoreableFlags = GetInteger(data);
      StandType = GetInteger(data);
    }

    public int HP { get; }

    public int HpRegeneration { get; }

    public int Armor { get; }

    public int CalorificCapacity { get; }

    public int DisableResist { get; }

    public int StoreableFlags { get; }

    public int StandType { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(HP);
          bw.Write(HpRegeneration);
          bw.Write(Armor);
          bw.Write(CalorificCapacity);
          bw.Write(DisableResist);
          bw.Write(StoreableFlags);
          bw.Write(StandType);
        }
        return output.ToArray();
      }
    }
  }
}
