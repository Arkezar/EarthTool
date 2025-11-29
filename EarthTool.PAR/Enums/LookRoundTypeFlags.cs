using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum LookRoundTypeFlags
  {
    Banner              = 0x01,
    Radar               = 0x02,
    Screamer            = 0x04,
    ScreamerAndRadar    = 0x06,
    Shadow              = 0x08,
    SpeedUpRegeneration = 0x10,
    SpeedUpShieldReload = 0x20,
  }
}
