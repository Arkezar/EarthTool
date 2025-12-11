using System;

namespace EarthTool.TEX
{
  [Flags]
  public enum TexFlags : uint
  {
    None           = 0x00000000, // Grid 1x1 (unused)
    Rgba32         = 0x00000002, // RGBA32 format (always set)
    Lod            = 0x00000004, // -mipmap1|2|3|4|...
    Standard       = 0x00000010, // Simple texture? (#*.bmp)
    TgaSpriteSheet = 0x00000020, // Sprite sheet (#*.tga)
    Animated       = 0x00000100, // Animated? (~)
    PreciseAlpha   = 0x00000200, // RGBA 4-channel (@4)
    Special        = 0x00000400, // Special texture ($)
    Cursor         = 0x000F0000, // Cursor definition (when defined)
    Mipmap         = 0x03000000, // Mipmap present (always set)
    SideColors     = 0x10000000, // Glow? -sides
    DamageStates   = 0x40000000, // Unknown source command
    Container      = 0x80000000, // Grid > 1x1
  }
}
