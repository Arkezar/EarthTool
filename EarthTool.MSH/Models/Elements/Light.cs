using EarthTool.Common.Extensions;
using System;
using System.Drawing;
using System.IO;

namespace EarthTool.MSH.Models.Elements
{
  public class Light : Vector
  {
    public float Length { get; }

    public int Direction { get; }

    public float Width { get; }

    public float U3 { get; }

    public float Tilt { get; }

    public float Ambience { get; }

    public Color Color
    {
      get;
    }

    public bool IsAvailable =>
      Value.Length() > 0;

    public Light(Stream stream) : base(stream)
    {
      var r = BitConverter.ToSingle(stream.ReadBytes(4)) * 0xff;
      var g = BitConverter.ToSingle(stream.ReadBytes(4)) * 0xff;
      var b = BitConverter.ToSingle(stream.ReadBytes(4)) * 0xff;
      Color = Color.FromArgb((int)r, (int)g, (int)b);

      Length = BitConverter.ToSingle(stream.ReadBytes(4));
      Direction = BitConverter.ToInt32(stream.ReadBytes(4));
      Width = BitConverter.ToSingle(stream.ReadBytes(4));
      U3 = BitConverter.ToSingle(stream.ReadBytes(4));
      Tilt = BitConverter.ToSingle(stream.ReadBytes(4));
      Ambience = BitConverter.ToSingle(stream.ReadBytes(4));
    }
  }
}
