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

    public uint NextRecordMarker { get; set; }

    /// <summary>
    /// Gets or sets the barrel maximum angle in degrees.
    /// </summary>
    public double RiseAngle { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      var vertices = Vertices.ToArray();
      var faces = Faces.ToArray();
      ValidateFaceIndices(faces, vertices.Length);
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(GetVertexBytes(vertices));
          writer.Write(BackTrackDepth);
          writer.Write((byte)PartType);
          writer.Write(Empty);
          writer.Write(Texture.ToByteArray(encoding));
          writer.Write(faces.Length);
          writer.Write(faces.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          writer.Write(Animations.ToByteArray(encoding));
          writer.Write((int)AnimationType);
          writer.Write(Offset.ToByteArray(encoding));
          writer.Write(GetRiseAngle());
          writer.Write(NextRecordMarker);
        }

        return stream.ToArray();
      }
    }

    private byte GetRiseAngle()
    {
      if (!PartType.HasFlag(PartType.Barrel))
      {
        return 0;
      }

      return (byte)(RiseAngle * 256d / 360d);
    }

    private static void ValidateFaceIndices(IEnumerable<IFace> faces, int vertexCount)
    {
      foreach (var face in faces)
      {
        if (face.V1 >= vertexCount || face.V2 >= vertexCount || face.V3 >= vertexCount)
        {
          throw new InvalidOperationException(
            $"Triangle index is outside the declared vertex range 0..{vertexCount - 1}.");
        }
      }
    }

    private static byte[] GetVertexBytes(IReadOnlyList<IVertex> vertices)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(vertices.Count);
          var blocks = vertices.Count / 4 + (vertices.Count % 4 == 0 ? 0 : 1);
          writer.Write(blocks);

          for (var i = 0; i < blocks; i++)
          {
            using (var blockStream = new MemoryStream(160))
            {
              using (var blockWriter = new BinaryWriter(blockStream))
              {
                var blockStart = i * 4;
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => vertex.Position.X);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => -vertex.Position.Y);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => vertex.Position.Z);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => vertex.Normal.X);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => -vertex.Normal.Y);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => vertex.Normal.Z);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => vertex.TextureCoordinate.U);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => vertex.TextureCoordinate.T);
                WriteFloatChannel(blockWriter, vertices, blockStart, vertex => vertex.TextureCoordinate.W);
                WriteUInt16Channel(blockWriter, vertices, blockStart, vertex => vertex.NormalVectorIdx);
                WriteUInt16Channel(blockWriter, vertices, blockStart, vertex => vertex.PositionVectorIdx);
              }

              writer.Write(blockStream.ToArray());
            }
          }
        }

        return stream.ToArray();
      }
    }

    private static void WriteFloatChannel(BinaryWriter writer, IReadOnlyList<IVertex> vertices, int blockStart,
      Func<IVertex, float> getValue)
    {
      for (var lane = 0; lane < 4; lane++)
      {
        var vertexIndex = blockStart + lane;
        writer.Write(vertexIndex < vertices.Count ? getValue(vertices[vertexIndex]) : 0);
      }
    }

    private static void WriteUInt16Channel(BinaryWriter writer, IReadOnlyList<IVertex> vertices, int blockStart,
      Func<IVertex, ushort> getValue)
    {
      for (var lane = 0; lane < 4; lane++)
      {
        var vertexIndex = blockStart + lane;
        writer.Write(vertexIndex < vertices.Count ? getValue(vertices[vertexIndex]) : (ushort)0);
      }
    }
  }
}
