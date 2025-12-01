using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class OmnidirectionalEquipment : Equipment
  {
    public OmnidirectionalEquipment()
    {
    }

    public OmnidirectionalEquipment(
      string name,
      IEnumerable<int> requiredResearch,
      EntityClassType type,
      BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      LookRoundTypeMask = (LookRoundTypeFlags)data.ReadInteger();
      LookRoundRange = data.ReadInteger();
      TurnSpeed = data.ReadInteger();
      BannerAddExperienceLevel = data.ReadInteger();
      RegenerationHPMultiple = data.ReadInteger();
      ShieldReloadAdd = data.ReadInteger();
    }

    public LookRoundTypeFlags LookRoundTypeMask { get; set; }

    public int LookRoundRange { get; set; }

    public int TurnSpeed { get; set; }

    public int BannerAddExperienceLevel { get; set; }

    public int RegenerationHPMultiple { get; set; }

    public int ShieldReloadAdd { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => LookRoundTypeMask,
          () => LookRoundRange,
          () => TurnSpeed,
          () => BannerAddExperienceLevel,
          () => RegenerationHPMultiple,
          () => ShieldReloadAdd
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)LookRoundTypeMask);
      bw.Write(LookRoundRange);
      bw.Write(TurnSpeed);
      bw.Write(BannerAddExperienceLevel);
      bw.Write(RegenerationHPMultiple);
      bw.Write(ShieldReloadAdd);

      return output.ToArray();
    }
  }
}
