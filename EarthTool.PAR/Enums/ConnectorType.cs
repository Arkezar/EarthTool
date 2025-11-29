using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum ConnectorType : uint
  {
    Disabled         = 0x0,
    Slot             = 0x00000001,
    BigSlot          = 0x00000002,
    AirSlot          = 0x00000004,
    Unknown8         = 0x00000008,
    BannerSlot       = 0x00000010,
    BuildingSlot     = 0x00000020,
    ShipSlot         = 0x00000040,
    MechSlot         = 0x00000080,
    BigMechSlot      = 0x00000100,
    SpecialSlot      = 0x00000200,
    SpecialAirSlot   = 0x00000400,
    SomeSmallSlot    = 0x00000800,
    AntirocketSlot   = 0x00001000,
    PlusAirSlot      = 0x00002000,
    SubmarineSlot    = 0x00004000,
    Unknown8000      = 0x00008000,
    RepairerSlot     = 0x00010000,
    Unknown20000     = 0x00020000,
    ExtraSlot        = 0x00040000,
    FabSlot          = 0x00080000,
    Unknown100000    = 0x00100000,
    Unknown200000    = 0x00200000,
    Unknown400000    = 0x00400000,
    Unknown800000    = 0x00800000,
    RadarSlot        = 0x01000000,
    ShadowSlot       = 0x02000000,
    ArtillerySlot    = 0x04000000,
    SolarBatterySlot = 0x10000000,
    SDISlot          = 0x20000000,
    LockedSlot       = 0x40000000,
    UniqueSlot       = 0x80000000,
  }
}
