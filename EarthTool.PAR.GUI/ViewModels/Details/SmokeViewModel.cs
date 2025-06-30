using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class SmokeViewModel : DestructibleEntityViewModel
{
  private string _mesh1;
  private string _mesh2;
  private string _mesh3;
  private int _smokeTime1;
  private int _smokeTime2;
  private int _smokeTime3;
  private int _smokeFrequency;
  private int _startingTime;
  private int _smokingTime;
  private int _endingTime;
  private int _smokeUpSpeed;
  private int _newSmokeDistance;

  public SmokeViewModel(Smoke smoke)
    : base(smoke)
  {
    _mesh1 = smoke.Mesh1;
    _mesh2 = smoke.Mesh2;
    _mesh3 = smoke.Mesh3;
    _smokeTime1 = smoke.SmokeTime1;
    _smokeTime2 = smoke.SmokeTime2;
    _smokeTime3 = smoke.SmokeTime3;
    _smokeFrequency = smoke.SmokeFrequency;
    _startingTime = smoke.StartingTime;
    _smokingTime = smoke.SmokingTime;
    _endingTime = smoke.EndingTime;
    _smokeUpSpeed = smoke.SmokeUpSpeed;
    _newSmokeDistance = smoke.NewSmokeDistance;
  }

  public string Mesh1
  {
    get => _mesh1;
    set => this.RaiseAndSetIfChanged(ref _mesh1, value);
  }

  public string Mesh2
  {
    get => _mesh2;
    set => this.RaiseAndSetIfChanged(ref _mesh2, value);
  }

  public string Mesh3
  {
    get => _mesh3;
    set => this.RaiseAndSetIfChanged(ref _mesh3, value);
  }

  public int SmokeTime1
  {
    get => _smokeTime1;
    set => this.RaiseAndSetIfChanged(ref _smokeTime1, value);
  }

  public int SmokeTime2
  {
    get => _smokeTime2;
    set => this.RaiseAndSetIfChanged(ref _smokeTime2, value);
  }

  public int SmokeTime3
  {
    get => _smokeTime3;
    set => this.RaiseAndSetIfChanged(ref _smokeTime3, value);
  }

  public int SmokeFrequency
  {
    get => _smokeFrequency;
    set => this.RaiseAndSetIfChanged(ref _smokeFrequency, value);
  }

  public int StartingTime
  {
    get => _startingTime;
    set => this.RaiseAndSetIfChanged(ref _startingTime, value);
  }

  public int SmokingTime
  {
    get => _smokingTime;
    set => this.RaiseAndSetIfChanged(ref _smokingTime, value);
  }

  public int EndingTime
  {
    get => _endingTime;
    set => this.RaiseAndSetIfChanged(ref _endingTime, value);
  }

  public int SmokeUpSpeed
  {
    get => _smokeUpSpeed;
    set => this.RaiseAndSetIfChanged(ref _smokeUpSpeed, value);
  }

  public int NewSmokeDistance
  {
    get => _newSmokeDistance;
    set => this.RaiseAndSetIfChanged(ref _newSmokeDistance, value);
  }
}