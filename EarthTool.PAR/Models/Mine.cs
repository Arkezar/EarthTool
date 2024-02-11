using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Mine : DestructibleEntity
  {
    public Mine(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      MineSize = GetInteger(data);
      MineTypeOfDamage = GetInteger(data);
      MineDamage = GetInteger(data);
    }

    public int MineSize { get; }

    public int MineTypeOfDamage { get; }

    public int MineDamage { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(MineSize);
          bw.Write(MineTypeOfDamage);
          bw.Write(MineDamage);
        }
        return output.ToArray();
      }
    }
  }
}
