using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class ModelPart : IModelPart
  {
    public IEnumerable<IVertex> Vertices { get; set; }

    public byte Depth { get; set; }

    public byte PartType { get; set; }

    public short UnknownFlag2 { get; set; }

    public ITextureInfo Texture { get; set; }

    public IEnumerable<IFace> Faces { get; set; }

    public IAnimations Animations { get; set; }

    public int UnknownValue { get; set; }

    public IVector Offset { get; set; }

    public byte[] UnknownBytes { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(GetVertexBytes());
          writer.Write(Depth);
          writer.Write(PartType);
          writer.Write(UnknownFlag2);
          writer.Write(Texture.ToByteArray(encoding));
          writer.Write(Faces.Count());
          writer.Write(Faces.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          writer.Write(Animations.ToByteArray(encoding));
          writer.Write(UnknownValue);
          writer.Write(Offset.ToByteArray(encoding));
          writer.Write(UnknownBytes);
        }
        return stream.ToArray();
      }
    }

    private byte[] GetVertexBytes()
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(Vertices.Count());
          var blocks = (int)Math.Ceiling(Vertices.Count() / 4d);
          writer.Write(blocks);

          for (var i = 0; i < blocks; i++)
          {
            using (var blockStream = new MemoryStream(160))
            {
              using (var blockWriter = new BinaryWriter(blockStream))
              {
                var blockVertices = Vertices.Skip(i * 4).Take(4).ToList();
                blockVertices.AddRange(Enumerable.Repeat(new Vertex(new Vector(), new Vector(), new UVMap(), 0, 0), 4 - blockVertices.Count()));
                blockVertices.ForEach(v => blockWriter.Write(v.Position.X));
                blockVertices.ForEach(v => blockWriter.Write(-v.Position.Y));
                blockVertices.ForEach(v => blockWriter.Write(v.Position.Z));
                blockVertices.ForEach(v => blockWriter.Write(v.Normal.X));
                blockVertices.ForEach(v => blockWriter.Write(-v.Normal.Y));
                blockVertices.ForEach(v => blockWriter.Write(v.Normal.Z));
                blockVertices.ForEach(v => blockWriter.Write(v.UVMap.U));
                blockVertices.ForEach(v => blockWriter.Write(1 - v.UVMap.V));
                blockVertices.ForEach(_ => blockWriter.Write(0));
                blockVertices.ForEach(v => blockWriter.Write(v.NormalVectorIdx));
                blockVertices.ForEach(v => blockWriter.Write(v.PositionVectorIdx));
              }
              writer.Write(blockStream.ToArray());
            }
          }
        }
        return stream.ToArray();
      }
    }
  }
}
