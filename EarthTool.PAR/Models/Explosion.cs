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
    public Explosion(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      ExplosionTicks = BitConverter.ToInt32(data.ReadBytes(4));
      ExplosionFlags = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int ExplosionTicks { get; }

    public int ExplosionFlags { get; }
  }
}
