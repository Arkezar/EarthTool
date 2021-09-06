﻿namespace EarthTool.Common.Enums
{
  public enum ResourceType
  {
    None,
    Interface         = 0x01004449,
    MainInterface     = 0x7fffffff,
    Map               = 0x0400444c,
    MapAssets         = 0x0700444d,
    Parameters        = 0x00000099,
    Terrain           = 0x05004454,
    Asset             = 0x00000001,

    TankScript        = 0x00000002,
    SapperScript      = 0x00000003,
    HarvesterScript   = 0x00000004,
    CarrierScript     = 0x00000005,
    RepairerScript    = 0x00000006,
    SupplierScript    = 0x00000007,
    BuilderScript     = 0x00000008,
    AircraftScript    = 0x00000009,
    CivilScript       = 0x0000000a,
    PlatoonScript     = 0x0000000b,
    CampaignScript    = 0x0000000c,
    PlayerScript      = 0x0000000d,
    MissionScript     = 0x0000000e,
    TransporterScript = 0x00000010,
  }
}
