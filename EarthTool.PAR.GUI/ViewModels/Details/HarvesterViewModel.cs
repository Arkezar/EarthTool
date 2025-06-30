using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class HarvesterViewModel : VehicleViewModel
{
  private int _containerCount;
  private int _ticksPerContainer;
  private int _putResourceAngle;
  private int _animHarvestStartStart;
  private int _animHarvestStartEnd;
  private int _animHarvestWorkStart;
  private int _animHarvestWorkEnd;
  private int _animHarvestEndStart;
  private int _animHarvestEndEnd;
  private string _harvestSomkeId;

  public HarvesterViewModel(Harvester harvester)
    : base(harvester)
  {
    _containerCount = harvester.ContainerCount;
    _ticksPerContainer = harvester.TicksPerContainer;
    _putResourceAngle = harvester.PutResourceAngle;
    _animHarvestStartStart = harvester.AnimHarvestStartStart;
    _animHarvestStartEnd = harvester.AnimHarvestStartEnd;
    _animHarvestWorkStart = harvester.AnimHarvestWorkStart;
    _animHarvestWorkEnd = harvester.AnimHarvestWorkEnd;
    _animHarvestEndStart = harvester.AnimHarvestEndStart;
    _animHarvestEndEnd = harvester.AnimHarvestEndEnd;
    _harvestSomkeId = harvester.HarvestSomkeId;
  }

  public int ContainerCount
  {
    get => _containerCount;
    set => this.RaiseAndSetIfChanged(ref _containerCount, value);
  }

  public int TicksPerContainer
  {
    get => _ticksPerContainer;
    set => this.RaiseAndSetIfChanged(ref _ticksPerContainer, value);
  }

  public int PutResourceAngle
  {
    get => _putResourceAngle;
    set => this.RaiseAndSetIfChanged(ref _putResourceAngle, value);
  }

  public int AnimHarvestStartStart
  {
    get => _animHarvestStartStart;
    set => this.RaiseAndSetIfChanged(ref _animHarvestStartStart, value);
  }

  public int AnimHarvestStartEnd
  {
    get => _animHarvestStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animHarvestStartEnd, value);
  }

  public int AnimHarvestWorkStart
  {
    get => _animHarvestWorkStart;
    set => this.RaiseAndSetIfChanged(ref _animHarvestWorkStart, value);
  }

  public int AnimHarvestWorkEnd
  {
    get => _animHarvestWorkEnd;
    set => this.RaiseAndSetIfChanged(ref _animHarvestWorkEnd, value);
  }

  public int AnimHarvestEndStart
  {
    get => _animHarvestEndStart;
    set => this.RaiseAndSetIfChanged(ref _animHarvestEndStart, value);
  }

  public int AnimHarvestEndEnd
  {
    get => _animHarvestEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animHarvestEndEnd, value);
  }

  public string HarvestSomkeId
  {
    get => _harvestSomkeId;
    set => this.RaiseAndSetIfChanged(ref _harvestSomkeId, value);
  }
}