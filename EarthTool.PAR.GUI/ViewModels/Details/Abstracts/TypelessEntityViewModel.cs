using EarthTool.PAR.Models.Abstracts;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class TypelessEntityViewModel : EntityViewModel
{
  protected TypelessEntityViewModel(TypelessEntity entity)
    : base(entity)
  {
  }
}