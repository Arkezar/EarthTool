using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Missile : DestructibleEntity
  {
    public Missile(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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

    public int Type { get; }

    public int RocketType { get; }

    public int MissileSize { get; }

    public string RocketDummyId { get; }

    public int IsAntiRocketTarget { get; }

    public int Speed { get; }

    public int TimeOfShoot { get; }

    public int PlusRangeOfFire { get; }

    public int HitType { get; }

    public int HitRange { get; }

    public int TypeOfDamage { get; }

    public int Damage { get; }

    public string ExplosionId { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
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
