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
    public Artifact(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      ArtefactMask = BitConverter.ToInt32(data.ReadBytes(4));
      ArtefactParam = BitConverter.ToInt32(data.ReadBytes(4));
      RespawnTime = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int ArtefactMask { get; }

    public int ArtefactParam { get; }

    public int RespawnTime { get; }
  }
}
