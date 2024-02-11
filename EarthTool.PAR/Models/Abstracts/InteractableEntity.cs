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
    protected InteractableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, fieldTypes)
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
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(Mesh.Length);
          bw.Write(encoding.GetBytes(Mesh));
          bw.Write(ShadowType);
          bw.Write(ViewParamsIndex);
          bw.Write(Cost);
          bw.Write(TimeOfBuild);
          bw.Write(SoundPackId.Length);
          bw.Write(encoding.GetBytes(SoundPackId));
          bw.Write(-1);
          bw.Write(SmokeId.Length);
          bw.Write(encoding.GetBytes(SmokeId));
          bw.Write(-1);
          bw.Write(KillExplosionId.Length);
          bw.Write(encoding.GetBytes(KillExplosionId));
          bw.Write(-1);
          bw.Write(DestructedId.Length);
          bw.Write(encoding.GetBytes(DestructedId));
          bw.Write(-1);
        }
        return output.ToArray();
      }
    }
  }
}
