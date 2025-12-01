using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
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
      Mesh = data.ReadParameterString();
      ShadowType = (ShadowType)data.ReadInteger();
      ViewParamsIndex = data.ReadInteger();
      Cost = data.ReadInteger();
      TimeOfBuild = data.ReadInteger();
      SoundPackId = data.ReadParameterStringRef();
      SmokeId = data.ReadParameterStringRef();
      KillExplosionId = data.ReadParameterStringRef();
      DestructedId = data.ReadParameterStringRef();
    }

    public string Mesh { get; set; }

    public ShadowType ShadowType { get; set; }

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
        () => (int)ShadowType,
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
      using var output = new MemoryStream();
      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.WriteParameterString(Mesh, encoding);
      bw.Write((int)ShadowType);
      bw.Write(ViewParamsIndex);
      bw.Write(Cost);
      bw.Write(TimeOfBuild);
      bw.WriteParameterStringRef(SoundPackId, encoding);
      bw.WriteParameterStringRef(SmokeId, encoding);
      bw.WriteParameterStringRef(KillExplosionId, encoding);
      bw.WriteParameterStringRef(DestructedId, encoding);
      return output.ToArray();
    }
  }
}
