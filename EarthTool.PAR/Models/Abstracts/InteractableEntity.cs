using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public abstract class InteractableEntity : Entity
  {
    protected InteractableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type)
    {
      Mesh = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      ShadowType = BitConverter.ToInt32(data.ReadBytes(4));
      ViewParamsIndex = BitConverter.ToInt32(data.ReadBytes(4));
      Cost = BitConverter.ToInt32(data.ReadBytes(4));
      TimeOfBuild = BitConverter.ToInt32(data.ReadBytes(4));
      SoundPackId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      SmokeId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      KillExplosionId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      DestructedId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
    }

    public string Mesh { get; }

    public int ShadowType { get; }

    public int ViewParamsIndex { get; }

    public int Cost { get; }

    public int TimeOfBuild { get; }

    public string SoundPackId { get; }

    public string SmokeId { get; }

    public string KillExplosionId { get; }

    public string DestructedId { get; }
  }
}
