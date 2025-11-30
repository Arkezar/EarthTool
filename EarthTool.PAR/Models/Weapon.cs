using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
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
      RangeOfSight = data.ReadInteger();
      PlugType = (ConnectorType)data.ReadUnsignedInteger();
      SlotType = (ConnectorType)data.ReadUnsignedInteger();
      MaxAlphaPerTick = data.ReadInteger();
      MaxBetaPerTick = data.ReadInteger();
      AlphaMargin = data.ReadInteger();
      BetaMargin = data.ReadInteger();
      BarrelBetaType = (BarrelBetaType)data.ReadInteger();
      BarrelBetaAngle = data.ReadInteger();
      BarrelCount = data.ReadInteger();
      AmmoId = data.ReadParameterStringRef();
      AmmoType = data.ReadInteger();
      TargetType = (TargetType)data.ReadInteger();
      RangeOfFire = data.ReadInteger();
      PlusDamage = data.ReadInteger();
      FireType = (WeaponFireType)data.ReadInteger();
      ShootDelay = data.ReadInteger();
      NeedExternal = data.ReadInteger();
      ReloadDelay = data.ReadInteger();
      MaxAmmo = data.ReadInteger();
      BarrelExplosionId = data.ReadParameterStringRef();
    }

    public int RangeOfSight { get; set; }

    public ConnectorType PlugType { get; set; }

    public ConnectorType SlotType { get; set; }

    public int MaxAlphaPerTick { get; set; }

    public int MaxBetaPerTick { get; set; }

    public int AlphaMargin { get; set; }

    public int BetaMargin { get; set; }

    public BarrelBetaType BarrelBetaType { get; set; }

    public int BarrelBetaAngle { get; set; }

    public int BarrelCount { get; set; }

    public string AmmoId { get; set; }

    public int AmmoType { get; set; }

    public TargetType TargetType { get; set; }

    public int RangeOfFire { get; set; }

    public int PlusDamage { get; set; }

    public WeaponFireType FireType { get; set; }

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
          () => ReferenceMarker,
          () => AmmoType,
          () => (int)TargetType,
          () => RangeOfFire,
          () => PlusDamage,
          () => FireType,
          () => ShootDelay,
          () => NeedExternal,
          () => ReloadDelay,
          () => MaxAmmo,
          () => BarrelExplosionId,
          () => ReferenceMarker
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(RangeOfSight);
      bw.Write((uint)PlugType);
      bw.Write((uint)SlotType);
      bw.Write(MaxAlphaPerTick);
      bw.Write(MaxBetaPerTick);
      bw.Write(AlphaMargin);
      bw.Write(BetaMargin);
      bw.Write((int)BarrelBetaType);
      bw.Write(BarrelBetaAngle);
      bw.Write(BarrelCount);
      bw.WriteParameterStringRef(AmmoId, encoding);
      bw.Write(AmmoType);
      bw.Write((int)TargetType);
      bw.Write(RangeOfFire);
      bw.Write(PlusDamage);
      bw.Write((int)FireType);
      bw.Write(ShootDelay);
      bw.Write(NeedExternal);
      bw.Write(ReloadDelay);
      bw.Write(MaxAmmo);
      bw.WriteParameterStringRef(BarrelExplosionId, encoding);

      return output.ToArray();
    }
  }
}
