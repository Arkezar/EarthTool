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
      Mesh = ReadString(data);
      ShadowType = (ShadowType)ReadInteger(data);
      ViewParamsIndex = ReadInteger(data);
      Cost = ReadInteger(data);
      TimeOfBuild = ReadInteger(data);
      SoundPackId = ReadStringRef(data);
      SmokeId = ReadStringRef(data);
      KillExplosionId = ReadStringRef(data);
      DestructedId = ReadStringRef(data);
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
      WriteString(bw, Mesh, encoding);
      bw.Write((int)ShadowType);
      bw.Write(ViewParamsIndex);
      bw.Write(Cost);
      bw.Write(TimeOfBuild);
      WriteStringRef(bw, SoundPackId, encoding);
      WriteStringRef(bw, SmokeId, encoding);
      WriteStringRef(bw, KillExplosionId, encoding);
      WriteStringRef(bw, DestructedId, encoding);
      return output.ToArray();
    }
  }
}
