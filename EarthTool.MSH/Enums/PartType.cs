using System;

namespace EarthTool.MSH.Enums
{
  [Flags]
  public enum PartType
  {
    Base = 0x0,
    ViewerFaced = 0x1,
    Barrel = 0x2,
    Rotor = 0x4,
    Subpart = 0x8,
    Emitter1 = 0x10,
    Emitter2 = 0x20,
    Emitter3 = 0x40,
    Emitter4 = 0x80
  }
}
