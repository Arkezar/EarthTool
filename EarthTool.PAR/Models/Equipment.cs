using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Equipment : InteractableEntity
  {
    public Equipment()
    {
    }

    public Equipment(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      RangeOfSight = GetInteger(data);
      PlugType = (ConnectorType)GetUnsignedInteger(data);
      SlotType = (ConnectorType)GetUnsignedInteger(data);
      MaxAlphaPerTick = GetInteger(data);
      MaxBetaPerTick = GetInteger(data);
    }

    public int RangeOfSight { get; set; }

    public ConnectorType PlugType { get; set; }

    public ConnectorType SlotType { get; set; }

    public int MaxAlphaPerTick { get; set; }

    public int MaxBetaPerTick { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => RangeOfSight,
          () => PlugType,
          () => SlotType,
          () => MaxAlphaPerTick,
          () => MaxBetaPerTick
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
          bw.Write(RangeOfSight);
          bw.Write((uint)PlugType);
          bw.Write((uint)SlotType);
          bw.Write(MaxAlphaPerTick);
          bw.Write(MaxBetaPerTick);
        }

        return output.ToArray();
      }
    }
  }
}
