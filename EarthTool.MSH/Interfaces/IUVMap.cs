using System;

namespace EarthTool.MSH.Interfaces
{
  public interface IUVMap : IEquatable<IUVMap>
  {
    float U { get; }
    float V { get; }
  }
}