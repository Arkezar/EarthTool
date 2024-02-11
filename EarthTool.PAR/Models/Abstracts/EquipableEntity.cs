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
    public EquipableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(SightRange);
          bw.Write(TalkPackId.Length);
          bw.Write(encoding.GetBytes(TalkPackId));
          bw.Write(-1);
          bw.Write(ShieldGeneratorId.Length);
          bw.Write(encoding.GetBytes(ShieldGeneratorId));
          bw.Write(-1);
          bw.Write(MaxShieldUpdate);
          bw.Write(Slot1Type);
          bw.Write(Slot2Type);
          bw.Write(Slot3Type);
          bw.Write(Slot4Type);
        }
        return output.ToArray();
      }
    }
  }
}
