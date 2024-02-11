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
    protected InteractableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type)
    {
      Mesh = GetString(data);
      ShadowType = GetInteger(data);
      ViewParamsIndex = GetInteger(data);
      Cost = GetInteger(data);
      TimeOfBuild = GetInteger(data);
      SoundPackId = GetString(data);
      data.ReadBytes(4);
      SmokeId = GetString(data);
      data.ReadBytes(4);
      KillExplosionId = GetString(data);
      data.ReadBytes(4);
      DestructedId = GetString(data);
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
