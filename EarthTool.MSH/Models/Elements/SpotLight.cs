using EarthTool.MSH.Interfaces;
using System;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class SpotLight : StaticLight, ISpotLight
  {
    public float HorizontalTargetDistance { get; set; }

    public byte TargetHeading { get; set; }

    public double TargetHeadingRadians => TargetHeading * Math.PI / 128.0;

    public byte Reserved1 { get; set; }

    public byte Reserved2 { get; set; }

    public byte Reserved3 { get; set; }

    public float ConeHalfAngleTangent { get; set; }

    public float DistanceScaledCone { get; set; }

    public float VerticalTargetSlope { get; set; }

    public float FinalParameter { get; set; }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(base.ToByteArray(encoding));
          writer.Write(HorizontalTargetDistance);
          writer.Write(TargetHeading);
          writer.Write(Reserved1);
          writer.Write(Reserved2);
          writer.Write(Reserved3);
          writer.Write(ConeHalfAngleTangent);
          writer.Write(DistanceScaledCone);
          writer.Write(VerticalTargetSlope);
          writer.Write(FinalParameter);
        }
        return stream.ToArray();
      }
    }
  }
}
