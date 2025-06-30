using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class DestructibleEntityViewModel : InteractableEntityViewModel
{
  private int _hp;
  private int _hpRegeneration;
  private int _armor;
  private int _calorificCapacity;
  private int _disableResist;
  private int _storeableFlags;
  private int _standType;

  protected DestructibleEntityViewModel(DestructibleEntity entity)
    : base(entity)
  {
    _hp = entity.HP;
    _hpRegeneration = entity.HpRegeneration;
    _armor = entity.Armor;
    _calorificCapacity = entity.CalorificCapacity;
    _disableResist = entity.DisableResist;
    _storeableFlags = entity.StoreableFlags;
    _standType = entity.StandType;
  }

  public int HP
  {
    get => _hp;
    set => this.RaiseAndSetIfChanged(ref _hp, value);
  }

  public int HpRegeneration
  {
    get => _hpRegeneration;
    set => this.RaiseAndSetIfChanged(ref _hpRegeneration, value);
  }

  public int Armor
  {
    get => _armor;
    set => this.RaiseAndSetIfChanged(ref _armor, value);
  }

  public int CalorificCapacity
  {
    get => _calorificCapacity;
    set => this.RaiseAndSetIfChanged(ref _calorificCapacity, value);
  }

  public int DisableResist
  {
    get => _disableResist;
    set => this.RaiseAndSetIfChanged(ref _disableResist, value);
  }

  public int StoreableFlags
  {
    get => _storeableFlags;
    set => this.RaiseAndSetIfChanged(ref _storeableFlags, value);
  }

  public int StandType
  {
    get => _standType;
    set => this.RaiseAndSetIfChanged(ref _standType, value);
  }
}