using EarthTool.Common.Extensions;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models.Collections
{
  public class Vertices : List<Vertex>
  {
    const int VERTICES_BLOCK_LENGTH = 160;
    const int VERTICES_IN_BLOCK = 4;
    const int FIELD_SIZE = sizeof(float);

    public Vertices(Stream stream)
    {
      var vertices = BitConverter.ToInt32(stream.ReadBytes(4));
      var blocks = BitConverter.ToInt32(stream.ReadBytes(4));

      AddRange(Enumerable.Range(0, blocks).SelectMany(_ => GetVertices(stream.ReadBytes(VERTICES_BLOCK_LENGTH))).Take(vertices));
    }

    public byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(this.Count);
          var blocks = this.Count / VERTICES_IN_BLOCK;
          writer.Write(blocks);

          for (var i = 0; i < blocks; i++)
          {
            using (var blockStream = new MemoryStream(VERTICES_BLOCK_LENGTH))
            {
              using (var blockWriter = new BinaryWriter(blockStream))
              {
                var blockVertices = this.Skip(i * VERTICES_IN_BLOCK).Take(VERTICES_IN_BLOCK).ToList();
                blockVertices.ForEach(v => blockWriter.Write(v.Position.X));
                blockVertices.ForEach(v => blockWriter.Write(-v.Position.Y));
                blockVertices.ForEach(v => blockWriter.Write(v.Position.Z));
                blockVertices.ForEach(v => blockWriter.Write(v.Normal.X));
                blockVertices.ForEach(v => blockWriter.Write(-v.Normal.Y));
                blockVertices.ForEach(v => blockWriter.Write(v.Normal.Z));
                blockVertices.ForEach(v => blockWriter.Write(v.U));
                blockVertices.ForEach(v => blockWriter.Write(1 - v.V));
                blockVertices.ForEach(_ => blockWriter.Write(0));
                blockVertices.ForEach(_ => blockWriter.Write(uint.MaxValue));
              }
              writer.Write(blockStream.ToArray());
            }
          }
        }
        return stream.ToArray();
      }
    }

    private IEnumerable<Vertex> GetVertices(byte[] vertexData)
    {
      for (var i = 0; i < VERTICES_IN_BLOCK; i++)
      {
        var idx = i * FIELD_SIZE;

        var x = BitConverter.ToSingle(vertexData, idx + 0x00);
        var y = -BitConverter.ToSingle(vertexData, idx + 0x10);
        var z = BitConverter.ToSingle(vertexData, idx + 0x20);

        var normalX = BitConverter.ToSingle(vertexData, idx + 0x30);
        var normalY = -BitConverter.ToSingle(vertexData, idx + 0x40);
        var normalZ = BitConverter.ToSingle(vertexData, idx + 0x50);

        var u = BitConverter.ToSingle(vertexData, idx + 0x60);
        var v = 1 - BitConverter.ToSingle(vertexData, idx + 0x70);

        yield return new Vertex(new Vector(x, y, z), new Vector(normalX, normalY, normalZ), u, v);
      }
    }
  }
}
