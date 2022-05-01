using EarthTool.Common.Interfaces;
using System.Numerics;

namespace EarthTool.MSH.Interfaces
{
  public interface IRotationFrame : IBinarySerializable
  {
    Matrix4x4 TransformationMatrix { get; }
  }
}