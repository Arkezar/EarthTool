using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class Position
  {
    public float X
    {
      get;
    }

    public float Y
    {
      get;
    }

    public float Z
    {
      get;
    }

    public Position(Stream stream)
    {
      X = BitConverter.ToSingle(stream.ReadBytes(4));
      Y = BitConverter.ToSingle(stream.ReadBytes(4));
      Z = BitConverter.ToSingle(stream.ReadBytes(4));
    }
  }
}
