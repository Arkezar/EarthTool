namespace EarthTool.Common.Enums
{
  public enum ResourceType
  {
    None,
    Interface     = 0x49,
    MainInterface = 0xff,
    Map           = 0x4c,
    MapAssets     = 0x4d,
    Parameters    = 0x99,
    Terrain       = 0x54,
    Asset         = 0x01,

    TankScript        = 0x02,
    SapperScript      = 0x03,
    HarvesterScript   = 0x04,
    CarrierScript     = 0x05,
    RepairerScript    = 0x06,
    SupplierScript    = 0x07,
    BuilderScript     = 0x08,
    AircraftScript    = 0x09,
    CivilScript       = 0x0a,
    PlatoonScript     = 0x0b,
    CampaignScript    = 0x0c,
    PlayerScript      = 0x0d,
    MissionScript     = 0x0e,
    TransporterScript = 0x10,

    WdArchive = 0x2004457
  }
}