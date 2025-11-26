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

    public EquipableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) :
      base(name, requiredResearch, type, data)
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

    public int SightRange { get; set; }

    public string TalkPackId { get; set; }

    public string ShieldGeneratorId { get; set; }

    public int MaxShieldUpdate { get; set; }

    public int Slot1Type { get; set; }

    public int Slot2Type { get; set; }

    public int Slot3Type { get; set; }

    public int Slot4Type { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
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
