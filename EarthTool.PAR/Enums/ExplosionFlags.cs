using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum ExplosionFlags
  {
    None        = 0x00,
    SmallQuake  = 0x01,
    MediumQuake = 0x02,
    // BigQuake    = 0x03, //??
    Crater      = 0x10,
    Track       = 0x20,
  }
}
