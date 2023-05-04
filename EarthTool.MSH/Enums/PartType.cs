﻿using System;

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
    Missile1 = 0x10,
    Missile2 = 0x20,
    Missile3 = 0x40,
    Missile4 = 0x80
  }
}