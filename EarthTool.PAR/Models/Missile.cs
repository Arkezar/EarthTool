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
    public Missile(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      Type = BitConverter.ToInt32(data.ReadBytes(4));
      RocketType = BitConverter.ToInt32(data.ReadBytes(4));
      MissileSize = BitConverter.ToInt32(data.ReadBytes(4));
      RocketDummyId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      IsAntiRocketTarget = BitConverter.ToInt32(data.ReadBytes(4));
      Speed = BitConverter.ToInt32(data.ReadBytes(4));
      TimeOfShoot = BitConverter.ToInt32(data.ReadBytes(4));
      PlusRangeOfFire = BitConverter.ToInt32(data.ReadBytes(4));
      HitType = BitConverter.ToInt32(data.ReadBytes(4));
      HitRange = BitConverter.ToInt32(data.ReadBytes(4));
      TypeOfDamage = BitConverter.ToInt32(data.ReadBytes(4));
      Damage = BitConverter.ToInt32(data.ReadBytes(4));
      ExplosionId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
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
  }
}
