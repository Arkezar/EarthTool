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
      Value = ReadStringRef(data);
    }

    public string Value { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => IsStringMember(
        () => Value,
        () => ReferenceMarker
      );
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      WriteStringRef(bw, Value, encoding);

      return output.ToArray();
    }
  }
}
