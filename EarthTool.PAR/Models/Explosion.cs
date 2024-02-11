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
    public Explosion(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      ExplosionTicks = GetInteger(data);
      ExplosionFlags = GetInteger(data);
    }

    public int ExplosionTicks { get; }

    public int ExplosionFlags { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(ExplosionTicks);
          bw.Write(ExplosionFlags);
        }
        return output.ToArray();
      }
    }
  }
}
