using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class TypedEntity : Entity
  {
    public TypedEntity()
    {
    }

    public TypedEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type)
      : base(name, requiredResearch, type)
    {
    }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => IsStringMember(() => ClassId);
      set => _ = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)ClassId);

      return output.ToArray();
    }
  }
}
