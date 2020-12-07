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

    public ModelPart(Stream stream)
    {
      Vertices = new Vertices(stream);
      stream.ReadBytes(4); // empty
      Texture = new TextureInfo(stream);
      Faces = new Faces(stream);

      new UnhandledData(stream, 3, 4);
      new UnhandledData(stream, 3, 4);
      new UnhandledData(stream, 16, 4);
      stream.ReadBytes(21);
    }
  }
}
