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
    public Weapon(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      RangeOfSight = BitConverter.ToInt32(data.ReadBytes(4));
      PlugType = BitConverter.ToInt32(data.ReadBytes(4));
      SlotType = BitConverter.ToInt32(data.ReadBytes(4));
      MaxAlphaPerTick = BitConverter.ToInt32(data.ReadBytes(4));
      MaxBetaPerTick = BitConverter.ToInt32(data.ReadBytes(4));
      AlphaMargin = BitConverter.ToInt32(data.ReadBytes(4));
      BetaMargin = BitConverter.ToInt32(data.ReadBytes(4));
      BarrelBetaType = BitConverter.ToInt32(data.ReadBytes(4));
      BarrelBetaAngle = BitConverter.ToInt32(data.ReadBytes(4));
      BarrelCount = BitConverter.ToInt32(data.ReadBytes(4));
      AmmoId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      AmmoType = BitConverter.ToInt32(data.ReadBytes(4));
      TargetType = BitConverter.ToInt32(data.ReadBytes(4));
      RangeOfFire = BitConverter.ToInt32(data.ReadBytes(4));
      PlusDamage = BitConverter.ToInt32(data.ReadBytes(4));
      FireType = BitConverter.ToInt32(data.ReadBytes(4));
      ShootDelay = BitConverter.ToInt32(data.ReadBytes(4));
      NeedExternal = BitConverter.ToInt32(data.ReadBytes(4));
      ReloadDelay = BitConverter.ToInt32(data.ReadBytes(4));
      MaxAmmo = BitConverter.ToInt32(data.ReadBytes(4));
      BarrelExplosionId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
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
