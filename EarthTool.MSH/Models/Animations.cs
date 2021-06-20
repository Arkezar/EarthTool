using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Models
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
