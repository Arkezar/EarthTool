using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class Light
  {
    public float X { get; }

    public float Y { get; }

    public float Z { get; }

    public float Intensity { get; }

    public float U1 { get; }

    public float U2 { get; }

    public float U3 { get; }

    public float U4 { get; }

    public float U5 { get; }

    public Color Color
    {
      get;
    }

    public bool IsAvailable =>
      X != 0 || Y != 0 || Z != 0;

    public Light(Stream stream)
    {
      X = BitConverter.ToSingle(stream.ReadBytes(4));
      Y = BitConverter.ToSingle(stream.ReadBytes(4));
      Z = BitConverter.ToSingle(stream.ReadBytes(4));

      var r = BitConverter.ToSingle(stream.ReadBytes(4)) * 0xff;
      var g = BitConverter.ToSingle(stream.ReadBytes(4)) * 0xff;
      var b = BitConverter.ToSingle(stream.ReadBytes(4)) * 0xff;
      Color = Color.FromArgb((int)r, (int)g, (int)b);

      Intensity = BitConverter.ToSingle(stream.ReadBytes(4));

      U1 = BitConverter.ToSingle(stream.ReadBytes(4));
      U2 = BitConverter.ToSingle(stream.ReadBytes(4));
      U3 = BitConverter.ToSingle(stream.ReadBytes(4));
      U4 = BitConverter.ToSingle(stream.ReadBytes(4));
      U5 = BitConverter.ToSingle(stream.ReadBytes(4));
    }
  }
}
