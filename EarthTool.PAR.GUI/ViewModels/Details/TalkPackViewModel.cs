using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class TalkPackViewModel : EntityViewModel
{
  private string _selected;
  private string _move;
  private string _attack;
  private string _command;
  private string _enemy;
  private string _help;
  private string _freeWay;

  public TalkPackViewModel(TalkPack entry)
    : base(entry.Name, entry.RequiredResearch, entry.ClassId)
  {
    _selected = entry.Selected;
    _move = entry.Move;
    _attack = entry.Attack;
    _command = entry.Command;
    _enemy = entry.Enemy;
    _help = entry.Help;
    _freeWay = entry.FreeWay;
  }

  public string Selected
  {
    get => _selected;
    set => this.RaiseAndSetIfChanged(ref _selected, value);
  }

  public string Move
  {
    get => _move;
    set => this.RaiseAndSetIfChanged(ref _move, value);
  }

  public string Attack
  {
    get => _attack;
    set => this.RaiseAndSetIfChanged(ref _attack, value);
  }

  public string Command
  {
    get => _command;
    set => this.RaiseAndSetIfChanged(ref _command, value);
  }

  public string Enemy
  {
    get => _enemy;
    set => this.RaiseAndSetIfChanged(ref _enemy, value);
  }

  public string Help
  {
    get => _help;
    set => this.RaiseAndSetIfChanged(ref _help, value);
  }

  public string FreeWay
  {
    get => _freeWay;
    set => this.RaiseAndSetIfChanged(ref _freeWay, value);
  }
}