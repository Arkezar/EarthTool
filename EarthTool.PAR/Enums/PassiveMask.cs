using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum PassiveMask
  {
    None        = 0x0000,
    Unknown2    = 0x0002,
    Unknown4    = 0x0004,
    Unknown8    = 0x0008,
    Unknown10   = 0x0010,
    Unknown20   = 0x0020,
    Unknown40   = 0x0040,
    Unknown80   = 0x0080,
    Unknown100  = 0x0100,
    Unknown400  = 0x0400,
    Unknown8000 = 0x8000,
  }
}
