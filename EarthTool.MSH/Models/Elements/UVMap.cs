using EarthTool.MSH.Interfaces;

namespace EarthTool.MSH.Models.Elements
{
  public class UVMap : IUVMap
  {
    public float U
    {
      get;
    }

    public float V
    {
      get;
    }

    public UVMap()
    {
      
    }

    public UVMap(float u, float v)
    {
      U = u;
      V = v;
    }
  }
}