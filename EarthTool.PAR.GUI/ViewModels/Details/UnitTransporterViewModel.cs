using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class UnitTransporterViewModel : VerticalTransporterViewModel
{
  private int _unitsCount;
  private int _dockingHeight;
  private int _animLoadingStartStart;
  private int _animLoadingStartEnd;
  private int _animLoadingEndStart;
  private int _animLoadingEndEnd;
  private int _animUnloadingStartStart;
  private int _animUnloadingStartEnd;
  private int _animUnloadingEndStart;
  private int _animUnloadingEndEnd;

  public UnitTransporterViewModel(UnitTransporter unitTransporter)
    : base(unitTransporter)
  {
    _unitsCount = unitTransporter.UnitsCount;
    _dockingHeight = unitTransporter.DockingHeight;
    _animLoadingStartStart = unitTransporter.AnimLoadingStartStart;
    _animLoadingStartEnd = unitTransporter.AnimLoadingStartEnd;
    _animLoadingEndStart = unitTransporter.AnimLoadingEndStart;
    _animLoadingEndEnd = unitTransporter.AnimLoadingEndEnd;
    _animUnloadingStartStart = unitTransporter.AnimUnloadingStartStart;
    _animUnloadingStartEnd = unitTransporter.AnimUnloadingStartEnd;
    _animUnloadingEndStart = unitTransporter.AnimUnloadingEndStart;
    _animUnloadingEndEnd = unitTransporter.AnimUnloadingEndEnd;
  }

  public int UnitsCount
  {
    get => _unitsCount;
    set => this.RaiseAndSetIfChanged(ref _unitsCount, value);
  }

  public int DockingHeight
  {
    get => _dockingHeight;
    set => this.RaiseAndSetIfChanged(ref _dockingHeight, value);
  }

  public int AnimLoadingStartStart
  {
    get => _animLoadingStartStart;
    set => this.RaiseAndSetIfChanged(ref _animLoadingStartStart, value);
  }

  public int AnimLoadingStartEnd
  {
    get => _animLoadingStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animLoadingStartEnd, value);
  }

  public int AnimLoadingEndStart
  {
    get => _animLoadingEndStart;
    set => this.RaiseAndSetIfChanged(ref _animLoadingEndStart, value);
  }

  public int AnimLoadingEndEnd
  {
    get => _animLoadingEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animLoadingEndEnd, value);
  }

  public int AnimUnloadingStartStart
  {
    get => _animUnloadingStartStart;
    set => this.RaiseAndSetIfChanged(ref _animUnloadingStartStart, value);
  }

  public int AnimUnloadingStartEnd
  {
    get => _animUnloadingStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animUnloadingStartEnd, value);
  }

  public int AnimUnloadingEndStart
  {
    get => _animUnloadingEndStart;
    set => this.RaiseAndSetIfChanged(ref _animUnloadingEndStart, value);
  }

  public int AnimUnloadingEndEnd
  {
    get => _animUnloadingEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animUnloadingEndEnd, value);
  }
}