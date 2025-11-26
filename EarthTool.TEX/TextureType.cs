using System;

namespace EarthTool.TEX
{
  [Flags]
  public enum TextureType
  {
    Special = 0,
    Unknown1 = 1,
    Texture = 2,
    Lod = 4,
    Unknown8 = 8,
    Unknown16 = 16,
    Transparent = 32
  }
}
