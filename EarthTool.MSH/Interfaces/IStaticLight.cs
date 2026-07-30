using EarthTool.Common.Interfaces;
using System.Numerics;

namespace EarthTool.MSH.Interfaces
{
  public interface IStaticLight : IBinarySerializable
  {
    Vector3 Position { get; }
    Vector3 LightParameters { get; }
  }
}
