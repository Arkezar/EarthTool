using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Missile : DestructibleEntity
  {
    public Missile()
    {
    }

    public Missile(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      Type = (MissileType)ReadInteger(data);
      RocketType = (RocketType)ReadInteger(data);
      MissileSize = ReadInteger(data);
      RocketDummyId = ReadStringRef(data);
      IsAntiRocketTarget = ReadInteger(data);
      Speed = ReadInteger(data);
      TimeOfShoot = ReadInteger(data);
      PlusRangeOfFire = ReadInteger(data);
      HitType = (HitType)ReadInteger(data);
      HitRange = ReadInteger(data);
      TypeOfDamage = (DamageFlags)ReadInteger(data);
      Damage = ReadInteger(data);
      ExplosionId = ReadStringRef(data);
    }

    public MissileType Type { get; set; }

    public RocketType RocketType { get; set; }

    public int MissileSize { get; set; }

    public string RocketDummyId { get; set; }

    public int IsAntiRocketTarget { get; set; }

    public int Speed { get; set; }

    public int TimeOfShoot { get; set; }

    public int PlusRangeOfFire { get; set; }

    public HitType HitType { get; set; }

    public int HitRange { get; set; }

    public DamageFlags TypeOfDamage { get; set; }

    public int Damage { get; set; }

    public string ExplosionId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => Type,
          () => RocketType,
          () => MissileSize,
          () => RocketDummyId,
          () => ReferenceMarker,
          () => IsAntiRocketTarget,
          () => Speed,
          () => TimeOfShoot,
          () => PlusRangeOfFire,
          () => HitType,
          () => HitRange,
          () => TypeOfDamage,
          () => Damage,
          () => ExplosionId,
          () => ReferenceMarker
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)Type);
      bw.Write((int)RocketType);
      bw.Write(MissileSize);
      WriteStringRef(bw, RocketDummyId, encoding);
      bw.Write(IsAntiRocketTarget);
      bw.Write(Speed);
      bw.Write(TimeOfShoot);
      bw.Write(PlusRangeOfFire);
      bw.Write((int)HitType);
      bw.Write(HitRange);
      bw.Write((int)TypeOfDamage);
      bw.Write(Damage);
      WriteStringRef(bw, ExplosionId, encoding);

      return output.ToArray();
    }
  }
}
