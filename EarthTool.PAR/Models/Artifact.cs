using EarthTool.PAR.Enums;
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
      ArtefactMask = GetInteger(data);
      ArtefactParam = GetInteger(data);
      RespawnTime = GetInteger(data);
    }

    public int ArtefactMask { get; set; }

    public int ArtefactParam { get; set; }

    public int RespawnTime { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => ArtefactMask,
        () => ArtefactParam,
        () => RespawnTime
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
          bw.Write(ArtefactMask);
          bw.Write(ArtefactParam);
          bw.Write(RespawnTime);
        }

        return output.ToArray();
      }
    }
  }
}