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
    public Missile(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
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
  }
}
