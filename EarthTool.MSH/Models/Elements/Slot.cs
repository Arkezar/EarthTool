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

    public Slot(Stream stream, int id)
    {
      Id = id;
      var x = BitConverter.ToInt16(stream.ReadBytes(2)) / 256f;
      var y = BitConverter.ToInt16(stream.ReadBytes(2)) / 256f;
      var z = BitConverter.ToInt16(stream.ReadBytes(2)) / 256f;
      Position = new Vector(x, y, z);
      var direction = stream.ReadByte();
      Direction = Math.PI / 180.0 * (direction * 360.0 / 256.0);
      Flag = stream.ReadByte();
    }
  }
}
