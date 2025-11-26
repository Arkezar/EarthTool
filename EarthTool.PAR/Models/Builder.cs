using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Builder : Vehicle
  {
    public Builder()
    {
    }

    public Builder(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
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

    public string WallId { get; set; }

    public string BridgeId { get; set; }

    public int TunnelNumber { get; set; }

    public int RoadBuildTime { get; set; }

    public int FlatBuildTime { get; set; }

    public int TrenchBuildTime { get; set; }

    public int TunnelBuildTime { get; set; }

    public int BuildObjectAnimationAngle { get; set; }

    public int DigNormalAnimationAngle { get; set; }

    public int DigLowAnimationAngle { get; set; }

    public int AnimBuildObjectStartStart { get; set; }

    public int AnimBuildObjectStartEnd { get; set; }

    public int AnimBuildObjectWorkStart { get; set; }

    public int AnimBuildObjectWorkEnd { get; set; }

    public int AnimBuildObjectEndStart { get; set; }

    public int AnimBuildObjectEndEnd { get; set; }

    public int AnimDigNormalStartStart { get; set; }

    public int AnimDigNormalStartEnd { get; set; }

    public int AnimDigNormalWorkStart { get; set; }

    public int AnimDigNormalWorkEnd { get; set; }

    public int AnimDigNormalEndStart { get; set; }

    public int AnimDigNormalEndEnd { get; set; }

    public int AnimDigLowStartStart { get; set; }

    public int AnimDigLowStartEnd { get; set; }

    public int AnimDigLowWorkStart { get; set; }

    public int AnimDigLowWorkEnd { get; set; }

    public int AnimDigLowEndStart { get; set; }

    public int AnimDigLowEndEnd { get; set; }

    public string DigSmokeId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => WallId,
        () => 1,
        () => BridgeId,
        () => 1,
        () => TunnelNumber,
        () => RoadBuildTime,
        () => FlatBuildTime,
        () => TrenchBuildTime,
        () => TunnelBuildTime,
        () => BuildObjectAnimationAngle,
        () => DigNormalAnimationAngle,
        () => DigLowAnimationAngle,
        () => AnimBuildObjectStartStart,
        () => AnimBuildObjectStartEnd,
        () => AnimBuildObjectWorkStart,
        () => AnimBuildObjectWorkEnd,
        () => AnimBuildObjectEndStart,
        () => AnimBuildObjectEndEnd,
        () => AnimDigNormalStartStart,
        () => AnimDigNormalStartEnd,
        () => AnimDigNormalWorkStart,
        () => AnimDigNormalWorkEnd,
        () => AnimDigNormalEndStart,
        () => AnimDigNormalEndEnd,
        () => AnimDigLowStartStart,
        () => AnimDigLowStartEnd,
        () => AnimDigLowWorkStart,
        () => AnimDigLowWorkEnd,
        () => AnimDigLowEndStart,
        () => AnimDigLowEndEnd,
        () => DigSmokeId,
        () => 1
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
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
