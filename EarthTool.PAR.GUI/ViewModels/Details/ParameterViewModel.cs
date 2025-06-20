using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class ParameterViewModel : EntityViewModel
{
  public ParameterViewModel(Parameter parameter)
    : base(parameter.Name, parameter.RequiredResearch, parameter.ClassId)
  {
    Values = new ObservableCollection<string>(parameter.Values);
  }

  public ObservableCollection<string> Values { get; }
}