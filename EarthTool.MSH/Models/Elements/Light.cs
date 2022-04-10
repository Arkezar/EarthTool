using EarthTool.MSH.Interfaces;
using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public abstract class Light : Vector, ILight
  {
    public Color Color { get; set; }

    public bool IsAvailable =>
      Value.Length() > 0;

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(base.ToByteArray(encoding));
          writer.Write((float)Math.Round(Color.R / 255f, 3));
          writer.Write((float)Math.Round(Color.G / 255f, 3));
          writer.Write((float)Math.Round(Color.B / 255f, 3));
        }
        return stream.ToArray();
      }
    }
  }
}