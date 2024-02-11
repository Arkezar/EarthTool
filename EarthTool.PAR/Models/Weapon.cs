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
    public Weapon(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(RangeOfSight);
          bw.Write(PlugType);
          bw.Write(SlotType);
          bw.Write(MaxAlphaPerTick);
          bw.Write(MaxBetaPerTick);
          bw.Write(AlphaMargin);
          bw.Write(BetaMargin);
          bw.Write(BarrelBetaType);
          bw.Write(BarrelBetaAngle);
          bw.Write(BarrelCount);
          bw.Write(AmmoId.Length);
          bw.Write(encoding.GetBytes(AmmoId));
          bw.Write(-1);
          bw.Write(AmmoType);
          bw.Write(TargetType);
          bw.Write(RangeOfFire);
          bw.Write(PlusDamage);
          bw.Write(FireType);
          bw.Write(ShootDelay);
          bw.Write(NeedExternal);
          bw.Write(ReloadDelay);
          bw.Write(MaxAmmo);
          bw.Write(BarrelExplosionId.Length);
          bw.Write(encoding.GetBytes(BarrelExplosionId));
          bw.Write(-1);
        }
        return output.ToArray();
      }
    }
  }
}
