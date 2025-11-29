using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum CopulaAnimationFlags
  {
    None       = 0x00,
    Animated   = 0x01,
    EdBuild    = 0x02,
    CloseNorth = 0x04,
    CloseEast  = 0x08,
    CloseSouth = 0x10,
    CloseWest  = 0x20,
  }
}

