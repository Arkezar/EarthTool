using EarthTool.PAR.GUI.ViewModels.Details;
using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Abstracts;

namespace EarthTool.PAR.GUI.Extensions;

public static class EntityExtensions
{
  public static EntityViewModel? ToViewModel(this Entity entity)
    => entity switch
    {
      OmnidirectionalEquipment omnidirectionalEquipment => new OmnidirectionalEquipmentViewModel(omnidirectionalEquipment),
      Repairer repairer => new RepairerViewModel(repairer),
      TransporterHook transporterHook => new TransporterHookViewModel(transporterHook),
      UpgradeCopula upgradeCopula => new UpgradeCopulaViewModel(upgradeCopula),
      ContainerTransporter containerTransporter => new ContainerTransporterViewModel(containerTransporter),
      Equipment equipment => new EquipmentViewModel(equipment),
      BuildingTransporter buildingTransporter => new BuildingTransporterViewModel(buildingTransporter),
      ResourceTransporter resourceTransporter => new ResourceTransporterViewModel(resourceTransporter),
      UnitTransporter unitTransporter => new UnitTransporterViewModel(unitTransporter),
      Building building => new BuildingViewModel(building),
      Platoon platoon => new PlatoonViewModel(platoon),
      StartingPosition startingPosition => new StartingPositionViewModel(startingPosition),
      SupplyTransporter supplyTransporter => new SupplyTransporterViewModel(supplyTransporter),
      Builder builder => new BuilderViewModel(builder),
      Harvester harvester => new HarvesterViewModel(harvester),
      Sapper sapper => new SapperViewModel(sapper),
      Vehicle vehicle => new VehicleViewModel(vehicle),
      Weapon weapon => new WeaponViewModel(weapon),
      MultiExplosion multiExplosion => new MultiExplosionViewModel(multiExplosion),
      Explosion explosion => new ExplosionViewModel(explosion),
      FlyingWaste flyingWaste => new FlyingWasteViewModel(flyingWaste),
      Mine mine => new MineViewModel(mine),
      Missile missile => new MissileViewModel(missile),
      Smoke smoke => new SmokeViewModel(smoke),
      WallLaser wallLaser => new WallLaserViewModel(wallLaser),
      BuilderLine builderLine => new BuilderLineViewModel(builderLine),
      ShieldGenerator shieldGenerator => new ShieldGeneratorViewModel(shieldGenerator),
      Special special => new SpecialViewModel(special),
      Passive passive => new PassiveViewModel(passive),
      Artifact artifact => new ArtifactViewModel(artifact),
      PlayerTalkPack playerTalkPack => new PlayerTalkPackViewModel(playerTalkPack),
      SoundPack soundPack => new SoundPackViewModel(soundPack),
      TalkPack talkPack => new TalkPackViewModel(talkPack),
      Parameter parameter => new ParameterViewModel(parameter),
      SpecialUpdateLink specialUpdateLink => new SpecialUpdateLinkViewModel(specialUpdateLink),
      _ => null
    };

  public static EntityGroupViewModel ToViewModel(this EntityGroup entityGroup)
    => new EntityGroupViewModel(entityGroup);
}