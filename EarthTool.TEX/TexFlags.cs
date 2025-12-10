using System;

namespace EarthTool.TEX
{
  [Flags]
  public enum TexFlags : uint
  {
    
    TEX_FLAG_LOD  = 0x00000004,
    TEX_FLAG_STANDARD = 0x00000010, // Standard texture (#) without -sides
    TEX_FLAG_SIDES    = 0x00000020, // Multi-sided (#) with -sides
    TEX_FLAG_ANIMATED = 0x00000100, // Animated/destructible (~)
    TEX_FLAG_RGBA     = 0x00000200, // RGBA 4-channel (@4)
    TEX_FLAG_SPECIAL  = 0x00000400, // Special texture ($)

// Automatically added flags (always present)
    TEX_FLAG_RGBA32 = 0x00000002, // RGBA32 format (always set)
    TEX_FLAG_MIPMAP = 0x03000000, // Mipmap present (always set)

// Conditional flags
    TEX_FLAG_CURSOR = 0x000F0000, // Cursor definition (when defined)
    TEX_FLAG_SIDECOLOR = 0x10000000,
    TEX_FLAG_DESTROYED = 0x40000000,
    TEX_FLAG_CONTAINER = 0x80000000,
  }
}
