using EarthTool.PAR.GUI.ViewModels.Details.Abstracts;
using EarthTool.PAR.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels.Details;

public class MissileViewModel : DestructibleEntityViewModel
{
  private int _type;
  private int _rocketType;
  private int _missileSize;
  private string _rocketDummyId;
  private int _isAntiRocketTarget;
  private int _speed;
  private int _timeOfShoot;
  private int _plusRangeOfFire;
  private int _hitType;
  private int _hitRange;
  private int _typeOfDamage;
  private int _damage;
  private string _explosionId;

  public MissileViewModel(Missile missile)
    : base(missile)
  {
    _type = missile.Type;
    _rocketType = missile.RocketType;
    _missileSize = missile.MissileSize;
    _rocketDummyId = missile.RocketDummyId;
    _isAntiRocketTarget = missile.IsAntiRocketTarget;
    _speed = missile.Speed;
    _timeOfShoot = missile.TimeOfShoot;
    _plusRangeOfFire = missile.PlusRangeOfFire;
    _hitType = missile.HitType;
    _hitRange = missile.HitRange;
    _typeOfDamage = missile.TypeOfDamage;
    _damage = missile.Damage;
    _explosionId = missile.ExplosionId;
  }

  public int Type
  {
    get => _type;
    set => this.RaiseAndSetIfChanged(ref _type, value);
  }

  public int RocketType
  {
    get => _rocketType;
    set => this.RaiseAndSetIfChanged(ref _rocketType, value);
  }

  public int MissileSize
  {
    get => _missileSize;
    set => this.RaiseAndSetIfChanged(ref _missileSize, value);
  }

  public string RocketDummyId
  {
    get => _rocketDummyId;
    set => this.RaiseAndSetIfChanged(ref _rocketDummyId, value);
  }

  public int IsAntiRocketTarget
  {
    get => _isAntiRocketTarget;
    set => this.RaiseAndSetIfChanged(ref _isAntiRocketTarget, value);
  }

  public int Speed
  {
    get => _speed;
    set => this.RaiseAndSetIfChanged(ref _speed, value);
  }

  public int TimeOfShoot
  {
    get => _timeOfShoot;
    set => this.RaiseAndSetIfChanged(ref _timeOfShoot, value);
  }

  public int PlusRangeOfFire
  {
    get => _plusRangeOfFire;
    set => this.RaiseAndSetIfChanged(ref _plusRangeOfFire, value);
  }

  public int HitType
  {
    get => _hitType;
    set => this.RaiseAndSetIfChanged(ref _hitType, value);
  }

  public int HitRange
  {
    get => _hitRange;
    set => this.RaiseAndSetIfChanged(ref _hitRange, value);
  }

  public int TypeOfDamage
  {
    get => _typeOfDamage;
    set => this.RaiseAndSetIfChanged(ref _typeOfDamage, value);
  }

  public int Damage
  {
    get => _damage;
    set => this.RaiseAndSetIfChanged(ref _damage, value);
  }

  public string ExplosionId
  {
    get => _explosionId;
    set => this.RaiseAndSetIfChanged(ref _explosionId, value);
  }
}