using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class InteractableEntityViewModel : EntityViewModel
{
  private string _mesh;
  private int    _shadowType;
  private int    _viewParamsIndex;
  private int    _cost;
  private int    _timeOfBuild;
  private string _soundPackId;
  private string _smokeId;
  private string _killExplosionId;
  private string _destructedId;

  public InteractableEntityViewModel(
    InteractableEntity entity)
    : base(entity)
  {
    _mesh = entity.Mesh;
    _shadowType = entity.ShadowType;
    _viewParamsIndex = entity.ViewParamsIndex;
    _cost = entity.Cost;
    _timeOfBuild = entity.TimeOfBuild;
    _soundPackId = entity.SoundPackId;
    _smokeId = entity.SmokeId;
    _killExplosionId = entity.KillExplosionId;
    _destructedId = entity.DestructedId;
  }

  public string Mesh
  {
    get => _mesh;
    set => this.RaiseAndSetIfChanged(ref _mesh, value);
  }

  public int ShadowType
  {
    get => _shadowType;
    set => this.RaiseAndSetIfChanged(ref _shadowType, value);
  }

  public int ViewParamsIndex
  {
    get => _viewParamsIndex;
    set => this.RaiseAndSetIfChanged(ref _viewParamsIndex, value);
  }

  public int Cost
  {
    get => _cost;
    set => this.RaiseAndSetIfChanged(ref _cost, value);
  }

  public int TimeOfBuild
  {
    get => _timeOfBuild;
    set => this.RaiseAndSetIfChanged(ref _timeOfBuild, value);
  }

  public string SoundPackId
  {
    get => _soundPackId;
    set => this.RaiseAndSetIfChanged(ref _soundPackId, value);
  }

  public string SmokeId
  {
    get => _smokeId;
    set => this.RaiseAndSetIfChanged(ref _smokeId, value);
  }

  public string KillExplosionId
  {
    get => _killExplosionId;
    set => this.RaiseAndSetIfChanged(ref _killExplosionId, value);
  }

  public string DestructedId
  {
    get => _destructedId;
    set => this.RaiseAndSetIfChanged(ref _destructedId, value);
  }
}