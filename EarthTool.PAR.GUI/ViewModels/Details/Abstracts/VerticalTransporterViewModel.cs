using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class VerticalTransporterViewModel : EquipableEntityViewModel
{
  private int _vehicleSpeed;
  private int _verticalVehicleAnimationType;

  protected VerticalTransporterViewModel(VerticalTransporter entity)
    : base(entity)
  {
    _vehicleSpeed = entity.VehicleSpeed;
    _verticalVehicleAnimationType = entity.VerticalVehicleAnimationType;
  }

  public int VehicleSpeed
  {
    get => _vehicleSpeed;
    set => this.RaiseAndSetIfChanged(ref _vehicleSpeed, value);
  }

  public int VerticalVehicleAnimationType
  {
    get => _verticalVehicleAnimationType;
    set => this.RaiseAndSetIfChanged(ref _verticalVehicleAnimationType, value);
  }
}