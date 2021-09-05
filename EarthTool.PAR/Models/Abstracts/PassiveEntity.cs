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
    public PassiveEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      PassiveMask = BitConverter.ToInt32(data.ReadBytes(4));
      WallCopulaId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
    }

    public int PassiveMask { get; }

    public string WallCopulaId { get; }
  }
}
