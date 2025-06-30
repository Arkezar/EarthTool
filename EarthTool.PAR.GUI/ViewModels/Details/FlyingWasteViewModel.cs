using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class FlyingWasteViewModel : DestructibleEntityViewModel
{
  private int _wasteSize;
  private string _subWasteId1;
  private int _subWaste1Alpha;
  private string _subWasteId2;
  private int _subWaste2Alpha;
  private string _subWasteId3;
  private int _subWaste3Alpha;
  private string _subWasteId4;
  private int _subWaste4Alpha;
  private int _flightTime;
  private int _wasteSpeed;
  private int _wasteDistanceX4;
  private int _wasteBeta;

  public FlyingWasteViewModel(FlyingWaste flyingWaste)
    : base(flyingWaste)
  {
    _wasteSize = flyingWaste.WasteSize;
    _subWasteId1 = flyingWaste.SubWasteId1;
    _subWaste1Alpha = flyingWaste.SubWaste1Alpha;
    _subWasteId2 = flyingWaste.SubWasteId2;
    _subWaste2Alpha = flyingWaste.SubWaste2Alpha;
    _subWasteId3 = flyingWaste.SubWasteId3;
    _subWaste3Alpha = flyingWaste.SubWaste3Alpha;
    _subWasteId4 = flyingWaste.SubWasteId4;
    _subWaste4Alpha = flyingWaste.SubWaste4Alpha;
    _flightTime = flyingWaste.FlightTime;
    _wasteSpeed = flyingWaste.WasteSpeed;
    _wasteDistanceX4 = flyingWaste.WasteDistanceX4;
    _wasteBeta = flyingWaste.WasteBeta;
  }

  public int WasteSize
  {
    get => _wasteSize;
    set => this.RaiseAndSetIfChanged(ref _wasteSize, value);
  }

  public string SubWasteId1
  {
    get => _subWasteId1;
    set => this.RaiseAndSetIfChanged(ref _subWasteId1, value);
  }

  public int SubWaste1Alpha
  {
    get => _subWaste1Alpha;
    set => this.RaiseAndSetIfChanged(ref _subWaste1Alpha, value);
  }

  public string SubWasteId2
  {
    get => _subWasteId2;
    set => this.RaiseAndSetIfChanged(ref _subWasteId2, value);
  }

  public int SubWaste2Alpha
  {
    get => _subWaste2Alpha;
    set => this.RaiseAndSetIfChanged(ref _subWaste2Alpha, value);
  }

  public string SubWasteId3
  {
    get => _subWasteId3;
    set => this.RaiseAndSetIfChanged(ref _subWasteId3, value);
  }

  public int SubWaste3Alpha
  {
    get => _subWaste3Alpha;
    set => this.RaiseAndSetIfChanged(ref _subWaste3Alpha, value);
  }

  public string SubWasteId4
  {
    get => _subWasteId4;
    set => this.RaiseAndSetIfChanged(ref _subWasteId4, value);
  }

  public int SubWaste4Alpha
  {
    get => _subWaste4Alpha;
    set => this.RaiseAndSetIfChanged(ref _subWaste4Alpha, value);
  }

  public int FlightTime
  {
    get => _flightTime;
    set => this.RaiseAndSetIfChanged(ref _flightTime, value);
  }

  public int WasteSpeed
  {
    get => _wasteSpeed;
    set => this.RaiseAndSetIfChanged(ref _wasteSpeed, value);
  }

  public int WasteDistanceX4
  {
    get => _wasteDistanceX4;
    set => this.RaiseAndSetIfChanged(ref _wasteDistanceX4, value);
  }

  public int WasteBeta
  {
    get => _wasteBeta;
    set => this.RaiseAndSetIfChanged(ref _wasteBeta, value);
  }
}