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
    public PassiveEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      PassiveMask = GetInteger(data);
      WallCopulaId = GetString(data);
      data.ReadBytes(4);
    }

    public int PassiveMask { get; }

    public string WallCopulaId { get; }
  }
}
