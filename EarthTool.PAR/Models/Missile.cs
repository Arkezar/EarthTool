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
      Type = GetInteger(data);
      RocketType = GetInteger(data);
      MissileSize = GetInteger(data);
      RocketDummyId = GetString(data);
      data.ReadBytes(4);
      IsAntiRocketTarget = GetInteger(data);
      Speed = GetInteger(data);
      TimeOfShoot = GetInteger(data);
      PlusRangeOfFire = GetInteger(data);
      HitType = GetInteger(data);
      HitRange = GetInteger(data);
      TypeOfDamage = GetInteger(data);
      Damage = GetInteger(data);
      ExplosionId = GetString(data);
      data.ReadBytes(4);
    }

    public int Type { get; set; }

    public int RocketType { get; set; }

    public int MissileSize { get; set; }

    public string RocketDummyId { get; set; }

    public int IsAntiRocketTarget { get; set; }

    public int Speed { get; set; }

    public int TimeOfShoot { get; set; }

    public int PlusRangeOfFire { get; set; }

    public int HitType { get; set; }

    public int HitRange { get; set; }

    public int TypeOfDamage { get; set; }

    public int Damage { get; set; }

    public string ExplosionId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => Type,
        () => RocketType,
        () => MissileSize,
        () => RocketDummyId,
        () => 1,
        () => IsAntiRocketTarget,
        () => Speed,
        () => TimeOfShoot,
        () => PlusRangeOfFire,
        () => HitType,
        () => HitRange,
        () => TypeOfDamage,
        () => Damage,
        () => ExplosionId,
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
          bw.Write(Type);
          bw.Write(RocketType);
          bw.Write(MissileSize);
          bw.Write(RocketDummyId.Length);
          bw.Write(encoding.GetBytes(RocketDummyId));
          bw.Write(-1);
          bw.Write(IsAntiRocketTarget);
          bw.Write(Speed);
          bw.Write(TimeOfShoot);
          bw.Write(PlusRangeOfFire);
          bw.Write(HitType);
          bw.Write(HitRange);
          bw.Write(TypeOfDamage);
          bw.Write(Damage);
          bw.Write(ExplosionId.Length);
          bw.Write(encoding.GetBytes(ExplosionId));
          bw.Write(-1);
        }

        return output.ToArray();
      }
    }
  }
}
