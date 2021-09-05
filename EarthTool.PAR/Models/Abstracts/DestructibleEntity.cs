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
    public DestructibleEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      HP = BitConverter.ToInt32(data.ReadBytes(4));
      HpRegeneration = BitConverter.ToInt32(data.ReadBytes(4));
      Armor = BitConverter.ToInt32(data.ReadBytes(4));
      CalorificCapacity = BitConverter.ToInt32(data.ReadBytes(4));
      DisableResist = BitConverter.ToInt32(data.ReadBytes(4));
      StoreableFlags = BitConverter.ToInt32(data.ReadBytes(4));
      StandType = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int HP { get; }

    public int HpRegeneration { get; }

    public int Armor { get; }

    public int CalorificCapacity { get; }

    public int DisableResist { get; }

    public int StoreableFlags { get; }

    public int StandType { get; }
  }
}
