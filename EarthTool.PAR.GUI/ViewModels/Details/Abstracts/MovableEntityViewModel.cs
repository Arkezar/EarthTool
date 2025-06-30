using EarthTool.PAR.Models.Abstracts;

namespace EarthTool.PAR.GUI.ViewModels.Details.Abstracts;

public abstract class MovableEntityViewModel : InteractableEntityViewModel
{
  protected MovableEntityViewModel(MovableEntity entity)
    : base(entity)
  {
  }
}