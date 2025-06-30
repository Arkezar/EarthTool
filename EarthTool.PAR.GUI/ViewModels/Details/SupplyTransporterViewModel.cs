using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class SupplyTransporterViewModel : VehicleViewModel
{
  private int _ammoCapacity;
  private int _animSupplyDownStart;
  private int _animSupplyDownEnd;
  private int _animSupplyUpStart;
  private int _animSupplyUpEnd;

  public SupplyTransporterViewModel(SupplyTransporter supplyTransporter)
    : base(supplyTransporter)
  {
    _ammoCapacity = supplyTransporter.AmmoCapacity;
    _animSupplyDownStart = supplyTransporter.AnimSupplyDownStart;
    _animSupplyDownEnd = supplyTransporter.AnimSupplyDownEnd;
    _animSupplyUpStart = supplyTransporter.AnimSupplyUpStart;
    _animSupplyUpEnd = supplyTransporter.AnimSupplyUpEnd;
  }

  public int AmmoCapacity
  {
    get => _ammoCapacity;
    set => this.RaiseAndSetIfChanged(ref _ammoCapacity, value);
  }

  public int AnimSupplyDownStart
  {
    get => _animSupplyDownStart;
    set => this.RaiseAndSetIfChanged(ref _animSupplyDownStart, value);
  }

  public int AnimSupplyDownEnd
  {
    get => _animSupplyDownEnd;
    set => this.RaiseAndSetIfChanged(ref _animSupplyDownEnd, value);
  }

  public int AnimSupplyUpStart
  {
    get => _animSupplyUpStart;
    set => this.RaiseAndSetIfChanged(ref _animSupplyUpStart, value);
  }

  public int AnimSupplyUpEnd
  {
    get => _animSupplyUpEnd;
    set => this.RaiseAndSetIfChanged(ref _animSupplyUpEnd, value);
  }
}