using EarthTool.Common.Extensions;
using System;
using System.IO;
using System.Numerics;

namespace EarthTool.MSH.Models.Elements
{
  public class Vector
  {
    public float X => Value.X;

    public float Y => Value.Y;

    public float Z => Value.Z;

    public Vector3 Value
    {
      get;
    }

    public Vector(float x, float y, float z)
    {
      Value = new Vector3(x, y, z);
    }

    public Vector(Stream stream) : this(BitConverter.ToSingle(stream.ReadBytes(4)),
                                        -BitConverter.ToSingle(stream.ReadBytes(4)),
                                        BitConverter.ToSingle(stream.ReadBytes(4)))
    { }
  }
}
