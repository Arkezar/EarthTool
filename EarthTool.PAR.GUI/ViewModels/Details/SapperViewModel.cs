using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class SapperViewModel : VehicleViewModel
{
  private int _minesLookRange;
  private string _mineId;
  private int _maxMinesCount;
  private int _animDownStart;
  private int _animDownEnd;
  private int _animUpStart;
  private int _animUpEnd;
  private string _putMineSmokeId;

  public SapperViewModel(Sapper sapper)
    : base(sapper)
  {
    _minesLookRange = sapper.MinesLookRange;
    _mineId = sapper.MineId;
    _maxMinesCount = sapper.MaxMinesCount;
    _animDownStart = sapper.AnimDownStart;
    _animDownEnd = sapper.AnimDownEnd;
    _animUpStart = sapper.AnimUpStart;
    _animUpEnd = sapper.AnimUpEnd;
    _putMineSmokeId = sapper.PutMineSmokeId;
  }

  public int MinesLookRange
  {
    get => _minesLookRange;
    set => this.RaiseAndSetIfChanged(ref _minesLookRange, value);
  }

  public string MineId
  {
    get => _mineId;
    set => this.RaiseAndSetIfChanged(ref _mineId, value);
  }

  public int MaxMinesCount
  {
    get => _maxMinesCount;
    set => this.RaiseAndSetIfChanged(ref _maxMinesCount, value);
  }

  public int AnimDownStart
  {
    get => _animDownStart;
    set => this.RaiseAndSetIfChanged(ref _animDownStart, value);
  }

  public int AnimDownEnd
  {
    get => _animDownEnd;
    set => this.RaiseAndSetIfChanged(ref _animDownEnd, value);
  }

  public int AnimUpStart
  {
    get => _animUpStart;
    set => this.RaiseAndSetIfChanged(ref _animUpStart, value);
  }

  public int AnimUpEnd
  {
    get => _animUpEnd;
    set => this.RaiseAndSetIfChanged(ref _animUpEnd, value);
  }

  public string PutMineSmokeId
  {
    get => _putMineSmokeId;
    set => this.RaiseAndSetIfChanged(ref _putMineSmokeId, value);
  }
}