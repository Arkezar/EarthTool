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
    public Artifact(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      ArtefactMask = GetInteger(data);
      ArtefactParam = GetInteger(data);
      RespawnTime = GetInteger(data);
    }

    public int ArtefactMask { get; }

    public int ArtefactParam { get; }

    public int RespawnTime { get; }
  }
}
