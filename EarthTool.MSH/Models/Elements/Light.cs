using EarthTool.Common.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models.Elements
{
  public abstract class Light : Vector
  {
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
    }

    public override byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        using(var writer = new BinaryWriter(stream))
        {
          writer.Write(Color.R / 255f);
          writer.Write(Color.G / 255f);
          writer.Write(Color.B / 255f);
        }
        return base.ToByteArray().Concat(stream.ToArray()).ToArray();
      }
    }
  }
}