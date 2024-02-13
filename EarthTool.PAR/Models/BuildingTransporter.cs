using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class BuildingTransporter : VerticalTransporter
  {
    public BuildingTransporter()
    {
    }

    public BuildingTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      BuilderLineId = GetString(data);
      data.ReadBytes(4);
    }

    public string BuilderLineId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => BuilderLineId,
        () => 1
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
          bw.Write(BuilderLineId.Length);
          bw.Write(encoding.GetBytes(BuilderLineId));
          bw.Write(-1);
        }

        return output.ToArray();
      }
    }
  }
}