using EarthTool.PAR.Enums;
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
      MinesLookRange = GetInteger(data);
      MineId = GetString(data);
      data.ReadBytes(4);
      MaxMinesCount = GetInteger(data);
      AnimDownStart = GetInteger(data);
      AnimDownEnd = GetInteger(data);
      AnimUpStart = GetInteger(data);
      AnimUpEnd = GetInteger(data);
      PutMineSmokeId = GetString(data);
      data.ReadBytes(4);
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
        () => 1,
        () => MaxMinesCount,
        () => AnimDownStart,
        () => AnimDownEnd,
        () => AnimUpStart,
        () => AnimUpEnd,
        () => PutMineSmokeId,
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
          bw.Write(MinesLookRange);
          bw.Write(MineId.Length);
          bw.Write(encoding.GetBytes(MineId));
          bw.Write(-1);
          bw.Write(MaxMinesCount);
          bw.Write(AnimDownStart);
          bw.Write(AnimDownEnd);
          bw.Write(AnimUpStart);
          bw.Write(AnimUpEnd);
          bw.Write(PutMineSmokeId.Length);
          bw.Write(encoding.GetBytes(PutMineSmokeId));
          bw.Write(-1);
        }

        return output.ToArray();
      }
    }
  }
}
