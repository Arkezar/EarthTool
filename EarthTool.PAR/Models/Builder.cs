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
    public Builder(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      WallId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      BridgeId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      data.ReadBytes(4);
      TunnelNumber = BitConverter.ToInt32(data.ReadBytes(4));
      RoadBuildTime = BitConverter.ToInt32(data.ReadBytes(4));
      FlatBuildTime = BitConverter.ToInt32(data.ReadBytes(4));
      TrenchBuildTime = BitConverter.ToInt32(data.ReadBytes(4));
      TunnelBuildTime = BitConverter.ToInt32(data.ReadBytes(4));
      BuildObjectAnimationAngle = BitConverter.ToInt32(data.ReadBytes(4));
      DigNormalAnimationAngle = BitConverter.ToInt32(data.ReadBytes(4));
      DigLowAnimationAngle = BitConverter.ToInt32(data.ReadBytes(4));
      AnimBuildObjectStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimBuildObjectStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimBuildObjectWorkStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimBuildObjectWorkEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimBuildObjectEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimBuildObjectEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigNormalStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigNormalStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigNormalWorkStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigNormalWorkEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigNormalEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigNormalEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigLowStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigLowStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigLowWorkStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigLowWorkEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigLowEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimDigLowEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
      DigSmokeId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
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
