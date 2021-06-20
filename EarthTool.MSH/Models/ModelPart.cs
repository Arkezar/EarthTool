using EarthTool.Common.Extensions;
using System.IO;

namespace EarthTool.MSH.Models
{
  public class ModelPart
  {
    public Vertices Vertices
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

    public PartOffset Offset
    {
      get;
    }

    public ModelPart(Stream stream)
    {
      Vertices = new Vertices(stream);
      stream.ReadBytes(4); // empty
      Texture = new TextureInfo(stream);
      Faces = new Faces(stream);
      Animations = new Animations(stream);
      Offset = new PartOffset(stream);

      stream.ReadBytes(5);
    }
  }
}
