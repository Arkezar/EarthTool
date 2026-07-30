using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface ISpotLight : IStaticLight, IBinarySerializable
  {
    float HorizontalTargetDistance { get; set; }
    byte TargetHeading { get; set; }
    double TargetHeadingRadians { get; }
    byte Reserved1 { get; set; }
    byte Reserved2 { get; set; }
    byte Reserved3 { get; set; }
    float ConeHalfAngleTangent { get; set; }
    float DistanceScaledCone { get; set; }
    float VerticalTargetSlope { get; set; }
    float FinalParameter { get; set; }
  }
}
