using EarthTool.PAR.Enums;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class EntityViewModel : ParameterEntryViewModel
{
  private EntityClassType _classId;

  protected EntityViewModel(string name, IEnumerable<int> requiredResearch, EntityClassType classId)
    : base(name)
  {
    RequiredResearch = new ObservableCollection<int>(requiredResearch);
    _classId = classId;
  }

  public ObservableCollection<int> RequiredResearch { get; }

  public EntityClassType ClassId
  {
    get => _classId;
    set => this.RaiseAndSetIfChanged(ref _classId, value);
  }
}