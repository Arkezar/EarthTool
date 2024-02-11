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
    public Smoke(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      Mesh1 = GetString(data);
      Mesh2 = GetString(data);
      Mesh3 = GetString(data);
      SmokeTime1 = GetInteger(data);
      SmokeTime2 = GetInteger(data);
      SmokeTime3 = GetInteger(data);
      SmokeFrequency = GetInteger(data);
      StartingTime = GetInteger(data);
      SmokingTime = GetInteger(data);
      EndingTime = GetInteger(data);
      SmokeUpSpeed = GetInteger(data);
      NewSmokeDistance = GetInteger(data);
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
