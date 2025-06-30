using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class ContainerTransporterViewModel : EquipmentViewModel
{
  private int _animContainerDownStart;
  private int _animContainerDownEnd;
  private int _animContainerUpStart;
  private int _animContainerUpEnd;

  public ContainerTransporterViewModel(ContainerTransporter containerTransporter)
    : base(containerTransporter)
  {
    _animContainerDownStart = containerTransporter.AnimContainerDownStart;
    _animContainerDownEnd = containerTransporter.AnimContainerDownEnd;
    _animContainerUpStart = containerTransporter.AnimContainerUpStart;
    _animContainerUpEnd = containerTransporter.AnimContainerUpEnd;
  }

  public int AnimContainerDownStart
  {
    get => _animContainerDownStart;
    set => this.RaiseAndSetIfChanged(ref _animContainerDownStart, value);
  }

  public int AnimContainerDownEnd
  {
    get => _animContainerDownEnd;
    set => this.RaiseAndSetIfChanged(ref _animContainerDownEnd, value);
  }

  public int AnimContainerUpStart
  {
    get => _animContainerUpStart;
    set => this.RaiseAndSetIfChanged(ref _animContainerUpStart, value);
  }

  public int AnimContainerUpEnd
  {
    get => _animContainerUpEnd;
    set => this.RaiseAndSetIfChanged(ref _animContainerUpEnd, value);
  }
}