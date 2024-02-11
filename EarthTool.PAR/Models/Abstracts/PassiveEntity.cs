using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public abstract class PassiveEntity : DestructibleEntity
  {
    public PassiveEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      PassiveMask = GetInteger(data);
      WallCopulaId = GetString(data);
      data.ReadBytes(4);
    }

    public int PassiveMask { get; }

    public string WallCopulaId { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(PassiveMask);
          bw.Write(WallCopulaId.Length);
          bw.Write(encoding.GetBytes(WallCopulaId));
          bw.Write(-1);
        }
        return output.ToArray();
      }
    }
  }
}
