using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Artifact : PassiveEntity
  {
    public Artifact()
    {
    }

    public Artifact(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      ArtifactMask = (ArtifactType)data.ReadInteger();
      ArtifactParam = data.ReadInteger();
      RespawnTime = data.ReadInteger();
    }

    public ArtifactType ArtifactMask { get; set; }

    public int ArtifactParam { get; set; }

    public int RespawnTime { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => ArtifactMask,
          () => ArtifactParam,
          () => RespawnTime
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)ArtifactMask);
      bw.Write(ArtifactParam);
      bw.Write(RespawnTime);

      return output.ToArray();
    }
  }
}
