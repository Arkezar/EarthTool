using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class SpotLight : Light
  {
    public float Length { get; }

    public int Direction { get; }

    public float Width { get; }

    public float U3 { get; }

    public float Tilt { get; }

    public float Ambience { get; }


    public SpotLight(Stream stream) : base(stream)
    {
      Length = BitConverter.ToSingle(stream.ReadBytes(4));
      Direction = BitConverter.ToInt32(stream.ReadBytes(4));
      Width = BitConverter.ToSingle(stream.ReadBytes(4));
      U3 = BitConverter.ToSingle(stream.ReadBytes(4));
      Tilt = BitConverter.ToSingle(stream.ReadBytes(4));
      Ambience = BitConverter.ToSingle(stream.ReadBytes(4));
    }

    public override byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(Length);
          writer.Write(Direction);
          writer.Write(Width);
          writer.Write(U3);
          writer.Write(Tilt);
          writer.Write(Ambience);
        }
        return base.ToByteArray().Concat(stream.ToArray()).ToArray();
      }
    }
  }
}
