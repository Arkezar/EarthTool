using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class SpecialUpdateLinkViewModel : EntityViewModel
{
  private string _value;

  public SpecialUpdateLinkViewModel(SpecialUpdateLink entry)
    : base(entry.Name, entry.RequiredResearch, entry.ClassId)
  {
    _value = entry.Value;
  }

  public string Value
  {
    get => _value;
    set => this.RaiseAndSetIfChanged(ref _value, value);
  }
}