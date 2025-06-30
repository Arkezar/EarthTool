using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class VehicleViewModel : EquipableEntityViewModel
{
  private int _soilSpeed;
  private int _roadSpeed;
  private int _sandSpeed;
  private int _bankSpeed;
  private int _waterSpeed;
  private int _deepWaterSpeed;
  private int _airSpeed;
  private int _objectType;
  private string _engineSmokeId;
  private string _dustId;
  private string _billowId;
  private string _standBillowId;
  private string _trackId;

  public VehicleViewModel(Vehicle vehicle)
    : base(vehicle)
  {
    _soilSpeed = vehicle.SoilSpeed;
    _roadSpeed = vehicle.RoadSpeed;
    _sandSpeed = vehicle.SandSpeed;
    _bankSpeed = vehicle.BankSpeed;
    _waterSpeed = vehicle.WaterSpeed;
    _deepWaterSpeed = vehicle.DeepWaterSpeed;
    _airSpeed = vehicle.AirSpeed;
    _objectType = vehicle.ObjectType;
    _engineSmokeId = vehicle.EngineSmokeId;
    _dustId = vehicle.DustId;
    _billowId = vehicle.BillowId;
    _standBillowId = vehicle.StandBillowId;
    _trackId = vehicle.TrackId;
  }

  public int SoilSpeed
  {
    get => _soilSpeed;
    set => this.RaiseAndSetIfChanged(ref _soilSpeed, value);
  }

  public int RoadSpeed
  {
    get => _roadSpeed;
    set => this.RaiseAndSetIfChanged(ref _roadSpeed, value);
  }

  public int SandSpeed
  {
    get => _sandSpeed;
    set => this.RaiseAndSetIfChanged(ref _sandSpeed, value);
  }

  public int BankSpeed
  {
    get => _bankSpeed;
    set => this.RaiseAndSetIfChanged(ref _bankSpeed, value);
  }

  public int WaterSpeed
  {
    get => _waterSpeed;
    set => this.RaiseAndSetIfChanged(ref _waterSpeed, value);
  }

  public int DeepWaterSpeed
  {
    get => _deepWaterSpeed;
    set => this.RaiseAndSetIfChanged(ref _deepWaterSpeed, value);
  }

  public int AirSpeed
  {
    get => _airSpeed;
    set => this.RaiseAndSetIfChanged(ref _airSpeed, value);
  }

  public int ObjectType
  {
    get => _objectType;
    set => this.RaiseAndSetIfChanged(ref _objectType, value);
  }

  public string EngineSmokeId
  {
    get => _engineSmokeId;
    set => this.RaiseAndSetIfChanged(ref _engineSmokeId, value);
  }

  public string DustId
  {
    get => _dustId;
    set => this.RaiseAndSetIfChanged(ref _dustId, value);
  }

  public string BillowId
  {
    get => _billowId;
    set => this.RaiseAndSetIfChanged(ref _billowId, value);
  }

  public string StandBillowId
  {
    get => _standBillowId;
    set => this.RaiseAndSetIfChanged(ref _standBillowId, value);
  }

  public string TrackId
  {
    get => _trackId;
    set => this.RaiseAndSetIfChanged(ref _trackId, value);
  }
}