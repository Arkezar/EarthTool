using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class InteractableEntity : TypedEntity
  {
    public InteractableEntity()
    {
    }

    protected InteractableEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type,
      BinaryReader data) : base(name, requiredResearch, type)
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

    public string Mesh { get; set; }

    public int ShadowType { get; set; }

    public int ViewParamsIndex { get; set; }

    public int Cost { get; set; }

    public int TimeOfBuild { get; set; }

    public string SoundPackId { get; set; }

    public string SmokeId { get; set; }

    public string KillExplosionId { get; set; }

    public string DestructedId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => Mesh,
        () => ShadowType,
        () => ViewParamsIndex,
        () => Cost,
        () => TimeOfBuild,
        () => SoundPackId,
        () => 0,
        () => SmokeId,
        () => 0,
        () => KillExplosionId,
        () => 0,
        () => DestructedId,
        () => 0
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
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