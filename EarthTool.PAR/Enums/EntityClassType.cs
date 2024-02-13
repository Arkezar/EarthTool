namespace EarthTool.PAR.Enums
{
  public enum EntityClassType
  {
    None = 0x00000000,

    Vehicle = 0x00c00101,
    SupplyTransporter = 0x01c00101,
    Builder = 0x02c00101,
    Harvester = 0x04c00101,
    Sapper = 0x08c00101,

    Equipment = 0x00000002,
    Cannon = 0x00000102,
    Repairer = 0x00000202,
    ContainerTransporter = 0x00000402,
    OmnidirectionalEquipment = 0x00000802,
    UpgradeCopula = 0x00001002,
    TransporterHook = 0x00002002,

    Passive = 0x00000201,
    Mine = 0x00000801,

    MultiExplosion = 0x00010004,

    Building = 0x00010101,
    BuildPassive = 0x00010201,
    Missile = 0x00010401,

    Platoon = 0x00020101,
    TransientPassive = 0x00020201,

    Artefact = 0x01020201,

    Explosion = 0x00020401,
    ExplosionEx = 0x01020401,

    FlyingWaste = 0x00040401,
    StartingPositionMark = 0x00080101,
    Smoke = 0x00080401,

    WallLaser = 0x01100401,
    BuilderLine = 0x02100401,

    BuildingTransporter = 0x01040101,
    ResourceTransporter = 0x02040101,
    UnitTransporter = 0x04040101
  }
}