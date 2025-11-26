using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class EquipableEntity : DestructibleEntity
  {
    public EquipableEntity()
    {
    }

    public EquipableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      :
      base(name, requiredResearch, type, data)
    {
      SightRange = GetInteger(data);
      TalkPackId = GetString(data);
      data.ReadBytes(4);
      ShieldGeneratorId = GetString(data);
      data.ReadBytes(4);
      MaxShieldUpdate = GetInteger(data);
      Slot1Type = (SlotType)GetUnsignedInteger(data);
      Slot2Type = (SlotType)GetUnsignedInteger(data);
      Slot3Type = (SlotType)GetUnsignedInteger(data);
      Slot4Type = (SlotType)GetUnsignedInteger(data);
    }

    public int SightRange { get; set; }
    

    public string TalkPackId { get; set; }

    public string ShieldGeneratorId { get; set; }

    public int MaxShieldUpdate { get; set; }

    public SlotType Slot1Type { get; set; }

    public SlotType Slot2Type { get; set; }

    public SlotType Slot3Type { get; set; }

    public SlotType Slot4Type { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => SightRange,
          () => TalkPackId,
          () => 1,
          () => ShieldGeneratorId,
          () => 1,
          () => MaxShieldUpdate,
          () => Slot1Type,
          () => Slot2Type,
          () => Slot3Type,
          () => Slot4Type
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
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
          bw.Write((uint)Slot1Type);
          bw.Write((uint)Slot2Type);
          bw.Write((uint)Slot3Type);
          bw.Write((uint)Slot4Type);
        }

        return output.ToArray();
      }
    }
  }
}
