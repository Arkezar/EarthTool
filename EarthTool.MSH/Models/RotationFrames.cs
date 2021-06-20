using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models
{
  public class RotationFrames : List<RotationFrame>
  {
    public RotationFrames(Stream stream)
    {
      var length = BitConverter.ToInt32(stream.ReadBytes(4));
      AddRange(Enumerable.Range(0, length).Select(_ => new RotationFrame(stream)));
    }
  }
}
