using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class SoundPack : Entity
  {
    public SoundPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, fieldTypes)
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

    public string NormalWavePack1 { get; }

    public string NormalWavePack2 { get; }

    public string NormalWavePack3 { get; }

    public string NormalWavePack4 { get; }

    public string LoopedWavePack1 { get; }

    public string LoopedWavePack2 { get; }

    public string LoopedWavePack3 { get; }

    public string LoopedWavePack4 { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
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
