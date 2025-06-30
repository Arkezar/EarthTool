using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class MultiExplosionViewModel : InteractableEntityViewModel
{
  private int _useDownBuilding;
  private int _downBuildingStart;
  private int _downBuildingTime;
  private string _subObject1;
  private int _time1;
  private int _angle1;
  private int _dist4X1;
  private string _subObject2;
  private int _time2;
  private int _angle2;
  private int _dist4X2;
  private string _subObject3;
  private int _time3;
  private int _angle3;
  private int _dist4X3;
  private string _subObject4;
  private int _time4;
  private int _angle4;
  private int _dist4X4;
  private string _subObject5;
  private int _time5;
  private int _angle5;
  private int _dist4X5;
  private string _subObject6;
  private int _time6;
  private int _angle6;
  private int _dist4X6;
  private string _subObject7;
  private int _time7;
  private int _angle7;
  private int _dist4X7;
  private string _subObject8;
  private int _time8;
  private int _angle8;
  private int _dist4X8;

  public MultiExplosionViewModel(MultiExplosion multiExplosion)
    : base(multiExplosion)
  {
    _useDownBuilding = multiExplosion.UseDownBuilding;
    _downBuildingStart = multiExplosion.DownBuildingStart;
    _downBuildingTime = multiExplosion.DownBuildingTime;
    _subObject1 = multiExplosion.SubObject1;
    _time1 = multiExplosion.Time1;
    _angle1 = multiExplosion.Angle1;
    _dist4X1 = multiExplosion.Dist4X1;
    _subObject2 = multiExplosion.SubObject2;
    _time2 = multiExplosion.Time2;
    _angle2 = multiExplosion.Angle2;
    _dist4X2 = multiExplosion.Dist4X2;
    _subObject3 = multiExplosion.SubObject3;
    _time3 = multiExplosion.Time3;
    _angle3 = multiExplosion.Angle3;
    _dist4X3 = multiExplosion.Dist4X3;
    _subObject4 = multiExplosion.SubObject4;
    _time4 = multiExplosion.Time4;
    _angle4 = multiExplosion.Angle4;
    _dist4X4 = multiExplosion.Dist4X4;
    _subObject5 = multiExplosion.SubObject5;
    _time5 = multiExplosion.Time5;
    _angle5 = multiExplosion.Angle5;
    _dist4X5 = multiExplosion.Dist4X5;
    _subObject6 = multiExplosion.SubObject6;
    _time6 = multiExplosion.Time6;
    _angle6 = multiExplosion.Angle6;
    _dist4X6 = multiExplosion.Dist4X6;
    _subObject7 = multiExplosion.SubObject7;
    _time7 = multiExplosion.Time7;
    _angle7 = multiExplosion.Angle7;
    _dist4X7 = multiExplosion.Dist4X7;
    _subObject8 = multiExplosion.SubObject8;
    _time8 = multiExplosion.Time8;
    _angle8 = multiExplosion.Angle8;
    _dist4X8 = multiExplosion.Dist4X8;
  }

  public int UseDownBuilding
  {
    get => _useDownBuilding;
    set => this.RaiseAndSetIfChanged(ref _useDownBuilding, value);
  }

  public int DownBuildingStart
  {
    get => _downBuildingStart;
    set => this.RaiseAndSetIfChanged(ref _downBuildingStart, value);
  }

  public int DownBuildingTime
  {
    get => _downBuildingTime;
    set => this.RaiseAndSetIfChanged(ref _downBuildingTime, value);
  }

  public string SubObject1
  {
    get => _subObject1;
    set => this.RaiseAndSetIfChanged(ref _subObject1, value);
  }

  public int Time1
  {
    get => _time1;
    set => this.RaiseAndSetIfChanged(ref _time1, value);
  }

  public int Angle1
  {
    get => _angle1;
    set => this.RaiseAndSetIfChanged(ref _angle1, value);
  }

  public int Dist4X1
  {
    get => _dist4X1;
    set => this.RaiseAndSetIfChanged(ref _dist4X1, value);
  }

  public string SubObject2
  {
    get => _subObject2;
    set => this.RaiseAndSetIfChanged(ref _subObject2, value);
  }

  public int Time2
  {
    get => _time2;
    set => this.RaiseAndSetIfChanged(ref _time2, value);
  }

  public int Angle2
  {
    get => _angle2;
    set => this.RaiseAndSetIfChanged(ref _angle2, value);
  }

  public int Dist4X2
  {
    get => _dist4X2;
    set => this.RaiseAndSetIfChanged(ref _dist4X2, value);
  }

  public string SubObject3
  {
    get => _subObject3;
    set => this.RaiseAndSetIfChanged(ref _subObject3, value);
  }

  public int Time3
  {
    get => _time3;
    set => this.RaiseAndSetIfChanged(ref _time3, value);
  }

  public int Angle3
  {
    get => _angle3;
    set => this.RaiseAndSetIfChanged(ref _angle3, value);
  }

  public int Dist4X3
  {
    get => _dist4X3;
    set => this.RaiseAndSetIfChanged(ref _dist4X3, value);
  }

  public string SubObject4
  {
    get => _subObject4;
    set => this.RaiseAndSetIfChanged(ref _subObject4, value);
  }

  public int Time4
  {
    get => _time4;
    set => this.RaiseAndSetIfChanged(ref _time4, value);
  }

  public int Angle4
  {
    get => _angle4;
    set => this.RaiseAndSetIfChanged(ref _angle4, value);
  }

  public int Dist4X4
  {
    get => _dist4X4;
    set => this.RaiseAndSetIfChanged(ref _dist4X4, value);
  }

  public string SubObject5
  {
    get => _subObject5;
    set => this.RaiseAndSetIfChanged(ref _subObject5, value);
  }

  public int Time5
  {
    get => _time5;
    set => this.RaiseAndSetIfChanged(ref _time5, value);
  }

  public int Angle5
  {
    get => _angle5;
    set => this.RaiseAndSetIfChanged(ref _angle5, value);
  }

  public int Dist4X5
  {
    get => _dist4X5;
    set => this.RaiseAndSetIfChanged(ref _dist4X5, value);
  }

  public string SubObject6
  {
    get => _subObject6;
    set => this.RaiseAndSetIfChanged(ref _subObject6, value);
  }

  public int Time6
  {
    get => _time6;
    set => this.RaiseAndSetIfChanged(ref _time6, value);
  }

  public int Angle6
  {
    get => _angle6;
    set => this.RaiseAndSetIfChanged(ref _angle6, value);
  }

  public int Dist4X6
  {
    get => _dist4X6;
    set => this.RaiseAndSetIfChanged(ref _dist4X6, value);
  }

  public string SubObject7
  {
    get => _subObject7;
    set => this.RaiseAndSetIfChanged(ref _subObject7, value);
  }

  public int Time7
  {
    get => _time7;
    set => this.RaiseAndSetIfChanged(ref _time7, value);
  }

  public int Angle7
  {
    get => _angle7;
    set => this.RaiseAndSetIfChanged(ref _angle7, value);
  }

  public int Dist4X7
  {
    get => _dist4X7;
    set => this.RaiseAndSetIfChanged(ref _dist4X7, value);
  }

  public string SubObject8
  {
    get => _subObject8;
    set => this.RaiseAndSetIfChanged(ref _subObject8, value);
  }

  public int Time8
  {
    get => _time8;
    set => this.RaiseAndSetIfChanged(ref _time8, value);
  }

  public int Angle8
  {
    get => _angle8;
    set => this.RaiseAndSetIfChanged(ref _angle8, value);
  }

  public int Dist4X8
  {
    get => _dist4X8;
    set => this.RaiseAndSetIfChanged(ref _dist4X8, value);
  }
}