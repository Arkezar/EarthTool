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
    public EquipableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      SightRange = BitConverter.ToInt32(data.ReadBytes(4));
      TalkPackId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      ShieldGeneratorId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      MaxShieldUpdate = BitConverter.ToInt32(data.ReadBytes(4));
      Slot1Type = BitConverter.ToInt32(data.ReadBytes(4));
      Slot2Type = BitConverter.ToInt32(data.ReadBytes(4));
      Slot3Type = BitConverter.ToInt32(data.ReadBytes(4));
      Slot4Type = BitConverter.ToInt32(data.ReadBytes(4));
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
