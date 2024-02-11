using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class EntityFactory
  {
    public Entity CreateEntity(BinaryReader data, EntityGroupType groupType)
    {
      var name = new string(data.ReadChars(data.ReadInt32()));
      var requiredResearch = Enumerable.Range(0, data.ReadInt32()).Select(i => data.ReadInt32()).ToList();
      var fieldTypes = Enumerable.Range(0, data.ReadInt32()).Select(i => data.ReadBoolean()).ToList();

      var type = groupType switch
      {
        EntityGroupType.SoundPack => EntityClassType.None,
        EntityGroupType.ShieldGenerator => EntityClassType.None,
        EntityGroupType.Parameter => EntityClassType.None,
        EntityGroupType.SpecialUpdatesLink => EntityClassType.None,
        _ => (EntityClassType)data.ReadInt32()
      };
        
      return BuildEntity(groupType, name, requiredResearch, type, data, fieldTypes);
    }

    private Entity BuildEntity(EntityGroupType groupType, string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes)
      => (groupType, type) switch
      {
        (EntityGroupType.Building, EntityClassType.Building) => new Building(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Cannon, EntityClassType.Cannon) => new Weapon(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Equipment, EntityClassType.Equipment) => new Equipment(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Equipment, EntityClassType.ContainerTransporter) => new ContainerTransporter(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Equipment, EntityClassType.OmnidirectionalEquipment) => new OmnidirectionalEquipment(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Equipment, EntityClassType.Repairer) => new Repairer(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Equipment, EntityClassType.TransporterHook) => new TransporterHook(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Missile, EntityClassType.Missile) => new Missile(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.UnitTransporter) => new UnitTransporter(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.ResourceTransporter) => new ResourceTransporter(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.BuildingTransporter) => new BuildingTransporter(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.UpgradeCopula) => new UpgradeCopula(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.Passive) => new Passive(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.TransientPassive) => new TransientPassive(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.BuildPassive) => new BuildPassive(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.Artefact) => new Artifact(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.StartingPositionMark) => new StartingPosition(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.MultiExplosion) => new MultiExplosion(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.Explosion) => new Explosion(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.ExplosionEx) => new Explosion(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.Smoke) => new Smoke(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.FlyingWaste) => new FlyingWaste(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.Mine) => new Mine(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.WallLaser) => new WallLaser(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.BuilderLine) => new BuilderLine(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Special, EntityClassType.Platoon) => new Platoon(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Vehicle, EntityClassType.Builder) => new Builder(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Vehicle, EntityClassType.Vehicle) => new Vehicle(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Vehicle, EntityClassType.Harvester) => new Harvester(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Vehicle, EntityClassType.Sapper) => new Sapper(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Vehicle, EntityClassType.SupplyTransporter) => new SupplyTransporter(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.SoundPack, _) when name.StartsWith("TALK_") => new TalkPack(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.SoundPack, _) when name.StartsWith("PLAYERTALK_") => new PlayerTalkPack(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.SoundPack, _) => new SoundPack(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.ShieldGenerator, _) => new ShieldGenerator(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.Parameter, _) => new Parameter(name, requiredResearch, type, data, fieldTypes),
        (EntityGroupType.SpecialUpdatesLink, _) => new SpecialUpdateLink(name, requiredResearch, type, data, fieldTypes),
        _ => throw new Exception("Unhandled entity type. Exiting...")
      };

  }
}
