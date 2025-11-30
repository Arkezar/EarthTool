using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum ConnectorType : uint
  {
    Disabled      = 0x0,
    Air           = 0x00000001,
    Big           = 0x00000002,
    Small         = 0x00000004,
    Heavy         = 0x00000008,
    SmallBuilding = 0x00000010,
    BigBuilding   = 0x00000020,
    Naval         = 0x00000040,
    SmallTurret   = 0x00000080,
    BigTurret     = 0x00000100,
    Special       = 0x00000200,
    Transporter   = 0x00000400,
    Utility       = 0x00000800,
    AntiRocket    = 0x00001000,
    Plus          = 0x00002000,
    Submarine     = 0x00004000,
    AntiAir       = 0x00008000,
    Repairer      = 0x00010000,
    AntiAirTurret = 0x00020000,
    Unknown40000  = 0x00040000, //not used
    Fabricator    = 0x00080000,
    Radar         = 0x01000000,
    Shadow        = 0x02000000,
    Artillery     = 0x04000000,
    SolarCell     = 0x10000000,
    SDI           = 0x20000000,
    Locked        = 0x40000000,
    Unique        = 0x80000000,
  }
}
