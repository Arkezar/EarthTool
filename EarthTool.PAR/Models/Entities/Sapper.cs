using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Sapper : Vehicle
  {
    public Sapper()
    {
    }

    public Sapper(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      MinesLookRange = data.ReadInteger();
      MineId = data.ReadParameterStringRef();
      MaxMinesCount = data.ReadInteger();
      AnimDownStart = data.ReadInteger();
      AnimDownEnd = data.ReadInteger();
      AnimUpStart = data.ReadInteger();
      AnimUpEnd = data.ReadInteger();
      PutMineSmokeId = data.ReadParameterStringRef();
    }

    public int MinesLookRange { get; set; }

    public string MineId { get; set; }

    public int MaxMinesCount { get; set; }

    public int AnimDownStart { get; set; }

    public int AnimDownEnd { get; set; }

    public int AnimUpStart { get; set; }

    public int AnimUpEnd { get; set; }

    public string PutMineSmokeId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => MinesLookRange,
        () => MineId,
        () => ReferenceMarker,
        () => MaxMinesCount,
        () => AnimDownStart,
        () => AnimDownEnd,
        () => AnimUpStart,
        () => AnimUpEnd,
        () => PutMineSmokeId,
        () => ReferenceMarker
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write(MinesLookRange);
      bw.WriteParameterStringRef(MineId, encoding);
      bw.Write(MaxMinesCount);
      bw.Write(AnimDownStart);
      bw.Write(AnimDownEnd);
      bw.Write(AnimUpStart);
      bw.Write(AnimUpEnd);
      bw.WriteParameterStringRef(PutMineSmokeId, encoding);

      return output.ToArray();
    }
  }
}
