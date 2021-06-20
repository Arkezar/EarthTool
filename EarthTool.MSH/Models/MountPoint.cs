using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MountPoint
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

    public bool IsAvailable =>
      X != 0 || Y != 0 || Z != 0;

    public MountPoint(Stream stream)
    {
      X = BitConverter.ToSingle(stream.ReadBytes(4), 0);
      Y = BitConverter.ToSingle(stream.ReadBytes(4), 0);
      Z = BitConverter.ToSingle(stream.ReadBytes(4), 0);
    }
  }
}
