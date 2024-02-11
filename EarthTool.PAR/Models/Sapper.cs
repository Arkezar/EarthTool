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
    public Sapper(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
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
