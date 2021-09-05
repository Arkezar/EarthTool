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
    public SoundPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type)
    {
      NormalWavePack1 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      NormalWavePack2 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      NormalWavePack3 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      NormalWavePack4 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LoopedWavePack1 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LoopedWavePack2 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LoopedWavePack3 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      LoopedWavePack4 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
    }

    public string NormalWavePack1 { get; }

    public string NormalWavePack2 { get; }

    public string NormalWavePack3 { get; }

    public string NormalWavePack4 { get; }

    public string LoopedWavePack1 { get; }

    public string LoopedWavePack2 { get; }

    public string LoopedWavePack3 { get; }

    public string LoopedWavePack4 { get; }
  }
}
