using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Sapper : Vehicle
  {
    public Sapper(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      MinesLookRange = BitConverter.ToInt32(data.ReadBytes(4));
      MineId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      MaxMinesCount = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDownStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDownEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimUpStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimUpEnd = BitConverter.ToInt32(data.ReadBytes(4));
      PutMineSmokeId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
    }

    public int MinesLookRange { get; }

    public string MineId { get; }

    public int MaxMinesCount { get; }

    public int AnimDownStart { get; }

    public int AnimDownEnd { get; }

    public int AnimUpStart { get; }

    public int AnimUpEnd { get; }

    public string PutMineSmokeId { get; }
  }
}
