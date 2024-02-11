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
    public SoundPack(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type)
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
  }
}
