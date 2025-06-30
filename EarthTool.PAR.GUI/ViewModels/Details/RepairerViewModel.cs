using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class RepairerViewModel : EquipmentViewModel
{
  private RepairerCapabilities _repairerCapabilities;
  private int _repairHPPerTick;
  private int _repairElectronicsPerTick;
  private int _ticksPerRepair;
  private int _convertTankTime;
  private int _convertBuildingTime;
  private int _convertHealthyTankTime;
  private int _convertHealthyBuildingTime;
  private int _repaintTankTime;
  private int _repaintBuildingTime;
  private int _upgradeTankTime;
  private int _animRepairStartStart;
  private int _animRepairStartEnd;
  private int _animRepairWorkStart;
  private int _animRepairWorkEnd;
  private int _animRepairEndStart;
  private int _animRepairEndEnd;
  private int _animConvertStartStart;
  private int _animConvertStartEnd;
  private int _animConvertWorkStart;
  private int _animConvertWorkEnd;
  private int _animConvertEndStart;
  private int _animConvertEndEnd;
  private int _animRepaintStartStart;
  private int _animRepaintStartEnd;
  private int _animRepaintWorkStart;
  private int _animRepaintWorkEnd;
  private int _animRepaintEndStart;
  private int _animRepaintEndEnd;

  public RepairerViewModel(Repairer repairer)
    : base(repairer)
  {
    _repairerCapabilities = repairer.RepairerCapabilities;
    _repairHPPerTick = repairer.RepairHPPerTick;
    _repairElectronicsPerTick = repairer.RepairElectronicsPerTick;
    _ticksPerRepair = repairer.TicksPerRepair;
    _convertTankTime = repairer.ConvertTankTime;
    _convertBuildingTime = repairer.ConvertBuildingTime;
    _convertHealthyTankTime = repairer.ConvertHealthyTankTime;
    _convertHealthyBuildingTime = repairer.ConvertHealthyBuildingTime;
    _repaintTankTime = repairer.RepaintTankTime;
    _repaintBuildingTime = repairer.RepaintBuildingTime;
    _upgradeTankTime = repairer.UpgradeTankTime;
    _animRepairStartStart = repairer.AnimRepairStartStart;
    _animRepairStartEnd = repairer.AnimRepairStartEnd;
    _animRepairWorkStart = repairer.AnimRepairWorkStart;
    _animRepairWorkEnd = repairer.AnimRepairWorkEnd;
    _animRepairEndStart = repairer.AnimRepairEndStart;
    _animRepairEndEnd = repairer.AnimRepairEndEnd;
    _animConvertStartStart = repairer.AnimConvertStartStart;
    _animConvertStartEnd = repairer.AnimConvertStartEnd;
    _animConvertWorkStart = repairer.AnimConvertWorkStart;
    _animConvertWorkEnd = repairer.AnimConvertWorkEnd;
    _animConvertEndStart = repairer.AnimConvertEndStart;
    _animConvertEndEnd = repairer.AnimConvertEndEnd;
    _animRepaintStartStart = repairer.AnimRepaintStartStart;
    _animRepaintStartEnd = repairer.AnimRepaintStartEnd;
    _animRepaintWorkStart = repairer.AnimRepaintWorkStart;
    _animRepaintWorkEnd = repairer.AnimRepaintWorkEnd;
    _animRepaintEndStart = repairer.AnimRepaintEndStart;
    _animRepaintEndEnd = repairer.AnimRepaintEndEnd;
  }

  public RepairerCapabilities RepairerCapabilities
  {
    get => _repairerCapabilities;
    set => this.RaiseAndSetIfChanged(ref _repairerCapabilities, value);
  }

  public int RepairHPPerTick
  {
    get => _repairHPPerTick;
    set => this.RaiseAndSetIfChanged(ref _repairHPPerTick, value);
  }

  public int RepairElectronicsPerTick
  {
    get => _repairElectronicsPerTick;
    set => this.RaiseAndSetIfChanged(ref _repairElectronicsPerTick, value);
  }

  public int TicksPerRepair
  {
    get => _ticksPerRepair;
    set => this.RaiseAndSetIfChanged(ref _ticksPerRepair, value);
  }

  public int ConvertTankTime
  {
    get => _convertTankTime;
    set => this.RaiseAndSetIfChanged(ref _convertTankTime, value);
  }

  public int ConvertBuildingTime
  {
    get => _convertBuildingTime;
    set => this.RaiseAndSetIfChanged(ref _convertBuildingTime, value);
  }

  public int ConvertHealthyTankTime
  {
    get => _convertHealthyTankTime;
    set => this.RaiseAndSetIfChanged(ref _convertHealthyTankTime, value);
  }

  public int ConvertHealthyBuildingTime
  {
    get => _convertHealthyBuildingTime;
    set => this.RaiseAndSetIfChanged(ref _convertHealthyBuildingTime, value);
  }

  public int RepaintTankTime
  {
    get => _repaintTankTime;
    set => this.RaiseAndSetIfChanged(ref _repaintTankTime, value);
  }

  public int RepaintBuildingTime
  {
    get => _repaintBuildingTime;
    set => this.RaiseAndSetIfChanged(ref _repaintBuildingTime, value);
  }

  public int UpgradeTankTime
  {
    get => _upgradeTankTime;
    set => this.RaiseAndSetIfChanged(ref _upgradeTankTime, value);
  }

  public int AnimRepairStartStart
  {
    get => _animRepairStartStart;
    set => this.RaiseAndSetIfChanged(ref _animRepairStartStart, value);
  }

  public int AnimRepairStartEnd
  {
    get => _animRepairStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animRepairStartEnd, value);
  }

  public int AnimRepairWorkStart
  {
    get => _animRepairWorkStart;
    set => this.RaiseAndSetIfChanged(ref _animRepairWorkStart, value);
  }

  public int AnimRepairWorkEnd
  {
    get => _animRepairWorkEnd;
    set => this.RaiseAndSetIfChanged(ref _animRepairWorkEnd, value);
  }

  public int AnimRepairEndStart
  {
    get => _animRepairEndStart;
    set => this.RaiseAndSetIfChanged(ref _animRepairEndStart, value);
  }

  public int AnimRepairEndEnd
  {
    get => _animRepairEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animRepairEndEnd, value);
  }

  public int AnimConvertStartStart
  {
    get => _animConvertStartStart;
    set => this.RaiseAndSetIfChanged(ref _animConvertStartStart, value);
  }

  public int AnimConvertStartEnd
  {
    get => _animConvertStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animConvertStartEnd, value);
  }

  public int AnimConvertWorkStart
  {
    get => _animConvertWorkStart;
    set => this.RaiseAndSetIfChanged(ref _animConvertWorkStart, value);
  }

  public int AnimConvertWorkEnd
  {
    get => _animConvertWorkEnd;
    set => this.RaiseAndSetIfChanged(ref _animConvertWorkEnd, value);
  }

  public int AnimConvertEndStart
  {
    get => _animConvertEndStart;
    set => this.RaiseAndSetIfChanged(ref _animConvertEndStart, value);
  }

  public int AnimConvertEndEnd
  {
    get => _animConvertEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animConvertEndEnd, value);
  }

  public int AnimRepaintStartStart
  {
    get => _animRepaintStartStart;
    set => this.RaiseAndSetIfChanged(ref _animRepaintStartStart, value);
  }

  public int AnimRepaintStartEnd
  {
    get => _animRepaintStartEnd;
    set => this.RaiseAndSetIfChanged(ref _animRepaintStartEnd, value);
  }

  public int AnimRepaintWorkStart
  {
    get => _animRepaintWorkStart;
    set => this.RaiseAndSetIfChanged(ref _animRepaintWorkStart, value);
  }

  public int AnimRepaintWorkEnd
  {
    get => _animRepaintWorkEnd;
    set => this.RaiseAndSetIfChanged(ref _animRepaintWorkEnd, value);
  }

  public int AnimRepaintEndStart
  {
    get => _animRepaintEndStart;
    set => this.RaiseAndSetIfChanged(ref _animRepaintEndStart, value);
  }

  public int AnimRepaintEndEnd
  {
    get => _animRepaintEndEnd;
    set => this.RaiseAndSetIfChanged(ref _animRepaintEndEnd, value);
  }
}