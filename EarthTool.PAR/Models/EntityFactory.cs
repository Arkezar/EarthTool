using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.PAR.Models
{
  public class EntityFactory
  {
    public Entity CreateEntity(BinaryReader data, EntityGroupType groupType)
    {
      string name = new string(data.ReadChars(data.ReadInt32()));
      List<int> requiredResearch = Enumerable.Range(0, data.ReadInt32()).Select(i => data.ReadInt32()).ToList();
      List<bool> fieldTypes = Enumerable.Range(0, data.ReadInt32()).Select(i => data.ReadBoolean()).ToList();

      EntityClassType type = groupType switch
      {
        EntityGroupType.SoundPack => EntityClassType.None,
        EntityGroupType.ShieldGenerator => EntityClassType.None,
        EntityGroupType.Parameter => EntityClassType.None,
        EntityGroupType.SpecialUpdatesLink => EntityClassType.None,
        _ => (EntityClassType)data.ReadInt32()
      };

      return BuildEntity(groupType, name, requiredResearch, type, data, fieldTypes);
    }

    private Entity BuildEntity(EntityGroupType groupType, string name, IEnumerable<int> requiredResearch,
      EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes)
    {
      return (groupType, type) switch
      {
        (EntityGroupType.Building, EntityClassType.Building) => new Building(name, requiredResearch, type, data),
        (EntityGroupType.Cannon, EntityClassType.Cannon) => new Weapon(name, requiredResearch, type, data),
        (EntityGroupType.Equipment, EntityClassType.Equipment) => new Equipment(name, requiredResearch, type, data),
        (EntityGroupType.Equipment, EntityClassType.ContainerTransporter) => new ContainerTransporter(name,
          requiredResearch, type, data),
        (EntityGroupType.Equipment, EntityClassType.OmnidirectionalEquipment) => new OmnidirectionalEquipment(name,
          requiredResearch, type, data),
        (EntityGroupType.Equipment, EntityClassType.Repairer) => new Repairer(name, requiredResearch, type, data),
        (EntityGroupType.Equipment, EntityClassType.TransporterHook) => new TransporterHook(name, requiredResearch,
          type, data),
        (EntityGroupType.Missile, EntityClassType.Missile) => new Missile(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.UnitTransporter) => new UnitTransporter(name, requiredResearch, type,
          data),
        (EntityGroupType.Special, EntityClassType.ResourceTransporter) => new ResourceTransporter(name,
          requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.BuildingTransporter) => new BuildingTransporter(name,
          requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.UpgradeCopula) => new UpgradeCopula(name, requiredResearch, type,
          data),
        (EntityGroupType.Special, EntityClassType.Passive) => new Passive(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.TransientPassive) => new Passive(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.BuildPassive) => new Passive(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.Artefact) => new Artifact(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.StartingPositionMark) => new StartingPosition(name, requiredResearch,
          type, data),
        (EntityGroupType.Special, EntityClassType.MultiExplosion) => new MultiExplosion(name, requiredResearch, type,
          data),
        (EntityGroupType.Special, EntityClassType.Explosion) => new Explosion(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.ExplosionEx) => new Explosion(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.Smoke) => new Smoke(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.FlyingWaste) => new FlyingWaste(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.Mine) => new Mine(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.WallLaser) => new WallLaser(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.BuilderLine) => new BuilderLine(name, requiredResearch, type, data),
        (EntityGroupType.Special, EntityClassType.Platoon) => new Platoon(name, requiredResearch, type, data),
        (EntityGroupType.Vehicle, EntityClassType.Builder) => new Builder(name, requiredResearch, type, data),
        (EntityGroupType.Vehicle, EntityClassType.Vehicle) => new Vehicle(name, requiredResearch, type, data),
        (EntityGroupType.Vehicle, EntityClassType.Harvester) => new Harvester(name, requiredResearch, type, data),
        (EntityGroupType.Vehicle, EntityClassType.Sapper) => new Sapper(name, requiredResearch, type, data),
        (EntityGroupType.Vehicle, EntityClassType.SupplyTransporter) => new SupplyTransporter(name, requiredResearch,
          type, data),
        (EntityGroupType.SoundPack, _) when name.StartsWith("TALK_") => new TalkPack(name, requiredResearch, data),
        (EntityGroupType.SoundPack, _) when name.StartsWith("PLAYERTALK_") => new PlayerTalkPack(name, requiredResearch,
          data),
        (EntityGroupType.SoundPack, _) => new SoundPack(name, requiredResearch, data),
        (EntityGroupType.ShieldGenerator, _) => new ShieldGenerator(name, requiredResearch, data),
        (EntityGroupType.Parameter, _) => new Parameter(name, requiredResearch, data, fieldTypes),
        (EntityGroupType.SpecialUpdatesLink, _) => new SpecialUpdateLink(name, requiredResearch, data),
        _ => throw new Exception("Unhandled entity type. Exiting...")
      };
    }
  }
}
