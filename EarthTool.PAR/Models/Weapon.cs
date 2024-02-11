using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Weapon : InteractableEntity
  {
    public Weapon(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      RangeOfSight = GetInteger(data);
      PlugType = GetInteger(data);
      SlotType = GetInteger(data);
      MaxAlphaPerTick = GetInteger(data);
      MaxBetaPerTick = GetInteger(data);
      AlphaMargin = GetInteger(data);
      BetaMargin = GetInteger(data);
      BarrelBetaType = GetInteger(data);
      BarrelBetaAngle = GetInteger(data);
      BarrelCount = GetInteger(data);
      AmmoId = GetString(data);
      data.ReadBytes(4);
      AmmoType = GetInteger(data);
      TargetType = GetInteger(data);
      RangeOfFire = GetInteger(data);
      PlusDamage = GetInteger(data);
      FireType = GetInteger(data);
      ShootDelay = GetInteger(data);
      NeedExternal = GetInteger(data);
      ReloadDelay = GetInteger(data);
      MaxAmmo = GetInteger(data);
      BarrelExplosionId = GetString(data);
      data.ReadBytes(4);
    }

    public int RangeOfSight { get; }

    public int PlugType { get; }

    public int SlotType { get; }

    public int MaxAlphaPerTick { get; }

    public int MaxBetaPerTick { get; }

    public int AlphaMargin { get; }

    public int BetaMargin { get; }

    public int BarrelBetaType { get; }

    public int BarrelBetaAngle { get; }

    public int BarrelCount { get; }

    public string AmmoId { get; }

    public int AmmoType { get; }

    public int TargetType { get; }

    public int RangeOfFire { get; }

    public int PlusDamage { get; }

    public int FireType { get; }

    public int ShootDelay { get; }

    public int NeedExternal { get; }

    public int ReloadDelay { get; }

    public int MaxAmmo { get; }

    public string BarrelExplosionId { get; }
  }
}
