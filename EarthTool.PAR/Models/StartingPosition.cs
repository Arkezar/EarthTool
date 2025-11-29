using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class StartingPosition : EquipableEntity
  {
    public StartingPosition()
    {
    }

    public StartingPosition(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      PositionType = (PositionType)ReadInteger(data);
    }

    public PositionType PositionType { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(() => PositionType
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)PositionType);

      return output.ToArray();
    }
  }
}
