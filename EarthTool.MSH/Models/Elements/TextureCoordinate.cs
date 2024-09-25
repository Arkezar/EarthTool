using EarthTool.MSH.Interfaces;

namespace EarthTool.MSH.Models.Elements
{
  public class TextureCoordinate : ITextureCoordinate
  {
    public float U { get; }
    public float V { get; }
    public float S => U;
    public float T => 1 - V;

    public TextureCoordinate()
    {
      U = 0;
      V = 0;
    }

    public TextureCoordinate(float u, float v)
    {
      U = u;
      V = v;
    }

    public bool Equals(ITextureCoordinate other)
    {
      return S == other.S && T == other.T;
    }
  }
}