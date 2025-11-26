using EarthTool.Common.Interfaces;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IAnimations : IBinarySerializable
  {
    IEnumerable<IVector> TranslationFrames { get; }
    IEnumerable<IRotationFrame> RotationFrames { get; }
    IEnumerable<IVector> ScaleFrames { get; }
  }
}
