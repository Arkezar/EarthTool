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

    public float U
    {
      get;
    }

    public float V
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

    public Vertex() : this(new Vector(), new Vector(), 0, 1, 0, 0)
    {
    }

    public Vertex(IVector position, IVector normal, float u, float v, short u1, short u2)
    {
      Position = position;
      Normal = normal;
      U = u;
      V = v;
      UnknownValue1 = u1;
      UnknownValue2 = u2;
    }
  }
}
