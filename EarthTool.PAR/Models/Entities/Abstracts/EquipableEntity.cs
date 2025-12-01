using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
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
      SightRange = data.ReadInteger();
      TalkPackId = data.ReadParameterStringRef();
      ShieldGeneratorId = data.ReadParameterStringRef();
      MaxShieldUpgrade = (MaxShieldUpgradeType)data.ReadInteger();
      Slot1Type = (ConnectorType)data.ReadUnsignedInteger();
      Slot2Type = (ConnectorType)data.ReadUnsignedInteger();
      Slot3Type = (ConnectorType)data.ReadUnsignedInteger();
      Slot4Type = (ConnectorType)data.ReadUnsignedInteger();
    }

    public int SightRange { get; set; }


    public string TalkPackId { get; set; }

    public string ShieldGeneratorId { get; set; }

    public MaxShieldUpgradeType MaxShieldUpgrade { get; set; }

    public ConnectorType Slot1Type { get; set; }

    public ConnectorType Slot2Type { get; set; }

    public ConnectorType Slot3Type { get; set; }

    public ConnectorType Slot4Type { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => SightRange,
          () => TalkPackId,
          () => ReferenceMarker,
          () => ShieldGeneratorId,
          () => ReferenceMarker,
          () => MaxShieldUpgrade,
          () => Slot1Type,
          () => Slot2Type,
          () => Slot3Type,
          () => Slot4Type
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();
      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(SightRange);
      bw.WriteParameterStringRef(TalkPackId, encoding);
      bw.WriteParameterStringRef(ShieldGeneratorId, encoding);
      bw.Write((uint)MaxShieldUpgrade);
      bw.Write((uint)Slot1Type);
      bw.Write((uint)Slot2Type);
      bw.Write((uint)Slot3Type);
      bw.Write((uint)Slot4Type);
      return output.ToArray();
    }
  }
}
