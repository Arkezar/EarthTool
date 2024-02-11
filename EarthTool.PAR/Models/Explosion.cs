using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Explosion : DestructibleEntity
  {
    public Explosion(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      ExplosionTicks = GetInteger(data);
      ExplosionFlags = GetInteger(data);
    }

    public int ExplosionTicks { get; }

    public int ExplosionFlags { get; }
  }
}
