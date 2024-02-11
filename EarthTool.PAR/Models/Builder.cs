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
    public Builder(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(WallId.Length);
          bw.Write(encoding.GetBytes(WallId));
          bw.Write(-1);
          bw.Write(BridgeId.Length);
          bw.Write(encoding.GetBytes(BridgeId));
          bw.Write(-1);
          bw.Write(TunnelNumber);
          bw.Write(RoadBuildTime);
          bw.Write(FlatBuildTime);
          bw.Write(TrenchBuildTime);
          bw.Write(TunnelBuildTime);
          bw.Write(BuildObjectAnimationAngle);
          bw.Write(DigNormalAnimationAngle);
          bw.Write(DigLowAnimationAngle);
          bw.Write(AnimBuildObjectStartStart);
          bw.Write(AnimBuildObjectStartEnd);
          bw.Write(AnimBuildObjectWorkStart);
          bw.Write(AnimBuildObjectWorkEnd);
          bw.Write(AnimBuildObjectEndStart);
          bw.Write(AnimBuildObjectEndEnd);
          bw.Write(AnimDigNormalStartStart);
          bw.Write(AnimDigNormalStartEnd);
          bw.Write(AnimDigNormalWorkStart);
          bw.Write(AnimDigNormalWorkEnd);
          bw.Write(AnimDigNormalEndStart);
          bw.Write(AnimDigNormalEndEnd);
          bw.Write(AnimDigLowStartStart);
          bw.Write(AnimDigLowStartEnd);
          bw.Write(AnimDigLowWorkStart);
          bw.Write(AnimDigLowWorkEnd);
          bw.Write(AnimDigLowEndStart);
          bw.Write(AnimDigLowEndEnd);
          bw.Write(DigSmokeId.Length);
          bw.Write(encoding.GetBytes(DigSmokeId));
          bw.Write(-1);
        }
        return output.ToArray();
      }
    }
  }
}
