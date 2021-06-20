using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class PositionOffsetFrames : List<Position>
  {
    public PositionOffsetFrames(Stream stream)
    {
      var length = BitConverter.ToInt32(stream.ReadBytes(4));
      AddRange(Enumerable.Range(0, length).Select(_ => new Position(stream)));
    }
  }
}
