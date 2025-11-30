using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class PassiveEntity : DestructibleEntity
  {
    public PassiveEntity()
    {
    }

    public PassiveEntity(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      PassiveMask = (PassiveMask)data.ReadInteger();
      WallCopulaId = data.ReadParameterStringRef();
    }

    public PassiveMask PassiveMask { get; set; }

    public string WallCopulaId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => (int)PassiveMask,
        () => WallCopulaId,
        () => ReferenceMarker
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();
      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)PassiveMask);
      bw.WriteParameterStringRef(WallCopulaId, encoding);
      return output.ToArray();
    }
  }
}
