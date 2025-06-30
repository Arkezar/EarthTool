using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class OmnidirectionalEquipmentViewModel : EquipmentViewModel
{
  private int _lookRoundTypeMask;
  private int _lookRoundRange;
  private int _turnSpeed;
  private int _bannerAddExperienceLevel;
  private int _regenerationHPMultiple;
  private int _shieldReloadAdd;

  public OmnidirectionalEquipmentViewModel(OmnidirectionalEquipment omnidirectionalEquipment)
    : base(omnidirectionalEquipment)
  {
    _lookRoundTypeMask = omnidirectionalEquipment.LookRoundTypeMask;
    _lookRoundRange = omnidirectionalEquipment.LookRoundRange;
    _turnSpeed = omnidirectionalEquipment.TurnSpeed;
    _bannerAddExperienceLevel = omnidirectionalEquipment.BannerAddExperienceLevel;
    _regenerationHPMultiple = omnidirectionalEquipment.RegenerationHPMultiple;
    _shieldReloadAdd = omnidirectionalEquipment.ShieldReloadAdd;
  }

  public int LookRoundTypeMask
  {
    get => _lookRoundTypeMask;
    set => this.RaiseAndSetIfChanged(ref _lookRoundTypeMask, value);
  }

  public int LookRoundRange
  {
    get => _lookRoundRange;
    set => this.RaiseAndSetIfChanged(ref _lookRoundRange, value);
  }

  public int TurnSpeed
  {
    get => _turnSpeed;
    set => this.RaiseAndSetIfChanged(ref _turnSpeed, value);
  }

  public int BannerAddExperienceLevel
  {
    get => _bannerAddExperienceLevel;
    set => this.RaiseAndSetIfChanged(ref _bannerAddExperienceLevel, value);
  }

  public int RegenerationHPMultiple
  {
    get => _regenerationHPMultiple;
    set => this.RaiseAndSetIfChanged(ref _regenerationHPMultiple, value);
  }

  public int ShieldReloadAdd
  {
    get => _shieldReloadAdd;
    set => this.RaiseAndSetIfChanged(ref _shieldReloadAdd, value);
  }
}