using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class ResourceTransporterViewModel : VerticalTransporterViewModel
{
  private int _resourceVehicleType;
  private int _animatedTransporterStop;
  private int _showVideoPerTransportersCount;
  private int _totalOrbitalMoney;

  public ResourceTransporterViewModel(ResourceTransporter resourceTransporter)
    : base(resourceTransporter)
  {
    _resourceVehicleType = resourceTransporter.ResourceVehicleType;
    _animatedTransporterStop = resourceTransporter.AnimatedTransporterStop;
    _showVideoPerTransportersCount = resourceTransporter.ShowVideoPerTransportersCount;
    _totalOrbitalMoney = resourceTransporter.TotalOrbitalMoney;
  }

  public int ResourceVehicleType
  {
    get => _resourceVehicleType;
    set => this.RaiseAndSetIfChanged(ref _resourceVehicleType, value);
  }

  public int AnimatedTransporterStop
  {
    get => _animatedTransporterStop;
    set => this.RaiseAndSetIfChanged(ref _animatedTransporterStop, value);
  }

  public int ShowVideoPerTransportersCount
  {
    get => _showVideoPerTransportersCount;
    set => this.RaiseAndSetIfChanged(ref _showVideoPerTransportersCount, value);
  }

  public int TotalOrbitalMoney
  {
    get => _totalOrbitalMoney;
    set => this.RaiseAndSetIfChanged(ref _totalOrbitalMoney, value);
  }
}