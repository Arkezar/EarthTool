using System.IO;

namespace EarthTool.MSH.Models.Collections
{
  public class Animations
  {
    public PositionOffsetFrames UnknownAnimationData;
    public PositionOffsetFrames MovementFrames;
    public RotationFrames RotationFrames;

    public Animations(Stream stream)
    {
      UnknownAnimationData = new PositionOffsetFrames(stream);
      MovementFrames = new PositionOffsetFrames(stream);
      RotationFrames = new RotationFrames(stream);
    }
  }
}
