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

    public short NormalVectorIdx
    {
      get;
    }

    public short PositionVectorIdx
    {
      get;
    }

    public Vertex(IVector position, IVector normal, IUVMap uvMap, short normalVectorIdx, short positionVectorIdx)
    {
      Position = position;
      Normal = normal;
      UVMap = uvMap;
      NormalVectorIdx = normalVectorIdx;
      PositionVectorIdx = positionVectorIdx;
    }

    public bool Equals(IVertex other)
    {
      return Position.Equals(other.Position) && Normal.Equals(other.Normal) && UVMap.Equals(other.UVMap);
    }
  }
}
