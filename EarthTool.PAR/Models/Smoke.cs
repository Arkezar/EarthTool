using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Smoke : DestructibleEntity
  {
    public Smoke(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      Mesh1 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      Mesh2 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      Mesh3 = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      SmokeTime1 = BitConverter.ToInt32(data.ReadBytes(4));
      SmokeTime2 = BitConverter.ToInt32(data.ReadBytes(4));
      SmokeTime3 = BitConverter.ToInt32(data.ReadBytes(4));
      SmokeFrequency = BitConverter.ToInt32(data.ReadBytes(4));
      StartingTime = BitConverter.ToInt32(data.ReadBytes(4));
      SmokingTime = BitConverter.ToInt32(data.ReadBytes(4));
      EndingTime = BitConverter.ToInt32(data.ReadBytes(4));
      SmokeUpSpeed = BitConverter.ToInt32(data.ReadBytes(4));
      NewSmokeDistance = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public string Mesh1 { get; }

    public string Mesh2 { get; }

    public string Mesh3 { get; }

    public int SmokeTime1 { get; }

    public int SmokeTime2 { get; }

    public int SmokeTime3 { get; }

    public int SmokeFrequency { get; }

    public int StartingTime { get; }

    public int SmokingTime { get; }

    public int EndingTime { get; }

    public int SmokeUpSpeed { get; }

    public int NewSmokeDistance { get; }
  }
}
