using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class OmnidirectionalEquipment : Equipment
  {
    public OmnidirectionalEquipment(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      LookRoundTypeMask = GetInteger(data);
      LookRoundRange = GetInteger(data);
      TurnSpeed = GetInteger(data);
      BannerAddExperienceLevel = GetInteger(data);
      RegenerationHPMultiple = GetInteger(data);
      ShieldReloadAdd = GetInteger(data);
    }

    public int LookRoundTypeMask { get; }

    public int LookRoundRange { get; }

    public int TurnSpeed { get; }

    public int BannerAddExperienceLevel { get; }

    public int RegenerationHPMultiple { get; }

    public int ShieldReloadAdd { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(LookRoundTypeMask);
          bw.Write(LookRoundRange);
          bw.Write(TurnSpeed);
          bw.Write(BannerAddExperienceLevel);
          bw.Write(RegenerationHPMultiple);
          bw.Write(ShieldReloadAdd);
        }
        return output.ToArray();
      }
    }
  }
}
