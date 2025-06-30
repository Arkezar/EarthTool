using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class BuildingTransporterViewModel : VerticalTransporterViewModel
{
  private string _builderLineId;

  public BuildingTransporterViewModel(BuildingTransporter buildingTransporter)
    : base(buildingTransporter)
  {
    _builderLineId = buildingTransporter.BuilderLineId;
  }

  public string BuilderLineId
  {
    get => _builderLineId;
    set => this.RaiseAndSetIfChanged(ref _builderLineId, value);
  }
}