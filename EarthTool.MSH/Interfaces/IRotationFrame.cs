using EarthTool.Common.Interfaces;
using System.Numerics;

namespace EarthTool.MSH.Interfaces
{
  public interface IRotationFrame : IBinarySerializable
  {
    Quaternion Quaternion { get; }
    Matrix4x4 TransformationMatrix { get; }
  }
}