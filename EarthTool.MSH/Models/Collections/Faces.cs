using EarthTool.Common.Extensions;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models.Collections
{
  public class Faces : List<Face>
  {
    public Faces(Stream stream)
    {
      var faces = BitConverter.ToInt32(stream.ReadBytes(4));

      AddRange(Enumerable.Range(0, faces).Select(_ => new Face(stream)));
    }

    public byte[] ToByteArray()
    {
      using(var stream = new MemoryStream())
      {
        using(var writer = new BinaryWriter(stream))
        {
          writer.Write(this.Count);
          this.ForEach(f => writer.Write(f.ToByteArray()));
        }
        return stream.ToArray();
      }
    }
  }
}
