using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class ShieldGeneratorViewModel : EntityViewModel
{
  private int    _shieldCost;
  private int    _shieldValue;
  private int    _reloadTime;
  private string _shieldMeshName;
  private int    _shieldMeshViewIndex;

  public ShieldGeneratorViewModel(ShieldGenerator entry)
    : base(entry)
  {
    _shieldCost = entry.ShieldCost;
    _shieldValue = entry.ShieldValue;
    _reloadTime = entry.ReloadTime;
    _shieldMeshName = entry.ShieldMeshName;
    _shieldMeshViewIndex = entry.ShieldMeshViewIndex;
  }

  public int ShieldCost
  {
    get => _shieldCost;
    set => this.RaiseAndSetIfChanged(ref _shieldCost, value);
  }

  public int ShieldValue
  {
    get => _shieldValue;
    set => this.RaiseAndSetIfChanged(ref _shieldValue, value);
  }

  public int ReloadTime
  {
    get => _reloadTime;
    set => this.RaiseAndSetIfChanged(ref _reloadTime, value);
  }

  public string ShieldMeshName
  {
    get => _shieldMeshName;
    set => this.RaiseAndSetIfChanged(ref _shieldMeshName, value);
  }

  public int ShieldMeshViewIndex
  {
    get => _shieldMeshViewIndex;
    set => this.RaiseAndSetIfChanged(ref _shieldMeshViewIndex, value);
  }
}