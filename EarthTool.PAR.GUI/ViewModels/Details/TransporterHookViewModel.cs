using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class TransporterHookViewModel : EquipmentViewModel
{
  private int _animTransporterDownStart;
  private int _animTransporterDownEnd;
  private int _animTransporterUpStart;
  private int _animTransporterUpEnd;
  private int _angleToGetPut;
  private int _angleOfGetUnitByLandTransporter;
  private int _takeHeight;

  public TransporterHookViewModel(TransporterHook transporterHook)
    : base(transporterHook)
  {
    _animTransporterDownStart = transporterHook.AnimTransporterDownStart;
    _animTransporterDownEnd = transporterHook.AnimTransporterDownEnd;
    _animTransporterUpStart = transporterHook.AnimTransporterUpStart;
    _animTransporterUpEnd = transporterHook.AnimTransporterUpEnd;
    _angleToGetPut = transporterHook.AngleToGetPut;
    _angleOfGetUnitByLandTransporter = transporterHook.AngleOfGetUnitByLandTransporter;
    _takeHeight = transporterHook.TakeHeight;
  }

  public int AnimTransporterDownStart
  {
    get => _animTransporterDownStart;
    set => this.RaiseAndSetIfChanged(ref _animTransporterDownStart, value);
  }

  public int AnimTransporterDownEnd
  {
    get => _animTransporterDownEnd;
    set => this.RaiseAndSetIfChanged(ref _animTransporterDownEnd, value);
  }

  public int AnimTransporterUpStart
  {
    get => _animTransporterUpStart;
    set => this.RaiseAndSetIfChanged(ref _animTransporterUpStart, value);
  }

  public int AnimTransporterUpEnd
  {
    get => _animTransporterUpEnd;
    set => this.RaiseAndSetIfChanged(ref _animTransporterUpEnd, value);
  }

  public int AngleToGetPut
  {
    get => _angleToGetPut;
    set => this.RaiseAndSetIfChanged(ref _angleToGetPut, value);
  }

  public int AngleOfGetUnitByLandTransporter
  {
    get => _angleOfGetUnitByLandTransporter;
    set => this.RaiseAndSetIfChanged(ref _angleOfGetUnitByLandTransporter, value);
  }

  public int TakeHeight
  {
    get => _takeHeight;
    set => this.RaiseAndSetIfChanged(ref _takeHeight, value);
  }
}