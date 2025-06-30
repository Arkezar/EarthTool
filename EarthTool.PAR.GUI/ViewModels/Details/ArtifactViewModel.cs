using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class ArtifactViewModel : PassiveEntityViewModel
{
  private int _artefactMask;
  private int _artefactParam;
  private int _respawnTime;

  public ArtifactViewModel(Artifact artifact)
    : base(artifact)
  {
    _artefactMask = artifact.ArtefactMask;
    _artefactParam = artifact.ArtefactParam;
    _respawnTime = artifact.RespawnTime;
  }

  public int ArtefactMask
  {
    get => _artefactMask;
    set => this.RaiseAndSetIfChanged(ref _artefactMask, value);
  }

  public int ArtefactParam
  {
    get => _artefactParam;
    set => this.RaiseAndSetIfChanged(ref _artefactParam, value);
  }

  public int RespawnTime
  {
    get => _respawnTime;
    set => this.RaiseAndSetIfChanged(ref _respawnTime, value);
  }
}