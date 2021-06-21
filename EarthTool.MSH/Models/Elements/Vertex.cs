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

    public Vertex(Vector position, Vector normal, float u, float v)
    {
      Position = position;
      Normal = normal;
      U = u;
      V = v;
    }
  }
}
