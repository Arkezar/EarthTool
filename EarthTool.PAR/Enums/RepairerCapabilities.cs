using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum RepairerCapabilities
  {
    CanRepairTanks             = 0x1,
    CanRepairBuildings         = 0x2,
    CanConvertTanks            = 0x4,
    CanConvertBuildings        = 0x8,
    CanRepaint                 = 0x10,
    CanUpgrade                 = 0x20,
    CanConvertHealthyTanks     = 0x40,
    CanConvertHealthyBuildings = 0x80,
  }
}