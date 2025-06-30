using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class PassiveEntityViewModel : DestructibleEntityViewModel
{
  private int _passiveMask;
  private string _wallCopulaId;

  protected PassiveEntityViewModel(PassiveEntity entity)
    : base(entity)
  {
    _passiveMask = entity.PassiveMask;
    _wallCopulaId = entity.WallCopulaId;
  }

  public int PassiveMask
  {
    get => _passiveMask;
    set => this.RaiseAndSetIfChanged(ref _passiveMask, value);
  }

  public string WallCopulaId
  {
    get => _wallCopulaId;
    set => this.RaiseAndSetIfChanged(ref _wallCopulaId, value);
  }
}