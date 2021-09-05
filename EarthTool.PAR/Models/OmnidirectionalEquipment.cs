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
    public OmnidirectionalEquipment(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      LookRoundTypeMask = BitConverter.ToInt32(data.ReadBytes(4));
      LookRoundRange = BitConverter.ToInt32(data.ReadBytes(4));
      TurnSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      BannerAddExperienceLevel = BitConverter.ToInt32(data.ReadBytes(4));
      RegenerationHPMultiple = BitConverter.ToInt32(data.ReadBytes(4));
      ShieldReloadAdd = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int LookRoundTypeMask { get; }

    public int LookRoundRange { get; }

    public int TurnSpeed { get; }

    public int BannerAddExperienceLevel { get; }

    public int RegenerationHPMultiple { get; }

    public int ShieldReloadAdd { get; }
  }
}
