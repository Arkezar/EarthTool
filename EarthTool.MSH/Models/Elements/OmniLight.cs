using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class OmniLight : Light
  {
    public float Radius { get; }

    public OmniLight(Stream stream) : base(stream)
    {
      Radius = BitConverter.ToSingle(stream.ReadBytes(4));
    }

    public override byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(Radius);
        }
        return base.ToByteArray().Concat(stream.ToArray()).ToArray();
      }
    }
  }
}