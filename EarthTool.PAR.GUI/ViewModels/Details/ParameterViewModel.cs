using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class ParameterViewModel : TypelessEntityViewModel
{
  private ObservableCollection<bool> _fieldTypes;
  private ObservableCollection<string> _values;

  public ParameterViewModel(Parameter parameter)
    : base(parameter)
  {
    _fieldTypes = new ObservableCollection<bool>(parameter.FieldTypes);
    _values = new ObservableCollection<string>(parameter.Values);
  }

  public ObservableCollection<bool> FieldTypes
  {
    get => _fieldTypes;
    set => this.RaiseAndSetIfChanged(ref _fieldTypes, value);
  }

  public ObservableCollection<string> Values
  {
    get => _values;
    set => this.RaiseAndSetIfChanged(ref _values, value);
  }
}