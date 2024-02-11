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
    public OmnidirectionalEquipment(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
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
  }
}
