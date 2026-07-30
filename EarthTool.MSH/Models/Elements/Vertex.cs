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

    public ITextureCoordinate TextureCoordinate
    {
      get;
    }

    public ushort NormalVectorIdx
    {
      get;
    }

    public ushort PositionVectorIdx
    {
      get;
    }

    public Vertex(IVector position, IVector normal, ITextureCoordinate textureCoordinate, ushort normalVectorIdx,
      ushort positionVectorIdx)
    {
      Position = position;
      Normal = normal;
      TextureCoordinate = textureCoordinate;
      NormalVectorIdx = normalVectorIdx;
      PositionVectorIdx = positionVectorIdx;
    }

    public bool Equals(IVertex other)
    {
      return Position.Equals(other.Position) && Normal.Equals(other.Normal) && TextureCoordinate.Equals(other.TextureCoordinate);
    }
  }
}
