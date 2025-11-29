using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum VehicleObjectType
  {
    None          = 0x0000,
    Mech          = 0x0001,
    Rotor         = 0x0002,
    Engine        = 0x0004,
    Amphibia      = 0x0008,
    Naval         = 0x0010,
    Aircraft      = 0x0020,
    Transportable = 0x0100,
    Immovable     = 0x1000,
  }
}
