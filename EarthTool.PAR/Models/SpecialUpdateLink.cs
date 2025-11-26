using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class SpecialUpdateLink : TypelessEntity
  {
    public SpecialUpdateLink()
    {
    }

    public SpecialUpdateLink(string name, IEnumerable<int> requiredResearch, BinaryReader data)
      : base(name, requiredResearch)
    {
      Value = GetString(data);
      data.ReadBytes(4);
    }

    public string Value { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => IsStringMember(
        () => Value,
        () => 1
      );
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(Value.Length);
          bw.Write(encoding.GetBytes(Value));
          bw.Write(-1);
        }

        return output.ToArray();
      }
    }
  }
}
