using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class ParameterEntryViewModel : ViewModelBase
{
  private string _name;

  protected ParameterEntryViewModel(string name)
  {
    _name = name;
  }

  public string Name
  {
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
  }
}