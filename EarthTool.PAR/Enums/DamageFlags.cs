using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum DamageFlags
  {
    Normal     = 0x0,
    NoArmour   = 0x1,
    NoShield   = 0x2,
    KillShield = 0x4,
  }
}
