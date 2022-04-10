using EarthTool.Common.Interfaces;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IAnimations : IBinarySerializable
  {
    IEnumerable<IVector> MovementFrames { get; }
    IEnumerable<IRotationFrame> RotationFrames { get; }
    IEnumerable<IVector> UnknownAnimationData { get; }
  }
}