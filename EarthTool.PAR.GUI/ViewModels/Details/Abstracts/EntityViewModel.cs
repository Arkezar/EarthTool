using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class EntityViewModel : ParameterEntryViewModel
{
  private EntityClassType _classId;

  protected EntityViewModel(Entity entity)
    : base(entity)
  {
    RequiredResearch = new ObservableCollection<int>(entity.RequiredResearch);
    _classId = entity.ClassId;
  }

  public ObservableCollection<int> RequiredResearch { get; }

  public EntityClassType ClassId
  {
    get => _classId;
    set => this.RaiseAndSetIfChanged(ref _classId, value);
  }
}