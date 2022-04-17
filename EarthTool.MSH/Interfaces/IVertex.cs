using System;

namespace EarthTool.MSH.Interfaces
{
  public interface IVertex : IEquatable<IVertex>
  {
    IVector Normal { get; }
    IVector Position { get; }
    IUVMap UVMap { get; }
    short NormalVectorIdx { get; }
    short PositionVectorIdx { get; }
  }
}