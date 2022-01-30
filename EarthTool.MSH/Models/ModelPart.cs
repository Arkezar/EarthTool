using EarthTool.Common.Extensions;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using System;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class ModelPart
  {
    public Vertices Vertices
    {
      get;
    }

    public int SkipParent
    {
      get;
    }

    public int UnknownFlag
    {
      get;
    }

    public TextureInfo Texture
    {
      get;
    }

    public Faces Faces
    {
      get;
    }

    public Animations Animations
    {
      get;
    }

    public int UnknownValue
    {
      get;
    }

    public Vector Offset
    {
      get;
    }

    public byte[] UnknownBytes
    {
      get;
    }

    public ModelPart(Stream stream)
    {
      Vertices = new Vertices(stream);
      SkipParent = stream.ReadByte();
      UnknownFlag = stream.ReadByte();
      stream.ReadBytes(2); // empty
      Texture = new TextureInfo(stream);
      Faces = new Faces(stream);
      Animations = new Animations(stream);
      UnknownValue = BitConverter.ToInt32(stream.ReadBytes(4));
      Offset = new Vector(stream);
      UnknownBytes = stream.ReadBytes(5);
    }

    public byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream, Encoding.GetEncoding("ISO-8859-2")))
        {
          writer.Write(Vertices.ToByteArray());
          writer.Write((byte)SkipParent);
          writer.Write((byte)UnknownFlag);
          writer.Write(new byte[2]);
          writer.Write(Texture.FileName.Length);
          writer.Write(Encoding.GetEncoding("ISO-8859-2").GetBytes(Texture.FileName));
          writer.Write(Faces.ToByteArray());
          writer.Write(Animations.ToByteArray());
          writer.Write(UnknownValue);
          writer.Write(Offset.ToByteArray());
          writer.Write(UnknownBytes);
        }
        return stream.ToArray();
      }
    }
  }
}
