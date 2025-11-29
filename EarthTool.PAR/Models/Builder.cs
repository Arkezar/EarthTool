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
      WallId = ReadStringRef(data);
      BridgeId = ReadStringRef(data);
      TunnelNumber = ReadInteger(data);
      RoadBuildTime = ReadInteger(data);
      FlatBuildTime = ReadInteger(data);
      TrenchBuildTime = ReadInteger(data);
      TunnelBuildTime = ReadInteger(data);
      BuildObjectAnimationAngle = ReadInteger(data);
      DigNormalAnimationAngle = ReadInteger(data);
      DigLowAnimationAngle = ReadInteger(data);
      AnimBuildObjectStartStart = ReadInteger(data);
      AnimBuildObjectStartEnd = ReadInteger(data);
      AnimBuildObjectWorkStart = ReadInteger(data);
      AnimBuildObjectWorkEnd = ReadInteger(data);
      AnimBuildObjectEndStart = ReadInteger(data);
      AnimBuildObjectEndEnd = ReadInteger(data);
      AnimDigNormalStartStart = ReadInteger(data);
      AnimDigNormalStartEnd = ReadInteger(data);
      AnimDigNormalWorkStart = ReadInteger(data);
      AnimDigNormalWorkEnd = ReadInteger(data);
      AnimDigNormalEndStart = ReadInteger(data);
      AnimDigNormalEndEnd = ReadInteger(data);
      AnimDigLowStartStart = ReadInteger(data);
      AnimDigLowStartEnd = ReadInteger(data);
      AnimDigLowWorkStart = ReadInteger(data);
      AnimDigLowWorkEnd = ReadInteger(data);
      AnimDigLowEndStart = ReadInteger(data);
      AnimDigLowEndEnd = ReadInteger(data);
      DigSmokeId = ReadStringRef(data);
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
        () => ReferenceMarker,
        () => BridgeId,
        () => ReferenceMarker,
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
        () => ReferenceMarker
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      WriteStringRef(bw, WallId, encoding);
      WriteStringRef(bw, BridgeId, encoding);
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
      WriteStringRef(bw, DigSmokeId, encoding);

      return output.ToArray();
    }
  }
}
