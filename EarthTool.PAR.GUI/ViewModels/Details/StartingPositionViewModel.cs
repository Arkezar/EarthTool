using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class StartingPositionViewModel : EquipableEntityViewModel
{
  private int _positionType;

  public StartingPositionViewModel(StartingPosition startingPosition)
    : base(startingPosition)
  {
    _positionType = startingPosition.PositionType;
  }

  public int PositionType
  {
    get => _positionType;
    set => this.RaiseAndSetIfChanged(ref _positionType, value);
  }
}