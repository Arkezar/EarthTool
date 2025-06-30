using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class MineViewModel : DestructibleEntityViewModel
{
  private int _mineSize;
  private int _mineTypeOfDamage;
  private int _mineDamage;

  public MineViewModel(Mine mine)
    : base(mine)
  {
    _mineSize = mine.MineSize;
    _mineTypeOfDamage = mine.MineTypeOfDamage;
    _mineDamage = mine.MineDamage;
  }

  public int MineSize
  {
    get => _mineSize;
    set => this.RaiseAndSetIfChanged(ref _mineSize, value);
  }

  public int MineTypeOfDamage
  {
    get => _mineTypeOfDamage;
    set => this.RaiseAndSetIfChanged(ref _mineTypeOfDamage, value);
  }

  public int MineDamage
  {
    get => _mineDamage;
    set => this.RaiseAndSetIfChanged(ref _mineDamage, value);
  }
}