using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class Slot
  {
    public int Id
    {
      get;
    }

    public Vector Position
    {
      get;
    }

    public double Direction
    {
      get;
    }

    public int Flag
    {
      get;
    }

    public bool IsValid
      => Position.X != -128 && Position.Y != -128 && Position.Z != -128;

    public Slot(Stream stream, int id)
    {
      Id = id;
      var x = BitConverter.ToInt16(stream.ReadBytes(2)) / 256f;
      var y = -BitConverter.ToInt16(stream.ReadBytes(2)) / 256f;
      var z = BitConverter.ToInt16(stream.ReadBytes(2)) / 256f;
      Position = new Vector(x, y, z);
      var direction = stream.ReadByte();
      Direction = direction / 256.0 * Math.PI * 2.0;
      Flag = stream.ReadByte();
    }

    public byte[] ToByteArray()
    {
      using(var stream = new MemoryStream())
      {
        using(var writer = new BinaryWriter(stream))
        {
          writer.Write((short)(Position.X * 256));
          writer.Write((short)(-Position.Y * 256));
          writer.Write((short)(Position.Z * 256));
          writer.Write((byte)(Direction / 2 / Math.PI * 256));
          writer.Write((byte)Flag);
        }
        return stream.ToArray();
      }
    }
  }
}
