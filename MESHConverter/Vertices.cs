using MESHConverter.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MESHConverter
{
  public class Vertices : List<Vertex>
  {
    const int VERTICES_BLOCK_LENGTH = 160;
    const int VERTICES_IN_BLOCK = 4;
    const int FIELD_SIZE = 4;

    public Vertices(Stream stream)
    {
      var vertices = BitConverter.ToInt32(stream.ReadBytes(4));
      var blocks = BitConverter.ToInt32(stream.ReadBytes(4));

      AddRange(Enumerable.Range(0, blocks).SelectMany(_ => GetVertices(stream.ReadBytes(VERTICES_BLOCK_LENGTH))).Take(vertices));
    }

    private IEnumerable<Vertex> GetVertices(byte[] vertexData)
    {
      for (var i = 0; i < VERTICES_IN_BLOCK; i++)
      {
        var idx = i * FIELD_SIZE;

        yield return new Vertex()
        {
          X = BitConverter.ToSingle(vertexData, idx + 0x00),
          Z = BitConverter.ToSingle(vertexData, idx + 0x10),
          Y = BitConverter.ToSingle(vertexData, idx + 0x20),

          NormalX = BitConverter.ToSingle(vertexData, idx + 0x30),
          NormalZ = BitConverter.ToSingle(vertexData, idx + 0x40),
          NormalY = BitConverter.ToSingle(vertexData, idx + 0x50),

          V = BitConverter.ToSingle(vertexData, idx + 0x60),
          U = BitConverter.ToSingle(vertexData, idx + 0x70)
        };
      }
    }
  }
}
