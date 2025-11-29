using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class SoundPack : TypelessEntity
  {
    public SoundPack()
    {
    }

    public SoundPack(string name, IEnumerable<int> requiredResearch, BinaryReader data)
      : base(name, requiredResearch)
    {
      NormalWavePack1 = ReadString(data);
      NormalWavePack2 = ReadString(data);
      NormalWavePack3 = ReadString(data);
      NormalWavePack4 = ReadString(data);
      LoopedWavePack1 = ReadString(data);
      LoopedWavePack2 = ReadString(data);
      LoopedWavePack3 = ReadString(data);
      LoopedWavePack4 = ReadString(data);
    }

    public string NormalWavePack1 { get; set; }

    public string NormalWavePack2 { get; set; }

    public string NormalWavePack3 { get; set; }

    public string NormalWavePack4 { get; set; }

    public string LoopedWavePack1 { get; set; }

    public string LoopedWavePack2 { get; set; }

    public string LoopedWavePack3 { get; set; }

    public string LoopedWavePack4 { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => IsStringMember(
        () => NormalWavePack1,
        () => NormalWavePack2,
        () => NormalWavePack3,
        () => NormalWavePack4,
        () => LoopedWavePack1,
        () => LoopedWavePack2,
        () => LoopedWavePack3,
        () => LoopedWavePack4
      );
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      WriteString(bw, NormalWavePack1, encoding);
      WriteString(bw, NormalWavePack2, encoding);
      WriteString(bw, NormalWavePack3, encoding);
      WriteString(bw, NormalWavePack4, encoding);
      WriteString(bw, LoopedWavePack1, encoding);
      WriteString(bw, LoopedWavePack2, encoding);
      WriteString(bw, LoopedWavePack3, encoding);
      WriteString(bw, LoopedWavePack4, encoding);

      return output.ToArray();
    }
  }
}
