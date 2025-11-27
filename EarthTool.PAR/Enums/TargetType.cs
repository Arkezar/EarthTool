using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum TargetType
  {
    None   = 0x00,
    Ground = 0x01,
    Air    = 0x02,
  }
}
