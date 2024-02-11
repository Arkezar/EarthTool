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
    public Sapper(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
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
