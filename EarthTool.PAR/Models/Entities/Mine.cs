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
  public class Mine : DestructibleEntity
  {
    public Mine()
    {
    }

    public Mine(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      MineSize = data.ReadInteger();
      MineTypeOfDamage = data.ReadInteger();
      MineDamage = data.ReadInteger();
    }

    public int MineSize { get; set; }

    public int MineTypeOfDamage { get; set; }

    public int MineDamage { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => MineSize,
        () => MineTypeOfDamage,
        () => MineDamage
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(MineSize);
      bw.Write(MineTypeOfDamage);
      bw.Write(MineDamage);

      return output.ToArray();
    }
  }
}
