using System;

namespace EarthTool.MSH.Interfaces
{
  public interface ITextureCoordinate : IEquatable<ITextureCoordinate>
  {
    float S { get; }
    float T { get; }
    float U { get; }
    float V { get; }
  }
}