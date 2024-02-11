using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Artifact : PassiveEntity
  {
    public Artifact(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      ArtefactMask = GetInteger(data);
      ArtefactParam = GetInteger(data);
      RespawnTime = GetInteger(data);
    }

    public int ArtefactMask { get; }

    public int ArtefactParam { get; }

    public int RespawnTime { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(ArtefactMask);
          bw.Write(ArtefactParam);
          bw.Write(RespawnTime);
        }
        return output.ToArray();
      }
    }
  }
}
