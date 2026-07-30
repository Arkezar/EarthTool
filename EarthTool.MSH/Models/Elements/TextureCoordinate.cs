using EarthTool.MSH.Interfaces;

namespace EarthTool.MSH.Models.Elements
{
  public class TextureCoordinate : ITextureCoordinate
  {
    public float U { get; }
    public float V { get; }
    public float W { get; }
    public float S => U;
    public float T { get; }

    public TextureCoordinate()
    {
      U = 0;
      V = 0;
      W = 0;
      T = 1;
    }

    public TextureCoordinate(float u, float v)
      : this(u, v, 0)
    {
    }

    public TextureCoordinate(float u, float v, float w)
      : this(u, v, w, 1 - v)
    {
    }

    private TextureCoordinate(float u, float v, float w, float t)
    {
      U = u;
      V = v;
      W = w;
      T = t;
    }

    internal static TextureCoordinate FromSerialized(float u, float v, float w)
    {
      return new TextureCoordinate(u, 1 - v, w, v);
    }

    public bool Equals(ITextureCoordinate other)
    {
      return S == other.S && T == other.T && W == other.W;
    }
  }
}
