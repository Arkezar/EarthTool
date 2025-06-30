using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class BuilderViewModel : VehicleViewModel
{
  private string _wallId;
  private string _bridgeId;
  private int _tunnelNumber;
  private int _roadBuildTime;
  private int _flatBuildTime;
  private int _trenchBuildTime;
  private int _tunnelBuildTime;
  private int _buildObjectAnimationAngle;
  private int _digNormalAnimationAngle;
  private int _digLowAnimationAngle;
  private int _animBuildObjectStartStart;
  private int _animBuildObjectStartEnd;
  private int _animBuildObjectWorkStart;
  private int _animBuildObjectWorkEnd;
  private int _animBuildObjectEndStart;
  private int _animBuildObjectEndEnd;
  private int _animDigNormalStartStart;
  private int _animDigNormalStartEnd;
  private int _animDigNormalWorkStart;
  private int _animDigNormalWorkEnd;
  private int _animDigNormalEndStart;
  private int _animDigNormalEndEnd;
  private int _animDigLowStartStart;
  private int _animDigLowStartEnd;
  private int _animDigLowWorkStart;
  private int _animDigLowWorkEnd;
  private int _animDigLowEndStart;
  private int _animDigLowEndEnd;
  private string _digSmokeId;

  public BuilderViewModel(Builder builder)
    : base(builder)
  {
    _wallId = builder.WallId;
    _bridgeId = builder.BridgeId;
    _tunnelNumber = builder.TunnelNumber;
    _roadBuildTime = builder.RoadBuildTime;
    _flatBuildTime = builder.FlatBuildTime;
    _trenchBuildTime = builder.TrenchBuildTime;
    _tunnelBuildTime = builder.TunnelBuildTime;
    _buildObjectAnimationAngle = builder.BuildObjectAnimationAngle;
    _digNormalAnimationAngle = builder.DigNormalAnimationAngle;
    _digLowAnimationAngle = builder.DigLowAnimationAngle;
    _animBuildObjectStartStart = builder.AnimBuildObjectStartStart;
    _animBuildObjectStartEnd = builder.AnimBuildObjectStartEnd;
    _animBuildObjectWorkStart = builder.AnimBuildObjectWorkStart;
    _animBuildObjectWorkEnd = builder.AnimBuildObjectWorkEnd;
    _animBuildObjectEndStart = builder.AnimBuildObjectEndStart;
    _animBuildObjectEndEnd = builder.AnimBuildObjectEndEnd;
    _animDigNormalStartStart = builder.AnimDigNormalStartStart;
    _animDigNormalStartEnd = builder.AnimDigNormalStartEnd;
    _animDigNormalWorkStart = builder.AnimDigNormalWorkStart;
    _animDigNormalWorkEnd = builder.AnimDigNormalWorkEnd;
    _animDigNormalEndStart = builder.AnimDigNormalEndStart;
    _animDigNormalEndEnd = builder.AnimDigNormalEndEnd;
    _animDigLowStartStart = builder.AnimDigLowStartStart;
    _animDigLowStartEnd = builder.AnimDigLowStartEnd;
    _animDigLowWorkStart = builder.AnimDigLowWorkStart;
    _animDigLowWorkEnd = builder.AnimDigLowWorkEnd;
    _animDigLowEndStart = builder.AnimDigLowEndStart;
    _animDigLowEndEnd = builder.AnimDigLowEndEnd;
    _digSmokeId = builder.DigSmokeId;
  }

  public string WallId
  {
    get => _wallId;
    set => this.RaiseAndSetIfChanged(ref _wallId, value);
  }

  public string BridgeId
  {
    get => _bridgeId;
    set => this.RaiseAndSetIfChanged(ref _bridgeId, value);
  }

  public int TunnelNumber
  {
    get => _tunnelNumber;
    set => this.RaiseAndSetIfChanged(ref _tunnelNumber, value);
  }

  public int RoadBuildTime
  {
    get => _roadBuildTime;
    set => this.RaiseAndSetIfChanged(ref _roadBuildTime, value);
  }

  public int FlatBuildTime
  {
    get => _flatBuildTime;
    set => this.RaiseAndSetIfChanged(ref _flatBuildTime, value);
  }

  public int TrenchBuildTime
  {
    get => _trenchBuildTime;
    set => this.RaiseAndSetIfChanged(ref _trenchBuildTime, value);
  }

  public int TunnelBuildTime
  {
    get => _tunnelBuildTime;
    set => this.RaiseAndSetIfChanged(ref _tunnelBuildTime, value);
  }

  public int BuildObjectAnimationAngle
  {
    get => _buildObjectAnimationAngle;
    set => this.RaiseAndSetIfChanged(ref _buildObjectAnimationAngle, value);
  }

  public int DigNormalAnimationAngle
  {
    get => _digNormalAnimationAngle;
    set => this.RaiseAndSetIfChanged(ref _digNormalAnimationAngle, value);
  }

  public int DigLowAnimationAngle
  {
    get => _digLowAnimationAngle;
    set => this.RaiseAndSetIfChanged(ref _digLowAnimationAngle, value);
  }

  public int AnimBuildObjectStartStart
  {
    get => _animBuildObjectStartStart;
    set => this.RaiseAndSetIfChanged(ref _animBuildObjectStartStart, value);
  }

  public int AnimBuildObjectStartEnd
  {
    get => _animBuildObjectStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animBuildObjectStartEnd, value);
  }

  public int AnimBuildObjectWorkStart
  {
    get => _animBuildObjectWorkStart;
    set => this.RaiseAndSetIfChanged(ref _animBuildObjectWorkStart, value);
  }

  public int AnimBuildObjectWorkEnd
  {
    get => _animBuildObjectWorkEnd;
    set => this.RaiseAndSetIfChanged(ref _animBuildObjectWorkEnd, value);
  }

  public int AnimBuildObjectEndStart
  {
    get => _animBuildObjectEndStart;
    set => this.RaiseAndSetIfChanged(ref _animBuildObjectEndStart, value);
  }

  public int AnimBuildObjectEndEnd
  {
    get => _animBuildObjectEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animBuildObjectEndEnd, value);
  }

  public int AnimDigNormalStartStart
  {
    get => _animDigNormalStartStart;
    set => this.RaiseAndSetIfChanged(ref _animDigNormalStartStart, value);
  }

  public int AnimDigNormalStartEnd
  {
    get => _animDigNormalStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animDigNormalStartEnd, value);
  }

  public int AnimDigNormalWorkStart
  {
    get => _animDigNormalWorkStart;
    set => this.RaiseAndSetIfChanged(ref _animDigNormalWorkStart, value);
  }

  public int AnimDigNormalWorkEnd
  {
    get => _animDigNormalWorkEnd;
    set => this.RaiseAndSetIfChanged(ref _animDigNormalWorkEnd, value);
  }

  public int AnimDigNormalEndStart
  {
    get => _animDigNormalEndStart;
    set => this.RaiseAndSetIfChanged(ref _animDigNormalEndStart, value);
  }

  public int AnimDigNormalEndEnd
  {
    get => _animDigNormalEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animDigNormalEndEnd, value);
  }

  public int AnimDigLowStartStart
  {
    get => _animDigLowStartStart;
    set => this.RaiseAndSetIfChanged(ref _animDigLowStartStart, value);
  }

  public int AnimDigLowStartEnd
  {
    get => _animDigLowStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animDigLowStartEnd, value);
  }

  public int AnimDigLowWorkStart
  {
    get => _animDigLowWorkStart;
    set => this.RaiseAndSetIfChanged(ref _animDigLowWorkStart, value);
  }

  public int AnimDigLowWorkEnd
  {
    get => _animDigLowWorkEnd;
    set => this.RaiseAndSetIfChanged(ref _animDigLowWorkEnd, value);
  }

  public int AnimDigLowEndStart
  {
    get => _animDigLowEndStart;
    set => this.RaiseAndSetIfChanged(ref _animDigLowEndStart, value);
  }

  public int AnimDigLowEndEnd
  {
    get => _animDigLowEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animDigLowEndEnd, value);
  }

  public string DigSmokeId
  {
    get => _digSmokeId;
    set => this.RaiseAndSetIfChanged(ref _digSmokeId, value);
  }
}