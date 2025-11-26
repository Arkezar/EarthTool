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
      NormalWavePack1 = GetString(data);
      NormalWavePack2 = GetString(data);
      NormalWavePack3 = GetString(data);
      NormalWavePack4 = GetString(data);
      LoopedWavePack1 = GetString(data);
      LoopedWavePack2 = GetString(data);
      LoopedWavePack3 = GetString(data);
      LoopedWavePack4 = GetString(data);
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
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(NormalWavePack1.Length);
          bw.Write(encoding.GetBytes(NormalWavePack1));
          bw.Write(NormalWavePack2.Length);
          bw.Write(encoding.GetBytes(NormalWavePack2));
          bw.Write(NormalWavePack3.Length);
          bw.Write(encoding.GetBytes(NormalWavePack3));
          bw.Write(NormalWavePack4.Length);
          bw.Write(encoding.GetBytes(NormalWavePack4));
          bw.Write(LoopedWavePack1.Length);
          bw.Write(encoding.GetBytes(LoopedWavePack1));
          bw.Write(LoopedWavePack2.Length);
          bw.Write(encoding.GetBytes(LoopedWavePack2));
          bw.Write(LoopedWavePack3.Length);
          bw.Write(encoding.GetBytes(LoopedWavePack3));
          bw.Write(LoopedWavePack4.Length);
          bw.Write(encoding.GetBytes(LoopedWavePack4));
        }

        return output.ToArray();
      }
    }
  }
}
