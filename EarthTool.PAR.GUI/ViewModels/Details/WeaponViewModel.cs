using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class WeaponViewModel : InteractableEntityViewModel
{
  private int _rangeOfSight;
  private SlotType _plugType;
  private SlotType _slotType;
  private int _maxAlphaPerTick;
  private int _maxBetaPerTick;
  private int _alphaMargin;
  private int _betaMargin;
  private int _barrelBetaType;
  private int _barrelBetaAngle;
  private int _barrelCount;
  private string _ammoId;
  private int _ammoType;
  private int _targetType;
  private int _rangeOfFire;
  private int _plusDamage;
  private int _fireType;
  private int _shootDelay;
  private int _needExternal;
  private int _reloadDelay;
  private int _maxAmmo;
  private string _barrelExplosionId;

  public WeaponViewModel(Weapon weapon)
    : base(weapon)
  {
    _rangeOfSight = weapon.RangeOfSight;
    _plugType = weapon.PlugType;
    _slotType = weapon.SlotType;
    _maxAlphaPerTick = weapon.MaxAlphaPerTick;
    _maxBetaPerTick = weapon.MaxBetaPerTick;
    _alphaMargin = weapon.AlphaMargin;
    _betaMargin = weapon.BetaMargin;
    _barrelBetaType = weapon.BarrelBetaType;
    _barrelBetaAngle = weapon.BarrelBetaAngle;
    _barrelCount = weapon.BarrelCount;
    _ammoId = weapon.AmmoId;
    _ammoType = weapon.AmmoType;
    _targetType = weapon.TargetType;
    _rangeOfFire = weapon.RangeOfFire;
    _plusDamage = weapon.PlusDamage;
    _fireType = weapon.FireType;
    _shootDelay = weapon.ShootDelay;
    _needExternal = weapon.NeedExternal;
    _reloadDelay = weapon.ReloadDelay;
    _maxAmmo = weapon.MaxAmmo;
    _barrelExplosionId = weapon.BarrelExplosionId;
  }

  public int RangeOfSight
  {
    get => _rangeOfSight;
    set => this.RaiseAndSetIfChanged(ref _rangeOfSight, value);
  }

  public SlotType PlugType
  {
    get => _plugType;
    set => this.RaiseAndSetIfChanged(ref _plugType, value);
  }

  public SlotType SlotType
  {
    get => _slotType;
    set => this.RaiseAndSetIfChanged(ref _slotType, value);
  }

  public int MaxAlphaPerTick
  {
    get => _maxAlphaPerTick;
    set => this.RaiseAndSetIfChanged(ref _maxAlphaPerTick, value);
  }

  public int MaxBetaPerTick
  {
    get => _maxBetaPerTick;
    set => this.RaiseAndSetIfChanged(ref _maxBetaPerTick, value);
  }

  public int AlphaMargin
  {
    get => _alphaMargin;
    set => this.RaiseAndSetIfChanged(ref _alphaMargin, value);
  }

  public int BetaMargin
  {
    get => _betaMargin;
    set => this.RaiseAndSetIfChanged(ref _betaMargin, value);
  }

  public int BarrelBetaType
  {
    get => _barrelBetaType;
    set => this.RaiseAndSetIfChanged(ref _barrelBetaType, value);
  }

  public int BarrelBetaAngle
  {
    get => _barrelBetaAngle;
    set => this.RaiseAndSetIfChanged(ref _barrelBetaAngle, value);
  }

  public int BarrelCount
  {
    get => _barrelCount;
    set => this.RaiseAndSetIfChanged(ref _barrelCount, value);
  }

  public string AmmoId
  {
    get => _ammoId;
    set => this.RaiseAndSetIfChanged(ref _ammoId, value);
  }

  public int AmmoType
  {
    get => _ammoType;
    set => this.RaiseAndSetIfChanged(ref _ammoType, value);
  }

  public int TargetType
  {
    get => _targetType;
    set => this.RaiseAndSetIfChanged(ref _targetType, value);
  }

  public int RangeOfFire
  {
    get => _rangeOfFire;
    set => this.RaiseAndSetIfChanged(ref _rangeOfFire, value);
  }

  public int PlusDamage
  {
    get => _plusDamage;
    set => this.RaiseAndSetIfChanged(ref _plusDamage, value);
  }

  public int FireType
  {
    get => _fireType;
    set => this.RaiseAndSetIfChanged(ref _fireType, value);
  }

  public int ShootDelay
  {
    get => _shootDelay;
    set => this.RaiseAndSetIfChanged(ref _shootDelay, value);
  }

  public int NeedExternal
  {
    get => _needExternal;
    set => this.RaiseAndSetIfChanged(ref _needExternal, value);
  }

  public int ReloadDelay
  {
    get => _reloadDelay;
    set => this.RaiseAndSetIfChanged(ref _reloadDelay, value);
  }

  public int MaxAmmo
  {
    get => _maxAmmo;
    set => this.RaiseAndSetIfChanged(ref _maxAmmo, value);
  }

  public string BarrelExplosionId
  {
    get => _barrelExplosionId;
    set => this.RaiseAndSetIfChanged(ref _barrelExplosionId, value);
  }
}