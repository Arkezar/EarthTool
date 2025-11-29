using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum RepairerCapabilityFlags
  {
    CanRepairUnits             = 0x1,
    CanRepairBuildings         = 0x2,
    CanConvertUnits            = 0x4,
    CanConvertBuildings        = 0x8,
    CanRepaint                 = 0x10,
    CanUpgrade                 = 0x20,
    CanConvertHealthyUnits     = 0x40,
    CanConvertHealthyBuildings = 0x80,
  }
}
