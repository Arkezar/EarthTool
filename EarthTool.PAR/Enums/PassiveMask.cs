using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum PassiveMask
  {
    None        = 0x0000,
    Generic     = 0x0001,
    Wall        = 0x0002,
    SingleWall  = 0x0004,
    Tree        = 0x0008,
    Rock        = 0x0010,
    Container   = 0x0020,
    RocketDummy = 0x0040,
    Ruin        = 0x0080,
    Bridge      = 0x0100,
    BridgeRuin  = 0x0200,
    Gadget      = 0x0400,
    Artefact    = 0x0800,
    Transient   = 0x8000,
  }
}
