namespace EarthTool.MSH.Models.Elements
{
  public class Vertex
  {
    public Vector Position
    {
      get;
    }

    public Vector Normal
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

    public short U1
    {
      get;
    }

    public short U2
    {
      get;
    }

    public Vertex(Vector position, Vector normal, float u, float v, short u1, short u2)
    {
      Position = position;
      Normal = normal;
      U = u;
      V = v;
      U1 = u1;
      U2 = u2;
    }
  }
}
