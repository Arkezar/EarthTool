using EarthTool.PAR.Extensions;
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
      NormalWavePack1 = data.ReadParameterString();
      NormalWavePack2 = data.ReadParameterString();
      NormalWavePack3 = data.ReadParameterString();
      NormalWavePack4 = data.ReadParameterString();
      LoopedWavePack1 = data.ReadParameterString();
      LoopedWavePack2 = data.ReadParameterString();
      LoopedWavePack3 = data.ReadParameterString();
      LoopedWavePack4 = data.ReadParameterString();
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
      bw.WriteParameterString(NormalWavePack1, encoding);
      bw.WriteParameterString(NormalWavePack2, encoding);
      bw.WriteParameterString(NormalWavePack3, encoding);
      bw.WriteParameterString(NormalWavePack4, encoding);
      bw.WriteParameterString(LoopedWavePack1, encoding);
      bw.WriteParameterString(LoopedWavePack2, encoding);
      bw.WriteParameterString(LoopedWavePack3, encoding);
      bw.WriteParameterString(LoopedWavePack4, encoding);

      return output.ToArray();
    }
  }
}
