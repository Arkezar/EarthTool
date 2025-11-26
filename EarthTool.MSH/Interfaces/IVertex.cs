using System;

namespace EarthTool.MSH.Interfaces
{
  public interface IVertex : IEquatable<IVertex>
  {
    IVector Normal { get; }
    IVector Position { get; }
    ITextureCoordinate TextureCoordinate { get; }
    short NormalVectorIdx { get; }
    short PositionVectorIdx { get; }
  }
}
