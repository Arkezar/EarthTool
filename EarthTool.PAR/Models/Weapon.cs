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
      RangeOfSight = ReadInteger(data);
      PlugType = (ConnectorType)ReadUnsignedInteger(data);
      SlotType = (ConnectorType)ReadUnsignedInteger(data);
      MaxAlphaPerTick = ReadInteger(data);
      MaxBetaPerTick = ReadInteger(data);
      AlphaMargin = ReadInteger(data);
      BetaMargin = ReadInteger(data);
      BarrelBetaType = (BarrelBetaType)ReadInteger(data);
      BarrelBetaAngle = ReadInteger(data);
      BarrelCount = ReadInteger(data);
      AmmoId = ReadStringRef(data);
      AmmoType = ReadInteger(data);
      TargetType = (TargetType)ReadInteger(data);
      RangeOfFire = ReadInteger(data);
      PlusDamage = ReadInteger(data);
      FireType = (WeaponFireType)ReadInteger(data);
      ShootDelay = ReadInteger(data);
      NeedExternal = ReadInteger(data);
      ReloadDelay = ReadInteger(data);
      MaxAmmo = ReadInteger(data);
      BarrelExplosionId = ReadStringRef(data);
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
      WriteStringRef(bw, AmmoId, encoding);
      bw.Write(AmmoType);
      bw.Write((int)TargetType);
      bw.Write(RangeOfFire);
      bw.Write(PlusDamage);
      bw.Write((int)FireType);
      bw.Write(ShootDelay);
      bw.Write(NeedExternal);
      bw.Write(ReloadDelay);
      bw.Write(MaxAmmo);
      WriteStringRef(bw, BarrelExplosionId, encoding);

      return output.ToArray();
    }
  }
}
