using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.GUI.Views.EntityTemplates;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Abstracts;
using System;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.Selectors
{
  public class EntityTemplateSelector : IDataTemplate
  {
    public Dictionary<Type, IDataTemplate> Templates { get; } = new();

    public EntityTemplateSelector()
    {
      RegisterTemplate<ResearchViewModel, ResearchTemplate>();
      RegisterTemplate<Building, BuildingTemplate>();
      RegisterTemplate<Vehicle, VehicleTemplate>();
      RegisterTemplate<Weapon, WeaponTemplate>();
      RegisterTemplate<Builder, BuilderTemplate>();
      RegisterTemplate<Harvester, HarvesterTemplate>();
      RegisterTemplate<Sapper, SapperTemplate>();
      RegisterTemplate<SupplyTransporter, SupplyTransporterTemplate>();
      RegisterTemplate<Equipment, EquipmentTemplate>();
      RegisterTemplate<Mine, MineTemplate>();
      RegisterTemplate<Missile, MissileTemplate>();
      RegisterTemplate<Explosion, ExplosionTemplate>();
      RegisterTemplate<Artifact, ArtifactTemplate>();
      RegisterTemplate<BuilderLine, BuilderLineTemplate>();
      RegisterTemplate<BuildingTransporter, BuildingTransporterTemplate>();
      RegisterTemplate<ContainerTransporter, ContainerTransporterTemplate>();
      RegisterTemplate<FlyingWaste, FlyingWasteTemplate>();
      RegisterTemplate<MultiExplosion, MultiExplosionTemplate>();
      RegisterTemplate<OmnidirectionalEquipment, OmnidirectionalEquipmentTemplate>();
      RegisterTemplate<Parameter, ParameterTemplate>();
      RegisterTemplate<Passive, PassiveTemplate>();
      RegisterTemplate<Platoon, PlatoonTemplate>();
      RegisterTemplate<PlayerTalkPack, PlayerTalkPackTemplate>();
      RegisterTemplate<Repairer, RepairerTemplate>();
      RegisterTemplate<ResourceTransporter, ResourceTransporterTemplate>();
      RegisterTemplate<ShieldGenerator, ShieldGeneratorTemplate>();
      RegisterTemplate<Smoke, SmokeTemplate>();
      RegisterTemplate<SoundPack, SoundPackTemplate>();
      RegisterTemplate<SpecialUpdateLink, SpecialUpdateLinkTemplate>();
      RegisterTemplate<StartingPosition, StartingPositionTemplate>();
      RegisterTemplate<TalkPack, TalkPackTemplate>();
      RegisterTemplate<TransporterHook, TransporterHookTemplate>();
      RegisterTemplate<UnitTransporter, UnitTransporterTemplate>();
      RegisterTemplate<UpgradeCopula, UpgradeCopulaTemplate>();
      RegisterTemplate<WallLaser, WallLaserTemplate>();
    }

    private void RegisterTemplate<TModel, TTemplate>()
      where TTemplate : Control, new()
    {
      Templates[typeof(TModel)] = new FuncDataTemplate<TModel>((_, _) => new TTemplate());
    }

    public Control Build(object data)
    {
      if (data == null) return new TextBlock { Text = "No data" };

      var dataType = data.GetType();

      if (Templates.TryGetValue(dataType, out var template))
      {
        return template.Build(data);
      }

      return new TextBlock { Text = $"No template for {dataType.Name}" };
    }

    public bool Match(object data)
    {
      return data is ParameterEntry || data is ResearchViewModel;
    }
  }
}