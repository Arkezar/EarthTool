using EarthTool.Common.Extensions;
using System;
using System.IO;

namespace EarthTool.MSH.Models.Elements
{
  public class Face
  {
    public int V1
    {
      get;
    }

    public int V2
    {
      get;
    }

    public int V3
    {
      get;
    }

    public int UNKNOWN
    {
      get;
    }

    public Face(Stream stream)
    {
      V1 = BitConverter.ToInt16(stream.ReadBytes(2));
      V2 = BitConverter.ToInt16(stream.ReadBytes(2));
      V3 = BitConverter.ToInt16(stream.ReadBytes(2));
      UNKNOWN = BitConverter.ToInt16(stream.ReadBytes(2));
    }
  }
}
