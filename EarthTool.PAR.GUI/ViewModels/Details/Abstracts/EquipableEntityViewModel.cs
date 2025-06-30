using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class EquipableEntityViewModel : DestructibleEntityViewModel
{
  private int _sightRange;
  private string _talkPackId;
  private string _shieldGeneratorId;
  private int _maxShieldUpdate;
  private SlotType _slot1Type;
  private SlotType _slot2Type;
  private SlotType _slot3Type;
  private SlotType _slot4Type;

  protected EquipableEntityViewModel(EquipableEntity entity)
    : base(entity)
  {
    _sightRange = entity.SightRange;
    _talkPackId = entity.TalkPackId;
    _shieldGeneratorId = entity.ShieldGeneratorId;
    _maxShieldUpdate = entity.MaxShieldUpdate;
    _slot1Type = entity.Slot1Type;
    _slot2Type = entity.Slot2Type;
    _slot3Type = entity.Slot3Type;
    _slot4Type = entity.Slot4Type;
  }

  public int SightRange
  {
    get => _sightRange;
    set => this.RaiseAndSetIfChanged(ref _sightRange, value);
  }

  public string TalkPackId
  {
    get => _talkPackId;
    set => this.RaiseAndSetIfChanged(ref _talkPackId, value);
  }

  public string ShieldGeneratorId
  {
    get => _shieldGeneratorId;
    set => this.RaiseAndSetIfChanged(ref _shieldGeneratorId, value);
  }

  public int MaxShieldUpdate
  {
    get => _maxShieldUpdate;
    set => this.RaiseAndSetIfChanged(ref _maxShieldUpdate, value);
  }

  public SlotType Slot1Type
  {
    get => _slot1Type;
    set => this.RaiseAndSetIfChanged(ref _slot1Type, value);
  }

  public SlotType Slot2Type
  {
    get => _slot2Type;
    set => this.RaiseAndSetIfChanged(ref _slot2Type, value);
  }

  public SlotType Slot3Type
  {
    get => _slot3Type;
    set => this.RaiseAndSetIfChanged(ref _slot3Type, value);
  }

  public SlotType Slot4Type
  {
    get => _slot4Type;
    set => this.RaiseAndSetIfChanged(ref _slot4Type, value);
  }
}