using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public abstract class EquipableEntity : DestructibleEntity
  {
    public EquipableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      SightRange = GetInteger(data);
      TalkPackId = GetString(data);
      data.ReadBytes(4);
      ShieldGeneratorId = GetString(data);
      data.ReadBytes(4);
      MaxShieldUpdate = GetInteger(data);
      Slot1Type = GetInteger(data);
      Slot2Type = GetInteger(data);
      Slot3Type = GetInteger(data);
      Slot4Type = GetInteger(data);
    }

    public int SightRange { get; }

    public string TalkPackId { get; }

    public string ShieldGeneratorId { get; }

    public int MaxShieldUpdate { get; }

    public int Slot1Type { get; }

    public int Slot2Type { get; }

    public int Slot3Type { get; }

    public int Slot4Type { get; }
  }
}
