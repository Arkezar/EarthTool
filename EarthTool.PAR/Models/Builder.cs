using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Builder : Vehicle
  {
    public Builder(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      WallId = GetString(data);
      data.ReadBytes(4);
      BridgeId = GetString(data);
      data.ReadBytes(4);
      TunnelNumber = GetInteger(data);
      RoadBuildTime = GetInteger(data);
      FlatBuildTime = GetInteger(data);
      TrenchBuildTime = GetInteger(data);
      TunnelBuildTime = GetInteger(data);
      BuildObjectAnimationAngle = GetInteger(data);
      DigNormalAnimationAngle = GetInteger(data);
      DigLowAnimationAngle = GetInteger(data);
      AnimBuildObjectStartStart = GetInteger(data);
      AnimBuildObjectStartEnd = GetInteger(data);
      AnimBuildObjectWorkStart = GetInteger(data);
      AnimBuildObjectWorkEnd = GetInteger(data);
      AnimBuildObjectEndStart = GetInteger(data);
      AnimBuildObjectEndEnd = GetInteger(data);
      AnimDigNormalStartStart = GetInteger(data);
      AnimDigNormalStartEnd = GetInteger(data);
      AnimDigNormalWorkStart = GetInteger(data);
      AnimDigNormalWorkEnd = GetInteger(data);
      AnimDigNormalEndStart = GetInteger(data);
      AnimDigNormalEndEnd = GetInteger(data);
      AnimDigLowStartStart = GetInteger(data);
      AnimDigLowStartEnd = GetInteger(data);
      AnimDigLowWorkStart = GetInteger(data);
      AnimDigLowWorkEnd = GetInteger(data);
      AnimDigLowEndStart = GetInteger(data);
      AnimDigLowEndEnd = GetInteger(data);
      DigSmokeId = GetString(data);
      data.ReadBytes(4);
    }

    public string WallId { get; }

    public string BridgeId { get; }

    public int TunnelNumber { get; }

    public int RoadBuildTime { get; }

    public int FlatBuildTime { get; }

    public int TrenchBuildTime { get; }

    public int TunnelBuildTime { get; }

    public int BuildObjectAnimationAngle { get; }

    public int DigNormalAnimationAngle { get; }

    public int DigLowAnimationAngle { get; }

    public int AnimBuildObjectStartStart { get; }

    public int AnimBuildObjectStartEnd { get; }

    public int AnimBuildObjectWorkStart { get; }

    public int AnimBuildObjectWorkEnd { get; }

    public int AnimBuildObjectEndStart { get; }

    public int AnimBuildObjectEndEnd { get; }

    public int AnimDigNormalStartStart { get; }

    public int AnimDigNormalStartEnd { get; }

    public int AnimDigNormalWorkStart { get; }

    public int AnimDigNormalWorkEnd { get; }

    public int AnimDigNormalEndStart { get; }

    public int AnimDigNormalEndEnd { get; }

    public int AnimDigLowStartStart { get; }

    public int AnimDigLowStartEnd { get; }

    public int AnimDigLowWorkStart { get; }

    public int AnimDigLowWorkEnd { get; }

    public int AnimDigLowEndStart { get; }

    public int AnimDigLowEndEnd { get; }

    public string DigSmokeId { get; }
  }
}
