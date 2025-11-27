using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum VehicleObjectType
  {
    None        = 0x0000,
    Unknown2    = 0x0002,
    Unknown4    = 0x0004,
    Unknown8    = 0x0008,
    Unknown10   = 0x0010,
    Unknown100  = 0x0100,
    Unknown1000 = 0x1000,
  }
}
