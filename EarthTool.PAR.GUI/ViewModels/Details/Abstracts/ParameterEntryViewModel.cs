using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class ParameterEntryViewModel : ViewModelBase
{
  private string _name;

  protected ParameterEntryViewModel(ParameterEntry entry)
  {
    _name = entry.Name;
  }

  public string Name
  {
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
  }
}