using System;

namespace EarthTool.TEX
{
  [Flags]
  public enum TextureSubType
  {
    None = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    Unknown4 = 4,
    Unknown8 = 8,
    Sides = 16,
    Unknown32 = 32,
    Collection = 64,
    Grouped = 128
  }
}
