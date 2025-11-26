using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Weapon : InteractableEntity
  {
    public Weapon()
    {
    }

    public Weapon(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      RangeOfSight = GetInteger(data);
      PlugType = (SlotType)GetUnsignedInteger(data);
      SlotType = (SlotType)GetUnsignedInteger(data);
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

    public int RangeOfSight { get; set; }

    public SlotType PlugType { get; set; }

    public SlotType SlotType { get; set; }

    public int MaxAlphaPerTick { get; set; }

    public int MaxBetaPerTick { get; set; }

    public int AlphaMargin { get; set; }

    public int BetaMargin { get; set; }

    public int BarrelBetaType { get; set; }

    public int BarrelBetaAngle { get; set; }

    public int BarrelCount { get; set; }

    public string AmmoId { get; set; }

    public int AmmoType { get; set; }

    public int TargetType { get; set; }

    public int RangeOfFire { get; set; }

    public int PlusDamage { get; set; }

    public int FireType { get; set; }

    public int ShootDelay { get; set; }

    public int NeedExternal { get; set; }

    public int ReloadDelay { get; set; }

    public int MaxAmmo { get; set; }

    public string BarrelExplosionId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => RangeOfSight,
          () => PlugType,
          () => SlotType,
          () => MaxAlphaPerTick,
          () => MaxBetaPerTick,
          () => AlphaMargin,
          () => BetaMargin,
          () => BarrelBetaType,
          () => BarrelBetaAngle,
          () => BarrelCount,
          () => AmmoId,
          () => 1,
          () => AmmoType,
          () => TargetType,
          () => RangeOfFire,
          () => PlusDamage,
          () => FireType,
          () => ShootDelay,
          () => NeedExternal,
          () => ReloadDelay,
          () => MaxAmmo,
          () => BarrelExplosionId,
          () => 1
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(RangeOfSight);
          bw.Write((uint)PlugType);
          bw.Write((uint)SlotType);
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
