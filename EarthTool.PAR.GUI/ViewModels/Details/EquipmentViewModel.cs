using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class EquipmentViewModel : InteractableEntityViewModel
{
  private int _rangeOfSight;
  private SlotType _plugType;
  private SlotType _slotType;
  private int _maxAlphaPerTick;
  private int _maxBetaPerTick;

  public EquipmentViewModel(Equipment equipment)
    : base(equipment)
  {
    _rangeOfSight = equipment.RangeOfSight;
    _plugType = equipment.PlugType;
    _slotType = equipment.SlotType;
    _maxAlphaPerTick = equipment.MaxAlphaPerTick;
    _maxBetaPerTick = equipment.MaxBetaPerTick;
  }

  public int RangeOfSight
  {
    get => _rangeOfSight;
    set => this.RaiseAndSetIfChanged(ref _rangeOfSight, value);
  }

  public SlotType PlugType
  {
    get => _plugType;
    set => this.RaiseAndSetIfChanged(ref _plugType, value);
  }

  public SlotType SlotType
  {
    get => _slotType;
    set => this.RaiseAndSetIfChanged(ref _slotType, value);
  }

  public int MaxAlphaPerTick
  {
    get => _maxAlphaPerTick;
    set => this.RaiseAndSetIfChanged(ref _maxAlphaPerTick, value);
  }

  public int MaxBetaPerTick
  {
    get => _maxBetaPerTick;
    set => this.RaiseAndSetIfChanged(ref _maxBetaPerTick, value);
  }
}