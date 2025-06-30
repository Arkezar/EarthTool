using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.GUI.ViewModels.Details;
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
      RegisterTemplate<BuildingViewModel, BuildingTemplate>();
      RegisterTemplate<VehicleViewModel, VehicleTemplate>();
      RegisterTemplate<WeaponViewModel, WeaponTemplate>();
      RegisterTemplate<BuilderViewModel, BuilderTemplate>();
      RegisterTemplate<HarvesterViewModel, HarvesterTemplate>();
      RegisterTemplate<SapperViewModel, SapperTemplate>();
      RegisterTemplate<SupplyTransporterViewModel, SupplyTransporterTemplate>();
      RegisterTemplate<EquipmentViewModel, EquipmentTemplate>();
      RegisterTemplate<MineViewModel, MineTemplate>();
      RegisterTemplate<MissileViewModel, MissileTemplate>();
      RegisterTemplate<ExplosionViewModel, ExplosionTemplate>();
      RegisterTemplate<ArtifactViewModel, ArtifactTemplate>();
      RegisterTemplate<BuilderLineViewModel, BuilderLineTemplate>();
      RegisterTemplate<BuildingTransporterViewModel, BuildingTransporterTemplate>();
      RegisterTemplate<ContainerTransporterViewModel, ContainerTransporterTemplate>();
      RegisterTemplate<FlyingWasteViewModel, FlyingWasteTemplate>();
      RegisterTemplate<MultiExplosionViewModel, MultiExplosionTemplate>();
      RegisterTemplate<OmnidirectionalEquipmentViewModel, OmnidirectionalEquipmentTemplate>();
      RegisterTemplate<ParameterViewModel, ParameterTemplate>();
      RegisterTemplate<PassiveViewModel, PassiveTemplate>();
      RegisterTemplate<PlatoonViewModel, PlatoonTemplate>();
      RegisterTemplate<PlayerTalkPackViewModel, PlayerTalkPackTemplate>();
      RegisterTemplate<RepairerViewModel, RepairerTemplate>();
      RegisterTemplate<ResourceTransporterViewModel, ResourceTransporterTemplate>();
      RegisterTemplate<ShieldGeneratorViewModel, ShieldGeneratorTemplate>();
      RegisterTemplate<SmokeViewModel, SmokeTemplate>();
      RegisterTemplate<SoundPackViewModel, SoundPackTemplate>();
      RegisterTemplate<SpecialUpdateLinkViewModel, SpecialUpdateLinkTemplate>();
      RegisterTemplate<StartingPositionViewModel, StartingPositionTemplate>();
      RegisterTemplate<TalkPackViewModel, TalkPackTemplate>();
      RegisterTemplate<TransporterHookViewModel, TransporterHookTemplate>();
      RegisterTemplate<UnitTransporterViewModel, UnitTransporterTemplate>();
      RegisterTemplate<UpgradeCopulaViewModel, UpgradeCopulaTemplate>();
      RegisterTemplate<WallLaserViewModel, WallLaserTemplate>();
      RegisterTemplate<SpecialViewModel, SpecialTemplate>();
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
      => data is ViewModelBase;
  }
}