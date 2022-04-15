using EarthTool.MSH.Interfaces;

namespace EarthTool.MSH.Models.Elements
{
  public class Vertex : IVertex
  {
    public IVector Position
    {
      get;
    }

    public IVector Normal
    {
      get;
    }

    public IUVMap UVMap
    {
      get;
    }

    public short UnknownValue1
    {
      get;
    }

    public short UnknownValue2
    {
      get;
    }

    public Vertex(IVector position, IVector normal, IUVMap uvMap, short u1, short u2)
    {
      Position = position;
      Normal = normal;
      UVMap = uvMap;
      UnknownValue1 = u1;
      UnknownValue2 = u2;
    }
  }
}
