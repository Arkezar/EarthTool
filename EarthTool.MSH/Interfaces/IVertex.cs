using System;

namespace EarthTool.MSH.Interfaces
{
  public interface IVertex : IEquatable<IVertex>
  {
    IVector Normal { get; }
    IVector Position { get; }
    ITextureCoordinate TextureCoordinate { get; }
    ushort NormalVectorIdx { get; }
    ushort PositionVectorIdx { get; }
  }
}
