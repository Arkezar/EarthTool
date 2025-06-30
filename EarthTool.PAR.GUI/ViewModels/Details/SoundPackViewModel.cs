using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class SoundPackViewModel : TypelessEntityViewModel
{
  private string _normalWavePack1;
  private string _normalWavePack2;
  private string _normalWavePack3;
  private string _normalWavePack4;
  private string _loopedWavePack1;
  private string _loopedWavePack2;
  private string _loopedWavePack3;
  private string _loopedWavePack4;

  public SoundPackViewModel(SoundPack entry)
    : base(entry)
  {
    _normalWavePack1 = entry.NormalWavePack1;
    _normalWavePack2 = entry.NormalWavePack2;
    _normalWavePack3 = entry.NormalWavePack3;
    _normalWavePack4 = entry.NormalWavePack4;
    _loopedWavePack1 = entry.LoopedWavePack1;
    _loopedWavePack2 = entry.LoopedWavePack2;
    _loopedWavePack3 = entry.LoopedWavePack3;
    _loopedWavePack4 = entry.LoopedWavePack4;
  }

  public string NormalWavePack1
  {
    get => _normalWavePack1;
    set => this.RaiseAndSetIfChanged(ref _normalWavePack1, value);
  }

  public string NormalWavePack2
  {
    get => _normalWavePack2;
    set => this.RaiseAndSetIfChanged(ref _normalWavePack2, value);
  }

  public string NormalWavePack3
  {
    get => _normalWavePack3;
    set => this.RaiseAndSetIfChanged(ref _normalWavePack3, value);
  }

  public string NormalWavePack4
  {
    get => _normalWavePack4;
    set => this.RaiseAndSetIfChanged(ref _normalWavePack4, value);
  }

  public string LoopedWavePack1
  {
    get => _loopedWavePack1;
    set => this.RaiseAndSetIfChanged(ref _loopedWavePack1, value);
  }

  public string LoopedWavePack2
  {
    get => _loopedWavePack2;
    set => this.RaiseAndSetIfChanged(ref _loopedWavePack2, value);
  }

  public string LoopedWavePack3
  {
    get => _loopedWavePack3;
    set => this.RaiseAndSetIfChanged(ref _loopedWavePack3, value);
  }

  public string LoopedWavePack4
  {
    get => _loopedWavePack4;
    set => this.RaiseAndSetIfChanged(ref _loopedWavePack4, value);
  }
}