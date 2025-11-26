using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum SlotType : uint
  {
    Disabled        = 0x0,
    Slot            = 0x1,
    BigSlot         = 0x2,
    AirSlot         = 0x4,
    Unknown8        = 0x8,
    BannerSlot      = 0x10,
    BuildingSlot    = 0x20,
    ShipSlot        = 0x40,
    MechSlot        = 0x80,
    BigMechSlot     = 0x100,
    SpecialSlot     = 0x200,
    SpecialAirSlot  = 0x400,
    SomeSmallSlot   = 0x800,
    AntirocketSlot  = 0x1000,
    PlusAirSlot     = 0x2000,
    SubmarineSlot   = 0x4000,
    Unknown8000     = 0x8000,
    RepairerSlot    = 0x10000,
    Unknown20000    = 0x20000,
    ExtraSlot       = 0x40000,
    Unknown80000    = 0x80000,
    Unknown100000   = 0x100000,
    Unknown200000   = 0x200000,
    Unknown400000   = 0x400000,
    Unknown800000   = 0x800000,
    RadarSlot       = 0x1000000,
    ShadowSlot      = 0x2000000,
    ArtillerySlot   = 0x4000000,
    VerySpecialSlot = 0x80000000,
  }
}