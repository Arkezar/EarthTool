using EarthTool.MSH.Enums;
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

    public byte BackTrackDepth { get; set; }

    public PartType PartType { get; set; }

    public short Empty { get; set; }

    public ITextureInfo Texture { get; set; }

    public IEnumerable<IFace> Faces { get; set; }

    public IAnimations Animations { get; set; }

    public AnimationType AnimationType { get; set; }

    public IVector Offset { get; set; }

    public byte UnknownFlag { get; set; }

    public byte UnknownByte1 { get; set; }

    public byte UnknownByte2 { get; set; }

    public byte UnknownByte3 { get; set; }

    public double RiseAngle { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(GetVertexBytes());
          writer.Write(BackTrackDepth);
          writer.Write((byte)PartType);
          writer.Write(Empty);
          writer.Write(Texture.ToByteArray(encoding));
          writer.Write(Faces.Count());
          writer.Write(Faces.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          writer.Write(Animations.ToByteArray(encoding));
          writer.Write((int)AnimationType);
          writer.Write(Offset.ToByteArray(encoding));
          writer.Write(GetRiseAngle());
          writer.Write(UnknownFlag);
          writer.Write(UnknownByte1);
          writer.Write(UnknownByte2);
          writer.Write(UnknownByte3);
        }

        return stream.ToArray();
      }
    }

    private byte GetRiseAngle()
    {
      return (byte)(RiseAngle * (byte.MaxValue / 360));
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
                blockVertices.AddRange(Enumerable.Repeat(new Vertex(new Vector(), new Vector(), new TextureCoordinate(), 0, 0),
                  4 - blockVertices.Count()));
                blockVertices.ForEach(v => blockWriter.Write(v.Position.X));
                blockVertices.ForEach(v => blockWriter.Write(-v.Position.Y));
                blockVertices.ForEach(v => blockWriter.Write(v.Position.Z));
                blockVertices.ForEach(v => blockWriter.Write(v.Normal.X));
                blockVertices.ForEach(v => blockWriter.Write(-v.Normal.Y));
                blockVertices.ForEach(v => blockWriter.Write(v.Normal.Z));
                blockVertices.ForEach(v => blockWriter.Write(v.TextureCoordinate.U));
                blockVertices.ForEach(v => blockWriter.Write(v.TextureCoordinate.V));
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