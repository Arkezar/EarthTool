using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class ExplosionViewModel : DestructibleEntityViewModel
{
  private int _explosionTicks;
  private int _explosionFlags;

  public ExplosionViewModel(Explosion explosion)
    : base(explosion)
  {
    _explosionTicks = explosion.ExplosionTicks;
    _explosionFlags = explosion.ExplosionFlags;
  }

  public int ExplosionTicks
  {
    get => _explosionTicks;
    set => this.RaiseAndSetIfChanged(ref _explosionTicks, value);
  }

  public int ExplosionFlags
  {
    get => _explosionFlags;
    set => this.RaiseAndSetIfChanged(ref _explosionFlags, value);
  }
}