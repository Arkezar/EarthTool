using EarthTool.Common.Extensions;
using System;
using System.IO;

namespace EarthTool.MSH.Models.Elements
{
  public class Face
  {
    public short V1
    {
      get;
    }

    public short V2
    {
      get;
    }

    public short V3
    {
      get;
    }

    public short UNKNOWN
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

    public byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(V1);
          writer.Write(V2);
          writer.Write(V3);
          writer.Write(UNKNOWN);
        }
        return stream.ToArray();
      }
    }
  }
}
